using System;
using System.Threading.Tasks;

namespace nRpc.Protocol
{
    public interface TransferProtocol
    {        
        TransferProtocolReceiver GetReceiverProtocol(Func<Task> serverHandle, int port);
        TransferProtocolSender GetSenderProtocol(string host);
    }
}
