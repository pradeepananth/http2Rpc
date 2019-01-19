using System.Collections.Generic;
using System.Threading.Tasks;

namespace nRpc.Protocol
{
    public interface TransferProtocolReceiver
    {
        Task Receive();
        Task<string> GetRequestFunctionAsync();
        Task<byte[]> ReadRequestAsync();
        Task SendResponseAsync(IEnumerable<KeyValuePair<string, string>> headerFields, byte[] content);
    }
}