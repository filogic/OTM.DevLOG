using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OTM.DevLOG.Data;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;
using System.Collections.Generic;
using OTM.DevLOG.Extensions;

namespace OTM.DevLOG.BackgroundWorkers
{
    public class NdwOpenDataMeasurementSiteReferenceDownloadBackgroundWorker
        : AsyncPeriodicBackgroundWorkerBase
    {
        private readonly IRepository<NdwOpenDataMeasurementSiteReference, Guid> _ndwOpenDataMeasurementSiteReferenceRepository;

        public NdwOpenDataMeasurementSiteReferenceDownloadBackgroundWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, IRepository<NdwOpenDataMeasurementSiteReference, Guid> ndwOpenDataMeasurementSiteReferenceRepository ) 
            : base( timer, serviceScopeFactory )
        {
            _ndwOpenDataMeasurementSiteReferenceRepository = ndwOpenDataMeasurementSiteReferenceRepository;
            this.Timer.Period = 60000 * 3;
            this.Timer.Start();
        }



        protected async override Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            if (System.DateTime.UtcNow.IsNightTime())
                return;


            this.Timer.Stop();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // Download file from ndw opendata
                var tempCompressedFile = System.IO.Path.GetTempFileName();
                var requestUriString = "https://opendata.ndw.nu/measurement.xml.gz";
                this.Logger.LogInformation("{0} is downloading {1} to {2} ...", this.GetType().Name, requestUriString, tempCompressedFile);

                using (var httpClientHandler = new System.Net.Http.HttpClientHandler())
                {
                    httpClientHandler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true;
                    using (var httpClient = new System.Net.Http.HttpClient(httpClientHandler))
                    {
                        var responseStream = await httpClient.GetStreamAsync(requestUriString);
                        using (var fileStream = new FileStream(tempCompressedFile, FileMode.OpenOrCreate))
                        {
                            await responseStream.CopyToAsync(fileStream);
                        }
                        await responseStream.DisposeAsync();
                    }
                }
                this.Logger.LogInformation("{0} downloaded {1} to {2} in {3} ms.", this.GetType().Name, requestUriString, tempCompressedFile, sw.ElapsedMilliseconds );


                // Decompress file
                sw.Restart();
                var tempUncompressedFile = System.IO.Path.GetTempFileName();
                using (var uncompressedFileStream = File.Create(tempUncompressedFile))
                {
                    using (var compressedFileStream = File.OpenRead(tempCompressedFile))
                    {
                        using (var gzipStream = new GZipStream(compressedFileStream, CompressionMode.Decompress))
                        {
                            await gzipStream.CopyToAsync(uncompressedFileStream);
                        }
                    }
                }
                this.Logger.LogInformation("{0} decompressed {1} to {2} in {3} ms.", this.GetType().Name, tempCompressedFile, tempUncompressedFile, sw.ElapsedMilliseconds);


                // Extract all measurementSiteRecords from xml payload
                sw.Restart();
                var xmlPayload = await System.IO.File.ReadAllTextAsync(tempUncompressedFile);
                var xmlDoc = System.Xml.Linq.XDocument.Parse(xmlPayload);
                var measurementSiteRecords = xmlDoc
                    .Descendants()
                    .Where(e => e.Name.LocalName == "measurementSiteRecord")
                    .ToList();
                this.Logger.LogInformation("{0} extracted {1} measurementSiteRecords from {2} in {3} ms.", this.GetType().Name, measurementSiteRecords?.Count ?? 0, tempUncompressedFile, sw.ElapsedMilliseconds);


                // Persist all measurementSiteRecords in repository
                if (measurementSiteRecords != null && measurementSiteRecords.Count > 0)
                {
                    sw.Restart();

                    // Build list with measurement site references
                    var measurementSiteReferences = new List<NdwOpenDataMeasurementSiteReference>();
                    measurementSiteRecords.ForEach(measurementSiteRecord =>
                    {
                        var measurementSiteId = measurementSiteRecord.Attributes().Where(a => a.Name.LocalName == "id").FirstOrDefault()?.Value;
                        if (!String.IsNullOrWhiteSpace(measurementSiteId))
                        {
                            var serializedJson = JsonConvert.SerializeObject(measurementSiteRecord);
                            measurementSiteReferences.Add(new NdwOpenDataMeasurementSiteReference()
                            {
                                MeasurementSiteId = measurementSiteId,
                                MeasurementSiteReference = serializedJson
                            });
                        }
                    });
                    measurementSiteReferences = measurementSiteReferences.OrderBy(__ => __.MeasurementSiteId).ToList();

                    // Delete existing - and insert new measurement site references
                    await _ndwOpenDataMeasurementSiteReferenceRepository.DeleteDirectAsync(__ => true);
                    await _ndwOpenDataMeasurementSiteReferenceRepository.InsertManyAsync(measurementSiteReferences);
                }

