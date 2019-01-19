/*
 *   Taken from http2dotnet library. 
 *   Source: https://github.com/Matthias247/http2dotnet/blob/master/Http2Tests/BufferedPipe.cs    
 */
using System;
using System.Threading;
using System.Threading.Tasks;
using Http2;
using Http2.Internal;

namespace nRpc.Tests.TransferProtocol.Extensions
{
    /// <summary>
    /// A pipe, which connects two tasks.
    /// On task uses the reading side an can read data from the pipe,
    /// the other task uses the writing side and can write data or close the pipe
    /// </summary>
    public interface IBufferedPipe
        : IReadableByteStream, IWriteAndCloseableByteStream
    {
    }

    public class Http2BufferedPipe
        : IWriteableByteStream, IReadableByteStream,
          ICloseableByteStream, IWriteAndCloseableByteStream,
          IBufferedPipe
    {
        public byte[] Buffer;
        public int Written = 0;
        public bool IsClosed = false;
        AsyncManualResetEvent canRead = new AsyncManualResetEvent(false);
        AsyncManualResetEvent canWrite = new AsyncManualResetEvent(true);
        object mutex = new object();
        SemaphoreSlim writeLock = new SemaphoreSlim(1);

        public Http2BufferedPipe(int bufferSize)
        {
            if (bufferSize < 1) throw new ArgumentException(nameof(bufferSize));
            Buffer = new byte[bufferSize];
        }

        public async ValueTask<StreamReadResult> ReadAsync(ArraySegment<byte> buffer)
        {
            await canRead;

            var wakeupWriter = false;
            var toCopy = 0;

            lock (mutex)
            {
                var available = Written;
                if (available == 0)
                {
                    return new StreamReadResult
                    {
                        BytesRead = 0,
                        EndOfStream = true,
                    };
                }

                toCopy = Math.Min(available, buffer.Count);
                Array.Copy(Buffer, 0, buffer.Array, buffer.Offset, toCopy);

                if (toCopy == Written)
                {
                    Written = 0;
                    if (!IsClosed)
                    {
                        canRead.Reset();
                    }
                }
                else
                {
                    var remaining = Written - toCopy;
                    Array.Copy(Buffer, toCopy, Buffer, 0, remaining);
                    Written -= toCopy;
                }
                wakeupWriter = Written != Buffer.Length;
            }

            if (wakeupWriter)
            {
                canWrite.Set();
            }

            return new StreamReadResult
            {
                BytesRead = toCopy,
                EndOfStream = false,
            };
        }

        public async Task WriteAsync(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null) throw new ArgumentNullException(nameof(buffer));

            var offset = buffer.Offset;
            var count = buffer.Count;

            await writeLock.WaitAsync();
            try
            {

                while (count > 0)
                {
                    var segment = new ArraySegment<byte>(buffer.Array, offset, count);
                    var written = await WriteOnce(segment);
                    offset += written;
                    count -= written;
                }
            }
            finally
            {
                writeLock.Release();
            }
        }

        private async ValueTask<int> WriteOnce(ArraySegment<byte> buffer)
        {
            await canWrite;
            var writeAmount = 0;
            lock (mutex)
            {
                if (IsClosed)
                {
                    throw new Exception("Write on closed stream");
                }
                var free = Buffer.Length - Written;
                writeAmount = Math.Min(free, buffer.Count);
                Array.Copy(buffer.Array, buffer.Offset, Buffer, Written, writeAmount);
                Written += writeAmount;

                if (Written == Buffer.Length)
                {
                    canWrite.Reset();
                }
            }

            if (writeAmount > 0)
            {
                canRead.Set();
            }

            return writeAmount;
        }

        public Task CloseAsync()
        {
            lock (mutex)
            {
                IsClosed = true;
            }

            canRead.Set();
            canWrite.Set();
            return Task.CompletedTask;
        }       

        public static async Task WriteData(byte[] data, Http2BufferedPipe inPipe)
        {
            var fh = new FrameHeader
            {
                Type = FrameType.Data,
                StreamId = 1,
                Flags = (byte)DataFrameFlags.EndOfStream,
                Length = data.Length,
            };
            await inPipe.WriteFrameHeaderWithTimeout(fh);
            await inPipe.WriteWithTimeout(new ArraySegment<byte>(data));
        }
    }
}
