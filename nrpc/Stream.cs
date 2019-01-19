using System.Collections.Generic;
using System.Threading.Tasks;

namespace nRpc
{
    public interface Stream
    {       
        Task<byte[]> ReadToArrayAsyncWithTimeOut();
        Task WriteToStreamAsync(byte[] bytesToWrite);
        Task WriteToStreamWithHeadersAsync(IEnumerable<KeyValuePair<string, string>> headerFields, byte[] bytesToWrite);
    }
}
