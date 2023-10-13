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
using OTM.DevLOG.BackgroundWorkers.Dto;
using OTM.DevLOG.Data;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;
using OTM.DevLOG.Extensions;

using static Volo.Abp.Identity.IdentityPermissions;

namespace OTM.DevLOG.BackgroundWorkers
{
    public class DatexIIJsonOtmPublisherBackgroundWorker
        : AsyncPeriodicBackgroundWorkerBase
    {
        const string _gcpProjectId = "devlog-prod";

        const string _sourceGcpActualTrafficInfoTopic = "devlog-v1-ncis-actual-traffic-info-json-sub";
        const string _destinationGcpActualTrafficInfoSubscription = "devlog-v1-ncis-otm";

        const int _gcpMaxPubSubBodyLength = 10000000;


        private readonly IRepository<NdwOpenDataMeasurementSiteReference, Guid> _ndwOpenDataMeasurementSiteReferenceRepository;



        public DatexIIJsonOtmPublisherBackgroundWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, IRepository<NdwOpenDataMeasurementSiteReference, Guid> ndwOpenDataMeasurementSiteReferenceRepository)
            : base( timer, serviceScopeFactory )
        {
             _ndwOpenDataMeasurementSiteReferenceRepository = ndwOpenDataMeasurementSiteReferenceRepository;

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
                var subscriptionName = new SubscriptionName(_gcpProjectId, _sourceGcpActualTrafficInfoTopic);


                // Pull messages from the subscription. This will wait for some time if no new messages have been
                // published yet.
                var pullResponse = await subscriberServiceApiClient.PullAsync(subscriptionName, maxMessages: 20);
                if (pullResponse == null || pullResponse.ReceivedMessages == null)
                    throw new ApplicationException("subscriberServiceApiClient.PullAsync did not return an object from " + _sourceGcpActualTrafficInfoTopic);

                var acknowledges = new List<String>();
                if (pullResponse != null && pullResponse.ReceivedMessages != null && pullResponse.ReceivedMessages.Count > 0)
                {
                    this.Logger.LogInformation("DatexIIJsonOtmPublisherBackgroundWorker pulled {0} messages of subscription {1} on t={2}", pullResponse?.ReceivedMessages?.Count, _sourceGcpActualTrafficInfoTopic, sw.ElapsedMilliseconds );

                    var index = 0;
                    foreach (var receivedMessage in pullResponse.ReceivedMessages)
                    {
                        this.Logger.LogInformation("DatexIIJsonOtmPublisherBackgroundWorker is processing message {0} ({1} / {2}) of subscription {3} on t={4}",
                            receivedMessage.Message.MessageId, ++index, pullResponse.ReceivedMessages.Count, _sourceGcpActualTrafficInfoTopic, sw.ElapsedMilliseconds);

                        var pubsubMessage = receivedMessage.Message;

                        // Convert xml payload to json payload
                        var datex2PayloadBytes = pubsubMessage.Data.ToByteArray();
                        var datex2Payload = System.Text.Encoding.UTF8.GetString(datex2PayloadBytes);
                        var otmPayload = await this.ConvertDatex2ToOTM(datex2Payload);

                        // Publish json payload to gcp destination topic
                        var payloadBytes = String.IsNullOrWhiteSpace(otmPayload) ? null : System.Text.Encoding.UTF8.GetBytes(otmPayload);
                        if (payloadBytes != null && payloadBytes.Length < _gcpMaxPubSubBodyLength)
                        {
                            await this.PublishToPubSub(_destinationGcpActualTrafficInfoSubscription, payloadBytes);

                            this.Logger.LogInformation("DatexIIJsonOtmPublisherBackgroundWorker translated message {0} to OTM and published {1} bytes to {2}",
                                receivedMessage.Message.MessageId, payloadBytes.Length, _destinationGcpActualTrafficInfoSubscription);
                        }
                        else if (payloadBytes != null )
                        {
                            this.Logger.LogInformation("DatexIIJsonOtmPublisherBackgroundWorker ignored message {0} having {1} bytes.",
                                receivedMessage.Message.MessageId, payloadBytes?.Length);
                        }
                        acknowledges.Add(receivedMessage.AckId);
                    }

                    // Acknowledge that we've received the messages. If we don't do this within 60 seconds (as specified
                    // when we created the subscription) we'll receive the messages again when we next pull.
                    if (acknowledges.Count > 0)
                    {
                        await subscriberServiceApiClient.AcknowledgeAsync(subscriptionName, acknowledges);
                        this.Logger.LogInformation("DatexIIJsonOtmPublisherBackgroundWorker acknowledged {0} messages of subscription {1} on t={2}", acknowledges.Count, _sourceGcpActualTrafficInfoTopic, sw.ElapsedMilliseconds);
                    }
                }
            }
            catch( Exception exception )
            {
                this.Logger.LogInformation("Error DatexIIJsonOtmPublisherBackgroundWorker:DoWork on t={0}.  {1}.", sw.ElapsedMilliseconds, exception.ToString() );
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


        private async Task<System.String?> ConvertDatex2ToOTM( string datex2Payload )
        {
            try
            {
                datex2Payload = datex2Payload.Replace("@id", "id");
                datex2Payload = datex2Payload.Replace("@index", "index");
                datex2Payload = datex2Payload.Replace("@type", "type");

                // Deserialize datex2 payload
                var datex2SiteMeasurementDto = JsonConvert.DeserializeObject<Dto.Datex2SiteMeasurementDto>(datex2Payload);
                if ((datex2SiteMeasurementDto?.Body?.d2LogicalModel?.payloadPublication?.siteMeasurements?.Count() ?? 0) == 0)
                    return null;

                // Distinct site reference identifiers
                var siteMeasurements = datex2SiteMeasurementDto.Body.d2LogicalModel.payloadPublication.siteMeasurements.ToList();
                var siteMeasurementReferences = siteMeasurements
                    .Where(m => m.measurementSiteReference != null)
                    .Select(__ => __.measurementSiteReference.id)
                    .Distinct();


                // Load site reference identifiers from repository
                var otmConstraints = new List<dynamic>();
                var measurementSiteReferences = await _ndwOpenDataMeasurementSiteReferenceRepository.GetListAsync(siteReference => siteMeasurementReferences.Contains(siteReference.MeasurementSiteId));
                measurementSiteReferences.ForEach(measurementSiteReference =>
                {
                    // Find last site measurement
                    var mostRecentSiteMeasurement = siteMeasurements.Where(sm => sm.measurementSiteReference.id == measurementSiteReference.MeasurementSiteId).OrderBy( sm => sm.measurementTimeDefault ).LastOrDefault();
                    var measuredValues = mostRecentSiteMeasurement.measuredValue.ToList();

                    measuredValues.ForEach(measuredValue =>
                    {
                        var otmConstraint = this.ConvertSiteMeasurementToOtmConstraint(mostRecentSiteMeasurement, measurementSiteReference, measuredValue);
                        if (otmConstraint != null)
                            otmConstraints.Add(otmConstraint);
                    });
                });

                return JsonConvert.SerializeObject(otmConstraints);
            }
            catch (Exception exception)
            {
                this.Logger.LogError(exception, "Failed to convert DatexII to OTM.");
                return null;
            }
        }

        private dynamic ConvertSiteMeasurementToOtmConstraint( Sitemeasurement siteMeasurement, NdwOpenDataMeasurementSiteReference measurementSiteReference, Measuredvalue measuredValue)
        {
            var geoReferences = this.CreateGeoReference(measurementSiteReference);
            if (geoReferences == null)
                return null;

            var constraintValue = this.CreateConstraintValueFromBasicData(measuredValue.measuredValue.basicData);
            if (constraintValue == null)
                return null;


            return new
            {
                Id = Guid.NewGuid(),
                Name = String.Join("_", measurementSiteReference.MeasurementSiteId, siteMeasurement.measurementTimeDefault),
                creationDate = siteMeasurement.measurementTimeDefault,
                lastModified = siteMeasurement.measurementTimeDefault,
                GeoReferences = geoReferences,
                Constraint = new
                {
                    associationType = "inline",
                    entity = new
                    {
                        Id = Guid.NewGuid(),
                        Name = String.Join("_", measurementSiteReference.MeasurementSiteId, siteMeasurement.measurementTimeDefault, measuredValue.index ),
                        creationDate = siteMeasurement.measurementTimeDefault,
                        lastModified = siteMeasurement.measurementTimeDefault,
                        Value = constraintValue
                    },
                    description = measuredValue.measuredValue.basicData.type,
                }
            };
        }


        private dynamic CreateConstraintValueFromBasicData(Basicdata basicData)
        {
            if (basicData.averageVehicleSpeed != null )
            {
                var maximumSpeed = 0D;
                if ( Double.TryParse(basicData.averageVehicleSpeed.speed, out double s ) )
                    maximumSpeed = s;

                return new
                {
                    Enforceability = "preference",
                    Type = "ValueBoundConstraint",
                    ValueType = "Speed",
                    ConstraintType = "Maximum",
                    Maximum = new
                    {
                        Value = maximumSpeed,
                        Unit = "km/h"
                    },
                    Description = String.Format( "{0} average speed (km/h)", maximumSpeed )
                };
            }

            return null;
        }

        private dynamic CreateGeoReference(NdwOpenDataMeasurementSiteReference measurementSiteReference)
        {
            var measurementSiteReferenceJson = measurementSiteReference.MeasurementSiteReference;
            measurementSiteReferenceJson = measurementSiteReferenceJson.Replace("@id", "id");
            measurementSiteReferenceJson = measurementSiteReferenceJson.Replace("@index", "index");
            measurementSiteReferenceJson = measurementSiteReferenceJson.Replace("@type", "type");

            var measurementSiteReferenceDto = JsonConvert.DeserializeObject<Dto.MeasurementSiteReferenceDto>( measurementSiteReferenceJson );

            var lat = 0D;
            if (Double.TryParse(measurementSiteReferenceDto?.measurementSiteRecord?.measurementSiteLocation?.pointExtension?.openlrExtendedPoint?.openlrPointLocationReference?.openlrPointAlongLine?.openlrLocationReferencePoint?.openlrCoordinate?.latitude, out double l1))
                lat = l1;

            var lon = 0D;
            if (Double.TryParse(measurementSiteReferenceDto?.measurementSiteRecord?.measurementSiteLocation?.pointExtension?.openlrExtendedPoint?.openlrPointLocationReference?.openlrPointAlongLine?.openlrLocationReferencePoint?.openlrCoordinate?.longitude, out double l2))
                lon = l2;

            return lat == 0 && lon == null ? null : new
            {
                Type = "latLonPointGeoReference",
                Lat = lat,
                Lon = lon
            };        
        }
    }
}