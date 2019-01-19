using System;
using Http2;
using Http2.Hpack;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace nRpc.Protocol
{
    public class Http2Client : IDisposable
    {
        private const int ResponseBodySizePerRequest = 8192;
        private bool disposed = false;

        public Connection Connection { get; private set; }

        public string Host { get; }    

        public Http2Client(string host)
        {
            ThrowIf.IsNullOrEmpty(nameof(host), host);
            Host = host;
        }

        public virtual async Task<byte[]> SendAsync(Uri uri, byte[] requestMessage)
        {
            //TODO: Add error cases    
            await CreateConnectionIfNotExists(uri);
            var response = await PerformRequest(Connection, uri, requestMessage);
            return response;
        }

        private async Task CreateConnectionIfNotExists(Uri uri)
        {
            if (Connection == null) Connection = await CreateDirectConnection(new Uri(Host));
        }

        private async Task<Connection> CreateDirectConnection(Uri uri)
        {            
            var config =
                new ConnectionConfigurationBuilder(false)
                .UseSettings(Settings.Default)
                .UseHuffmanStrategy(HuffmanStrategy.IfSmaller)
                .Build();
            
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(uri.Host, uri.Port);
            tcpClient.Client.NoDelay = true;
            var wrappedStreams = tcpClient.Client.CreateStreams();
            
            return new Connection(
                config, wrappedStreams.ReadableStream, wrappedStreams.WriteableStream);
        }

        private async Task<byte[]> PerformRequest(Connection conn, Uri uri, byte[] requestMessage)
        {
            var headers = new HeaderField[]
            {
                new HeaderField { Name = ":method", Value = "POST" },
                new HeaderField { Name = ":scheme", Value = uri.Scheme.ToLowerInvariant() },
                new HeaderField { Name = ":path", Value = uri.PathAndQuery},
                new HeaderField { Name = ":authority", Value = uri.Host + ":" + uri.Port}
            };
            var stream = await conn.CreateStreamAsync(headers, false);            
            await stream.WriteAsync(new ArraySegment<byte>(
               requestMessage), true);
            return await stream.ReadBytes();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                Connection.GoAwayAsync(ErrorCode.NoError, true).GetAwaiter().GetResult();
            }

            disposed = true;
        }
    }
}
