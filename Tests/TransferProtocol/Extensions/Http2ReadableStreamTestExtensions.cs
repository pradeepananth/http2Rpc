/*
 *   Taken from http2dotnet library. 
 *   Source: https://github.com/Matthias247/http2dotnet/blob/master/Http2Tests/ReadableStreamTestExtensions.cs
 */
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Http2;

namespace nRpc.Tests.TransferProtocol.Extensions
{
    public static class Http2ReadableStreamTestExtensions
    {
        public const int ReadTimeout = 350;

        public static async Task<StreamReadResult> ReadWithTimeout(
            this IReadableByteStream stream, ArraySegment<byte> buf)
        {
            var readTask = stream.ReadAsync(buf).AsTask();
            var timeoutTask = Task.Delay(ReadTimeout);
            var combined = Task.WhenAny(new Task[] { readTask, timeoutTask });
            var done = await combined;
            if (done == readTask)
            {
                return readTask.Result;
            }
            throw new TimeoutException();
        }

        public static async Task ReadAllWithTimeout(
            this IReadableByteStream stream, ArraySegment<byte> buf)
        {
            var readTask = stream.ReadAll(buf).AsTask();
            var timeoutTask = Task.Delay(ReadTimeout);
            var combined = Task.WhenAny(new Task[] { readTask, timeoutTask });
            var done = await combined;
            if (done == readTask)
            {
                await readTask;
                return;
            }
            throw new TimeoutException();
        }

        public static async Task<byte[]> ReadAllToArray(
            this IReadableByteStream stream)
        {
            var totalBuf = new MemoryStream();
            var buf = new byte[16 * 1024];
            while (true)
            {
                var res = await stream.ReadAsync(new ArraySegment<byte>(buf));
                if (res.BytesRead > 0)
                {
                    totalBuf.Write(buf, 0, res.BytesRead);
                }
                if (res.EndOfStream)
                {
                    return totalBuf.ToArray();
                }
            }
        }

        public static async Task<byte[]> ReadAllToArrayWithTimeout(
            this IReadableByteStream stream)
        {
            var readTask = stream.ReadAllToArray();
            var timeoutTask = Task.Delay(ReadTimeout);
            var combined = Task.WhenAny(new Task[] { readTask, timeoutTask });
            var done = await combined;
            if (done == readTask)
            {
                return readTask.Result;
            }
            throw new TimeoutException();
        }

        public static async Task<FrameHeader> ReadFrameHeaderWithTimeout(
            this IReadableByteStream stream)
        {
            var headerSpace = new byte[FrameHeader.HeaderSize];
            var readTask = FrameHeader.ReceiveAsync(stream, headerSpace).AsTask();
            var timeoutTask = Task.Delay(5000);
            var combined = Task.WhenAny(new Task[] { readTask, timeoutTask });
            var done = await combined;
            if (done == readTask)
            {
                return readTask.Result;
            }
            throw new TimeoutException();
        }

        public static async Task ReadAndDiscardPreface(
            this IReadableByteStream stream)
        {
            var b = new byte[ClientPreface.Length];
            await stream.ReadAllWithTimeout(new ArraySegment<byte>(b));
        }

        public static async Task ReadAndDiscardSettings(
            this IReadableByteStream stream)
        {
            var header = await stream.ReadFrameHeaderWithTimeout();
            Assert.Equal(FrameType.Settings, header.Type);
            Assert.InRange(header.Length, 0, 256);
            await stream.ReadAllWithTimeout(
                new ArraySegment<byte>(new byte[header.Length]));
        }

        public static async Task ReadAndDiscardHeaders(
            this IReadableByteStream stream,
            uint expectedStreamId,
            bool expectEndOfStream)
        {
            var header = await stream.ReadFrameHeaderWithTimeout();
            Assert.Equal(FrameType.Headers, header.Type);
            Assert.Equal(expectedStreamId, header.StreamId);
            var isEndOfStream = (header.Flags & (byte)HeadersFrameFlags.EndOfStream) != 0;
            Assert.Equal(expectEndOfStream, isEndOfStream);
            var hbuf = new ArraySegment<byte>(new byte[header.Length]);
            await stream.ReadAllWithTimeout(hbuf);
        }

        public static async Task AssertSettingsAck(
            this IReadableByteStream stream)
        {
            var header = await stream.ReadFrameHeaderWithTimeout();
            Assert.Equal(FrameType.Settings, header.Type);
            Assert.Equal(0, header.Length);
            Assert.Equal((byte)SettingsFrameFlags.Ack, header.Flags);
            Assert.Equal(0u, header.StreamId);
        }

        public static async Task AssertResetStreamReception(
            this IReadableByteStream stream,
            uint expectedStreamId,
            ErrorCode expectedErrorCode)
        {
            var hdr = await stream.ReadFrameHeaderWithTimeout();
            Assert.Equal(FrameType.ResetStream, hdr.Type);
            Assert.Equal(expectedStreamId, hdr.StreamId);
            Assert.Equal(0, hdr.Flags);
            Assert.Equal(ResetFrameData.Size, hdr.Length);
            var resetBytes = new byte[hdr.Length];
            await stream.ReadAllWithTimeout(new ArraySegment<byte>(resetBytes));
            var resetData = ResetFrameData.DecodeFrom(new ArraySegment<byte>(resetBytes));
            Assert.Equal(expectedErrorCode, resetData.ErrorCode);
        }
    }
}