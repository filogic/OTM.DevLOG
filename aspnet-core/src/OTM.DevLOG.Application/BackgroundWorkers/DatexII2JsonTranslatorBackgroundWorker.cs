using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace OTM.DevLOG.BackgroundWorkers
{
    public class DatexII2JsonTranslatorBackgroundWorker
		: AsyncPeriodicBackgroundWorkerBase
    {
        const string _sourceGcpActualTrafficInfoSubscription = "devlog-v1-ncis-actual-traffic-info-sub";
        const string _destinationGcpActualTrafficInfoTopic = "devlog-v1-ncis-actual-traffic-info-json";
        const string _gcpProjectId = "devlog-prod";

        const int _gcpMaxPubSubBodyLength = 10000000;

        private readonly Microsoft.Extensions.Logging.ILogger _logger;




        public DatexII2JsonTranslatorBackgroundWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, ILogger<DatexII2JsonTranslatorBackgroundWorker> logger)
            : base( timer, serviceScopeFactory )
        {
            _logger = logger;

            this.Timer.Period = 250;
            this.Timer.Start();
        }

        protected async override Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {

                // Create new instance of subscriberServiceApiClient as well as subscriptionName
                var subscriberServiceApiClient = SubscriberServiceApiClient.Create();
                var subscriptionName = new SubscriptionName(_gcpProjectId, _sourceGcpActualTrafficInfoSubscription);

                // Pull messages from the subscription. This will wait for some time if no new messages have been
                // published yet.
                var pullResponse = await subscriberServiceApiClient.PullAsync(subscriptionName, maxMessages: 30);
                if (pullResponse?.ReceivedMessages == null)
                    throw new ApplicationException("subscriberServiceApiClient.PullAsync did not return an object.");

                var acknowledges = new List<String>();
                if (pullResponse != null && pullResponse.ReceivedMessages != null && pullResponse.ReceivedMessages.Count > 0)
                {
                    _logger.LogInformation("DatexII2JsonTranslatorBackgroundWorker pulled {0} messages of subscription {1} on t={2}", pullResponse?.ReceivedMessages?.Count, _sourceGcpActualTrafficInfoSubscription, sw.ElapsedMilliseconds );

                    var index = 0;
                    foreach (var receivedMessage in pullResponse.ReceivedMessages)
                    {
                        _logger.LogInformation("DatexII2JsonTranslatorBackgroundWorker is processing message {0} ({1} / {2}) of subscription {3} on t={4}",
                            receivedMessage.Message.MessageId, ++index, pullResponse.ReceivedMessages.Count, _sourceGcpActualTrafficInfoSubscription, sw.ElapsedMilliseconds);

                        var pubsubMessage = receivedMessage.Message;

                        var compressedBytes = pubsubMessage.Data.ToByteArray();
                        var uncompressedBytes = this.Decompress(compressedBytes);

                        // Convert xml payload to json payload
                        var xmlPayload = System.Text.Encoding.UTF8.GetString(uncompressedBytes);
                        var jsonPayload = this.ConvertXmlToJson(xmlPayload);


                        // Publish json payload to gcp destination topic
                        var payloadBytes = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                        if (payloadBytes != null && payloadBytes.Length < _gcpMaxPubSubBodyLength)
                        {
                            await this.PublishToPubSub( _destinationGcpActualTrafficInfoTopic, payloadBytes);

                            _logger.LogInformation("DatexII2JsonTranslatorBackgroundWorker translated message {0} to json and published {1} bytes to {2}",
                                receivedMessage.Message.MessageId, payloadBytes.Length, _destinationGcpActualTrafficInfoTopic);
                        }
                        else
                        {
                            _logger.LogInformation("DatexII2JsonTranslatorBackgroundWorker ignored message {0} having {1} bytes.",
                                receivedMessage.Message.MessageId, payloadBytes?.Length);
                        }
                        acknowledges.Add(receivedMessage.AckId);
                    }

                    // Acknowledge that we've received the messages. If we don't do this within 60 seconds (as specified
                    // when we created the subscription) we'll receive the messages again when we next pull.
                    if (acknowledges.Count > 0)
                    {
                        await subscriberServiceApiClient.AcknowledgeAsync(subscriptionName, acknowledges);
                        _logger.LogInformation("DatexII2JsonTranslatorBackgroundWorker acknowledged {0} messages of subscription {1} on t={2}", acknowledges.Count, _sourceGcpActualTrafficInfoSubscription, sw.ElapsedMilliseconds);
                    }
                }
            }
            catch( Exception exception )
            {
                _logger.LogInformation("Error DatexII2JsonTranslatorBackgroundWorker:DoWork on t={0}.  {1}.", sw.ElapsedMilliseconds, exception.ToString() );
            }
            finally
            {
                this.Timer.Period = 250;
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

