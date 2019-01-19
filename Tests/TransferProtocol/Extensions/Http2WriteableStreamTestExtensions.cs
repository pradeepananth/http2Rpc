/*
 *   Taken from http2dotnet library. 
 *   Source: https://github.com/Matthias247/http2dotnet/blob/master/Http2Tests/WriteableStreamTestExtensions.cs
 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Http2;
using Http2.Hpack;

namespace nRpc.Tests.TransferProtocol.Extensions
{
    public static class Http2WriteableStreamTestExtensions
    {
        public const int WriteTimeout = 200;

        public static async Task WriteFrameHeader(
            this IWriteAndCloseableByteStream stream,
            FrameHeader fh)
        {
            var headerBytes = new byte[FrameHeader.HeaderSize];
            fh.EncodeInto(new ArraySegment<byte>(headerBytes));
            await stream.WriteAsync(new ArraySegment<byte>(headerBytes));
        }

        public static async Task WriteFrameHeaderWithTimeout(
            this IWriteAndCloseableByteStream stream,
            FrameHeader fh)
        {
            var writeTask = stream.WriteFrameHeader(fh);
            var timeoutTask = Task.Delay(WriteTimeout);
            var combined = Task.WhenAny(new Task[] { writeTask, timeoutTask });
            var done = await combined;
            if (done == writeTask)
            {
                await writeTask;
                return;
            }
            throw new TimeoutException();
        }

        public static async Task WriteWithTimeout(
            this IWriteAndCloseableByteStream stream, ArraySegment<byte> buf)
        {
            var writeTask = stream.WriteAsync(buf);
            var timeoutTask = Task.Delay(WriteTimeout);
            var combined = Task.WhenAny(new Task[] { writeTask, timeoutTask });
            var done = await combined;
            if (done == writeTask)
            {
                await writeTask;
                return;
            }
            throw new TimeoutException();
        }

        public static async Task WriteSettings(
            this IWriteAndCloseableByteStream stream, Settings settings)
        {
            var settingsData = new byte[settings.RequiredSize];
            var fh = new FrameHeader
            {
                Type = FrameType.Settings,
                Length = settingsData.Length,
                Flags = 0,
                StreamId = 0,
            };
            settings.EncodeInto(new ArraySegment<byte>(settingsData));
            await stream.WriteFrameHeader(fh);
            await stream.WriteAsync(new ArraySegment<byte>(settingsData));
        }

        public static async Task WriteSettingsAck(
            this IWriteAndCloseableByteStream stream)
        {
            var fh = new FrameHeader
            {
                Type = FrameType.Settings,
                Length = 0,
                Flags = (byte)SettingsFrameFlags.Ack,
                StreamId = 0,
            };
            await stream.WriteFrameHeader(fh);
        }
        
        public static async Task WriteResetStream(
            this IWriteAndCloseableByteStream stream, uint streamId, ErrorCode errc)
        {
            var fh = new FrameHeader
            {
                Type = FrameType.ResetStream,
                Flags = 0,
                Length = ResetFrameData.Size,
                StreamId = streamId,
            };
            var data = new ResetFrameData
            {
                ErrorCode = errc,
            };
            var dataBytes = new byte[ResetFrameData.Size];
            data.EncodeInto(new ArraySegment<byte>(dataBytes));
            await stream.WriteFrameHeader(fh);
            await stream.WriteAsync(new ArraySegment<byte>(dataBytes));
        }
        
        public static async Task WriteHeaders(
            this IWriteAndCloseableByteStream stream,
            Encoder encoder,
            uint streamId,
            bool endOfStream,
            IEnumerable<HeaderField> headers,
            bool endOfHeaders = true)
        {
            var outBuf = new byte[Settings.Default.MaxFrameSize];
            var result = encoder.EncodeInto(new ArraySegment<byte>(outBuf), headers);
            // Check if all headers could be encoded
            if (result.FieldCount != headers.Count())
            {
                throw new Exception("Could not encode all headers");
            }

            byte flags = 0;
            if (endOfHeaders) flags |= (byte)(HeadersFrameFlags.EndOfHeaders);
            if (endOfStream) flags |= (byte)HeadersFrameFlags.EndOfStream;
            var fh = new FrameHeader
            {
                Type = FrameType.Headers,
                Length = result.UsedBytes,
                Flags = flags,
                StreamId = streamId,
            };
            await stream.WriteFrameHeader(fh);
            await stream.WriteAsync(
                new ArraySegment<byte>(outBuf, 0, result.UsedBytes));
        }

        public static async Task WriteData(
            this IWriteAndCloseableByteStream stream,
            uint streamId,
            int length,
            int? padLen = null,
            bool endOfStream = false)
        {
            byte flags = 0;
            if (endOfStream) flags |= (byte)DataFrameFlags.EndOfStream;
            if (padLen.HasValue) flags |= (byte)DataFrameFlags.Padded;

            var dataLen = length;
            if (padLen.HasValue) dataLen += 1 + padLen.Value;
            var data = new byte[dataLen];
            if (padLen.HasValue) data[0] = (byte)padLen.Value;

            var fh = new FrameHeader
            {
                Type = FrameType.Data,
                Length = dataLen,
                Flags = flags,
                StreamId = streamId,
            };
            await stream.WriteFrameHeader(fh);

            if (dataLen != 0)
            {
                await stream.WriteWithTimeout(new ArraySegment<byte>(data));
            }
        }        
    }
}