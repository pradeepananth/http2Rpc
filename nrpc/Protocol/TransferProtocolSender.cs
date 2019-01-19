using System.Threading.Tasks;

namespace nRpc.Protocol
{
    public interface TransferProtocolSender
    {
        Task<byte[]> Send(string uri, byte[] request);
    }
}