                if ( System.IO.File.Exists(tempCompressedFile))
                    System.IO.File.Delete(tempCompressedFile);

                if (System.IO.File.Exists(tempUncompressedFile))
                    System.IO.File.Delete(tempUncompressedFile);
            }
            catch( Exception exception )
            {
                this.Logger.LogException( exception );
            }
            finally
            {
                this.Timer.Period = 60000 * 3;
                this.Timer.Start();
            }
        }



        private async Task PublishToPubSub(string topic, byte[] data)
        {
            var pubsubMessage = new PubsubMessage
            {
                Data = Google.Protobuf.ByteString.CopyFrom(data)
            };

            const string projectId = "devlog-prod";

            var topicName = new TopicName(projectId, topic);
            var publisherServiceApiClient = PublisherServiceApiClient.Create();

            await publisherServiceApiClient.PublishAsync(topicName, new[] { pubsubMessage });
        }


        private byte[] Decompress(byte[] inputData)
        {
            try
            {
                if (inputData == null)
                    throw new ArgumentNullException("inputData must be non-null");

                using (var input = new MemoryStream(inputData))
                {
                    using (var output = new MemoryStream())
                    {
                        using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
                        {
                            dstream.CopyTo(output);
                        }
                        return output.ToArray();
                    }
                }
            }
            catch (Exception) { /* Ignore this exception */ }
            return inputData;
        }


        private string ConvertXmlToJson(string xmlString)
        {
            try
            {
                xmlString = xmlString.Replace("?", String.Empty).Replace("@", String.Empty).Replace("<xml version=\"1.0\" encoding=\"UTF-8\">", String.Empty);
                xmlString = DuplicateMeasuredValueIfNeeded(xmlString, "_SiteMeasurementsIndexMeasuredValue");
                xmlString = DuplicateMeasuredValueIfNeeded(xmlString, "MeasuredValue");

                // Load the XML string into an XDocument
                var xmlDoc = System.Xml.Linq.XDocument.Parse(xmlString);

                // Remove namespaces from the XML
                RemoveNamespaces(xmlDoc.Root);

                // Find all "measuredValue" elements in the XML and adjust them
                //AdjustMeasuredValueElements(xmlDoc);


                // Convert the XDocument to JSON using Newtonsoft.Json
                string json = JsonConvert.SerializeXNode(xmlDoc, Formatting.None, true);
                return json;
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur during conversion
                Console.WriteLine($"Error converting XML to JSON: {ex.Message}");
                return null;
            }
        }


        private string DuplicateMeasuredValueIfNeeded(string xml, string attribute)
        {
            var xmlDoc = System.Xml.Linq.XDocument.Parse(xml);
            var measuredValueElements = xmlDoc.Descendants()
                .Where(e => e.Name.LocalName == "measuredValue" && e.Attributes().Any(a => a.Value == attribute))
                .ToList();

            foreach (var measuredValueElement in measuredValueElements)
            {
                if (measuredValueElement.Elements().Count() == 1)
                {
                    // Duplicate the measuredValue element
                    var duplicatedElement = new System.Xml.Linq.XElement(measuredValueElement);
                    measuredValueElement.AddAfterSelf(duplicatedElement);
                }
            }

            return xmlDoc.ToString();
        }


        private void RemoveNamespaces(System.Xml.Linq.XElement element)
        {
            foreach (var e in element.DescendantsAndSelf())
            {
                e.Name = e.Name.LocalName;
                e.ReplaceAttributes(
                    e.Attributes().Where(a => !a.IsNamespaceDeclaration)
                );
            }
        }
    }
}

