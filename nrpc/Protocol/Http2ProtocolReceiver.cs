using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Http2;
using Http2.Hpack;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Tests")]

namespace nRpc.Protocol
{
    class Http2ProtocolReceiver : TransferProtocolReceiver
    {
        private readonly Func<Task> _responseHandler;
        private readonly TcpListener _tcpListener;
        private readonly ConnectionConfiguration _connectionConfigurationBuilder;
        public IStream Stream { get; internal set; }
        public int Port { get; }

        public Http2ProtocolReceiver(Func<Task> serverHandle, int port)
        {
            _responseHandler = serverHandle;
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _tcpListener.Start();
            var settings = Settings.Default;
            settings.MaxConcurrentStreams = 50;

            _connectionConfigurationBuilder =
                new ConnectionConfigurationBuilder(true)
                .UseStreamListener(AcceptIncomingStream)
                .UseSettings(settings)
                .UseHuffmanStrategy(HuffmanStrategy.IfSmaller)
                .Build();
        }
        
        internal Http2ProtocolReceiver()
        {
        }

        private bool AcceptIncomingStream(IStream stream)
        {
            Task.Run(() => HandleIncomingStream(stream));
            return true;
        }

        private async Task HandleIncomingStream(IStream stream)
        {
            try
            {
                Stream = stream;
                await _responseHandler();
            }
            catch (Exception)
            {
                stream.Cancel();
            }
        }

        public async Task Receive()
        {
            var clientSocket = await _tcpListener.AcceptSocketAsync();
            clientSocket.NoDelay = true;
            var wrappedStreams = clientSocket.CreateStreams();
            var http2Con = new Connection(
                _connectionConfigurationBuilder, wrappedStreams.ReadableStream, wrappedStreams.WriteableStream
                );

            var remoteGoAwayTask = http2Con.RemoteGoAwayReason;
            var closeWhenRemoteGoAway = Task.Run(async () =>
            {
                await remoteGoAwayTask;
                await http2Con.GoAwayAsync(ErrorCode.NoError, true);
            });
        }

        public async Task<string> GetRequestFunctionAsync()
        {
            var headers = await Stream.ReadHeadersAsync();
            var path = headers.First(h => h.Name == ":path").Value;
            return path.Replace("/", string.Empty);
        }

        public async Task<byte[]> ReadRequestAsync()
        {
            return await Stream.ReadBytes();
        }

        public async Task SendResponseAsync(IEnumerable<KeyValuePair<string, string>> headerFields, byte[] bytesToWrite)
        {
            ThrowIf.IsNullOrEmpty(nameof(bytesToWrite), bytesToWrite);
            ThrowIf.IsNull(nameof(headerFields), headerFields);
            var responseHeaders = new List<HeaderField>();
            foreach (var header in headerFields)
            {
                responseHeaders.Add(
                    new HeaderField
                    { Name = header.Key, Value = header.Value });
            }

            await Stream.WriteHeadersAsync(responseHeaders, false);
            await Stream.WriteAsync(new ArraySegment<byte>(
                 bytesToWrite), true);
        }
    }
}
