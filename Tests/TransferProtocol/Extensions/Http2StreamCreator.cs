using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Http2;
using Http2.Hpack;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Collections.Generic;

namespace nRpc.Tests.TransferProtocol.Extensions
{
    public static class Http2StreamCreator
    {
        public static readonly HeaderField[] DefaultGetHeaders = new HeaderField[]
        {
            new HeaderField { Name = ":method", Value = "GET" },
            new HeaderField { Name = ":scheme", Value = "http" },
            new HeaderField { Name = ":path", Value = "/AnyFunction" },
            new HeaderField { Name = "abc", Value = "def" },
        };

        public static readonly HeaderField[] DefaultStatusHeaders = new HeaderField[]
        {
            new HeaderField { Name = ":status", Value = "200" },
            new HeaderField { Name = "xyz", Value = "ghi" }
        };

        public struct Result
        {
            public Http2.Hpack.Encoder hEncoder;
            public Connection conn;
            public IStream stream;
        }

        public static async Task<Result> CreateClientConnectionAndStream(
            StreamState state,
            ILoggerProvider loggerProvider,
            IBufferedPipe iPipe, IBufferedPipe oPipe,
            Settings? localSettings = null,
            Settings? remoteSettings = null,
            HuffmanStrategy huffmanStrategy = HuffmanStrategy.Never)
        {
            if (state == StreamState.Idle)
            {
                throw new Exception("Not supported");
            }

            var hEncoder = new Encoder();
            var conn = await Http2ConnectionUtils.BuildEstablishedConnection(
                false, iPipe, oPipe, loggerProvider, null,
                localSettings: localSettings,
                remoteSettings: remoteSettings,
                huffmanStrategy: huffmanStrategy);

            var endOfStream = false;
            if (state == StreamState.HalfClosedLocal ||
                state == StreamState.Closed)
                endOfStream = true;
            var stream = await conn.CreateStreamAsync(
                DefaultGetHeaders, endOfStream: endOfStream);
            await oPipe.ReadAndDiscardHeaders(1u, endOfStream);

            if (state == StreamState.HalfClosedRemote ||
                state == StreamState.Closed)
            {
                var outBuf = new byte[Settings.Default.MaxFrameSize];
                var result = hEncoder.EncodeInto(
                    new ArraySegment<byte>(outBuf),
                    DefaultStatusHeaders);
                await iPipe.WriteFrameHeaderWithTimeout(
                    new FrameHeader
                    {
                        Type = FrameType.Headers,
                        Flags = (byte)(HeadersFrameFlags.EndOfHeaders |
                                       HeadersFrameFlags.EndOfStream),
                        StreamId = 1u,
                        Length = result.UsedBytes,
                    });
                await iPipe.WriteAsync(new ArraySegment<byte>(outBuf, 0, result.UsedBytes));
                var readHeadersTask = stream.ReadHeadersAsync();
                var combined = await Task.WhenAny(readHeadersTask, Task.Delay(
                    Http2ReadableStreamTestExtensions.ReadTimeout));
                Assert.True(readHeadersTask == combined, "Expected to receive headers");
                var headers = await readHeadersTask;
                Assert.True(headers.SequenceEqual(DefaultStatusHeaders));

            }
            else if (state == StreamState.Reset)
            {
                await iPipe.WriteResetStream(1u, ErrorCode.Cancel);
            }

            return new Result
            {
                hEncoder = hEncoder,
                conn = conn,
                stream = stream,
            };
        }

        public static async Task<Result> CreateServerConnectionAndStream(HeaderField[] getHeaders,
               StreamState state,
               ILoggerProvider loggerProvider,
               IBufferedPipe iPipe, IBufferedPipe oPipe,
               Settings? localSettings = null,
               Settings? remoteSettings = null,
               HuffmanStrategy huffmanStrategy = HuffmanStrategy.Never)
        {
            IStream stream = null;
            var handlerDone = new SemaphoreSlim(0);
            if (state == StreamState.Idle)
            {
                throw new Exception("Not supported");
            }

            Func<IStream, bool> listener = (s) =>
            {
                Task.Run(async () =>
                {
                    stream = s;
                    try
                    {
                        await s.ReadHeadersAsync();
                        if (state == StreamState.Reset)
                        {
                            s.Cancel();
                            return;
                        }

                        if (state == StreamState.HalfClosedRemote ||
                            state == StreamState.Closed)
                        {
                            await s.ReadAllToArrayWithTimeout();
                        }

                        if (state == StreamState.HalfClosedLocal ||
                            state == StreamState.Closed)
                        {
                            await s.WriteHeadersAsync(
                                DefaultStatusHeaders, true);
                        }
                    }
                    finally
                    {
                        handlerDone.Release();
                    }
                });
                return true;
            };
            var conn = await Http2ConnectionUtils.BuildEstablishedConnection(
                true, iPipe, oPipe, loggerProvider, listener,
                localSettings: localSettings,
                remoteSettings: remoteSettings,
                huffmanStrategy: huffmanStrategy);
            var hEncoder = new Encoder();

            await iPipe.WriteHeaders(
                hEncoder, 1, false, getHeaders);

            if (state == StreamState.HalfClosedRemote ||
                state == StreamState.Closed)
            {
                await iPipe.WriteData(1u, 0, endOfStream: true);
            }

            var ok = await handlerDone.WaitAsync(
                Http2ReadableStreamTestExtensions.ReadTimeout);
            if (!ok) throw new Exception("Stream handler did not finish");

            if (state == StreamState.HalfClosedLocal ||
                state == StreamState.Closed)
            {
                // Consume the sent headers and data
                await oPipe.ReadAndDiscardHeaders(1u, true);
            }
            else if (state == StreamState.Reset)
            {
                // Consume the sent reset frame
                await oPipe.AssertResetStreamReception(1, ErrorCode.Cancel);
            }

            return new Result
            {
                conn = conn,
                stream = stream,
                hEncoder = hEncoder,
            };
        }

        public static async Task<IStream> GetServerStream(IEnumerable<KeyValuePair<string, string>> headerFields, Http2BufferedPipe inPipe, Http2BufferedPipe outPipe)
        {
            var getHeaders = new List<HeaderField>();
            foreach (var header in headerFields)
            {
                getHeaders.Add(
                    new HeaderField
                    { Name = header.Key, Value = header.Value });
            }
            var resultStream = await CreateServerConnectionAndStream(getHeaders.ToArray(),
                StreamState.Open, null, inPipe, outPipe);
            await resultStream.stream.WriteHeadersAsync(DefaultStatusHeaders, false);
            await outPipe.ReadAndDiscardHeaders(1u, false);
            return resultStream.stream;
        }

        public static async Task<IStream> GetClientStream(Http2BufferedPipe inPipe, Http2BufferedPipe outPipe)
        {
            var resultStream = await CreateClientConnectionAndStream(
                StreamState.Open, null, inPipe, outPipe);
            await inPipe.WriteHeaders(resultStream.hEncoder, 1u, false, DefaultStatusHeaders);
            return resultStream.stream;
        }
    }
}
