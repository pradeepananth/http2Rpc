using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace nRpc.Protocol
{
    public class HttpClientProtocol : TransferProtocolSender
    {
        private HttpClient _httpClient;

        public HttpClientProtocol(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<byte[]> Send(string uri, byte[] content)
        {
            ThrowIf.IsNullOrEmpty(nameof(uri), uri);
            ThrowIf.IsNullOrEmpty(nameof(content), content);
            // TODO: Add error cases
            var response = await _httpClient.SendAsync(new HttpRequestMessage
            {
                RequestUri = new Uri(uri),
                Content = new ByteArrayContent(content)
            });

            if (response.Content == null) return new byte[0];
            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}