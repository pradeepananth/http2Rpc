using Http2;
using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace nRpc.Protocol
{
    public static class Http2StreamExtensions
    {
        private const int BufferMaxLength = 64 * 1024;
        private const int ReadBufferSize = 16 * 1024;
        private const int ReadTimeout = 1000;

        public static async Task<byte[]> ReadBytes(this IStream stream)
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

        static async Task<byte[]> ReadAllToArray(
            this IReadableByteStream stream)
        {
            var readBuffer = Buffers.Pool.Rent(ReadBufferSize);
            using (var memoryStream = new MemoryStream())
            {
                while (true)
                {
                    var streamReadResult = await stream.ReadAsync(new ArraySegment<byte>(readBuffer));
                    if (streamReadResult.BytesRead > 0)
                    {
                        memoryStream.Write(readBuffer, 0, streamReadResult.BytesRead);
                    }
                    if (streamReadResult.EndOfStream)
                    {
                        return memoryStream.ToArray();
                    }
                }
            }
        }        

        public static class Buffers
        {
            public static ArrayPool<byte> Pool = ArrayPool<byte>.Create(BufferMaxLength, 200);
        }
    }
}
