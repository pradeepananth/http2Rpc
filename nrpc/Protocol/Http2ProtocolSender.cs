using System;
using System.Threading.Tasks;

namespace nRpc.Protocol
{
    public class Http2ProtocolSender : TransferProtocolSender
    {
        private Http2Client _httpClient;        

        public Http2ProtocolSender(Http2Client httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<byte[]> Send(string uri, byte[] content)
        {
            ThrowIf.IsNullOrEmpty(nameof(uri), uri);
            ThrowIf.IsNullOrEmpty(nameof(content), content);
            // TODO: Add error cases
            var response = await _httpClient.SendAsync(new Uri(uri), content);
            return response;
        }
    }
}