using System;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Http;
using OTM.DevLOG.Extensions;

namespace OTM.DevLOG.ApplicationServices
{
	public class NdwIngestorAppService
		: DevLOGAppService, INdwIngestorAppService
	{
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NdwIngestorAppService( IHttpContextAccessor httpContextAccessor )
        {
            _httpContextAccessor = httpContextAccessor;
        }


        public async Task ActualTrafficInfo(  )
		{
            var httpRequest = _httpContextAccessor.HttpContext.Request;
            System.Diagnostics.Debug.Assert(httpRequest != null);

            var requestBody = await httpRequest.GetRawBodyAsync(System.Text.Encoding.UTF8);
            if (requestBody != null && requestBody.Length > 0)
                this.PublishToPubSub("devlog-v1-ncis-actual-traffic-info", requestBody);


            var responseBody = this.GenerateResponseBody();

            _httpContextAccessor.HttpContext.Response.Headers.Add("content-type", "application/xml");
            await _httpContextAccessor.HttpContext.Response.Body.WriteAsync(responseBody, 0, responseBody.Length);
        }



        private void PublishToPubSub(string topic, byte[] data)
        {
            var pubsubMessage = new PubsubMessage
            {
                Data = Google.Protobuf.ByteString.CopyFrom(data)
            };

            const string projectId = "filogic-tms";

            var topicName = new TopicName(projectId, topic);
            var publisherServiceApiClient = PublisherServiceApiClient.Create();

            publisherServiceApiClient.Publish(topicName, new[] { pubsubMessage });
        }


        private byte[] GenerateResponseBody()
        {
            var responseBody = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><SOAP:Envelope xmlns:SOAP=\"http://schemas.xmlsoap.org/soap/envelope/\"><SOAP:Body><d2LogicalModel xmlns=\"http://datex2.eu/schema/2/2_0\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" modelBaseVersion=\"2\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><exchange><response>acknowledge</response><supplierIdentification><country>nl</country><nationalIdentifier>FiLogic OpenTMS</nationalIdentifier></supplierIdentification></exchange></d2LogicalModel></SOAP:Body></SOAP:Envelope>";
            return System.Text.Encoding.UTF8.GetBytes(responseBody);
        }
    }
}


