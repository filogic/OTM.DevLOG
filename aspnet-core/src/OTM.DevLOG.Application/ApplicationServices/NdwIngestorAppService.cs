using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OTM.DevLOG.Extensions;

namespace OTM.DevLOG.ApplicationServices
{
	public class NdwIngestorAppService
		: DevLOGAppService, INdwIngestorAppService
	{
        const string _gcpActualTrafficInfoSubscription = "devlog-v1-ncis-actual-traffic-info-sub";
        const int _gcpMaxPubSubBodyLength = 10000000;


        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NdwIngestorAppService(ILogger<NdwIngestorAppService> logger, IHttpContextAccessor httpContextAccessor )
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }


        public async Task HelloPub()
        {
            await Task.Run(() =>
            {
                var helloWorld = System.Text.Encoding.UTF8.GetBytes("Hello pub !");
                this.PublishToPubSub(_gcpActualTrafficInfoSubscription, helloWorld);
            });
        }


        public async Task<int> HelloSub()
        {
            try
            {
                // Create new instance of subscriberServiceApiClient as well as subscriptionName
                var subscriberServiceApiClient = SubscriberServiceApiClient.Create();
                var subscriptionName = new SubscriptionName("devlog-prod", _gcpActualTrafficInfoSubscription);

                // Pull messages from the subscription. This will wait for some time if no new messages have been
                // published yet.
                var pullResponse = await subscriberServiceApiClient.PullAsync(subscriptionName, maxMessages: 50);
                return pullResponse.ReceivedMessages.Count;
            }
            catch (Exception) { /* Handling not a single thing */ }
            return -1;
        }


        public async Task ActualTrafficInfo(  )
		{
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var httpRequest = _httpContextAccessor.HttpContext.Request;
            System.Diagnostics.Debug.Assert(httpRequest != null);

            // Read request body from request stream
            var requestBody = await httpRequest.GetRawBodyAsync(System.Text.Encoding.UTF8);
            if (requestBody == null)
                return;

            var t1 = sw.ElapsedMilliseconds;

            // Always compress before forwarding to pub/sub.
            // The maximum size gcp pub/sub accepts is 10MB, so by compressing
            // more messages are forwarded
            requestBody = this.Compress(requestBody);

            var t2 = sw.ElapsedMilliseconds;


            // Forward to gcp bup/sub
            if (requestBody != null && requestBody.Length > 0 && requestBody.Length < _gcpMaxPubSubBodyLength )
                this.PublishToPubSub("devlog-v1-ncis-actual-traffic-info", requestBody);

            var t3 = sw.ElapsedMilliseconds;


            var responseBody = this.GenerateResponseBody();

            _httpContextAccessor.HttpContext.Response.Headers.Add("content-type", "text/xml");
            await _httpContextAccessor.HttpContext.Response.Body.WriteAsync(responseBody, 0, responseBody.Length);

            this.Logger.LogInformation("ActualTrafficInfo timestamps: t1={0}; t2={1}; t3={2}; t4={3}", t1,t2,t3,sw.ElapsedMilliseconds);
        }


        private void PublishToPubSub(string topic, byte[] data)
        {
            var pubsubMessage = new PubsubMessage
            {
                Data = Google.Protobuf.ByteString.CopyFrom(data)
            };

            const string projectId = "devlog-prod";

            var topicName = new TopicName(projectId, topic);
            var publisherServiceApiClient = PublisherServiceApiClient.Create();

            Console.WriteLine("projectId: " + projectId);
            Console.WriteLine("topic: " + topic);

            publisherServiceApiClient.Publish(topicName, new[] { pubsubMessage });

            _logger.LogInformation("Published {0} bytes to {1}", data.Length, topic );
        }


        private byte[] Compress(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData must be non-null");

            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(inputData, 0, inputData.Length);
            }
            return output.ToArray();
        }


        private byte[] GenerateResponseBody()
        {
            var responseBody = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><SOAP:Envelope xmlns:SOAP=\"http://schemas.xmlsoap.org/soap/envelope/\"><SOAP:Body><d2LogicalModel xmlns=\"http://datex2.eu/schema/2/2_0\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" modelBaseVersion=\"2\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><exchange><response>acknowledge</response><supplierIdentification><country>nl</country><nationalIdentifier>FiLogic OpenTMS</nationalIdentifier></supplierIdentification></exchange></d2LogicalModel></SOAP:Body></SOAP:Envelope>";
            return System.Text.Encoding.UTF8.GetBytes(responseBody);
        }
    }
}
