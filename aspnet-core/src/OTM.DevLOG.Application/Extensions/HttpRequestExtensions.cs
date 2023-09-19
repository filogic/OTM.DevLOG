using System;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using static Google.Rpc.Context.AttributeContext.Types;

namespace OTM.DevLOG.Extensions
{
	public static class HttpRequestExtensions
	{
        public static async Task<byte[]> GetRawBodyAsync(this HttpRequest httpRequest, Encoding encoding )
        {
            if (!httpRequest.Body.CanSeek)
                httpRequest.EnableBuffering();

            using (var memoryStream = new MemoryStream())
            {
                // Read the request body into the memory stream
                await httpRequest.Body.CopyToAsync(memoryStream);

                memoryStream.Position = 0;

                var contentEncodingHeader = "content-encoding";
                var contentEncodingOptions = new string[] { "gzip", "deflate" };
                if (httpRequest.Headers.ContainsKey(contentEncodingHeader) && httpRequest.Headers[contentEncodingHeader].Any(contentEncoding => contentEncodingOptions.Contains(contentEncoding)))
                {
                    // Content is encoded.  Decode and return 
                    var contentEncoding = httpRequest.Headers[contentEncodingHeader].First();
                    using (var decodingStream = contentEncoding == "gzip" ? (Stream)new GZipStream(memoryStream, CompressionMode.Decompress, true) : (Stream)new DeflateStream(memoryStream, CompressionMode.Decompress, true))
                    {
                        using (var reader = new MemoryStream())
                        {
                            await decodingStream.CopyToAsync(reader);
                            return reader.ToArray();
                        }
                    }
                }


                // Content is not encoded
                return memoryStream.ToArray();
            }
        }
    }
}

