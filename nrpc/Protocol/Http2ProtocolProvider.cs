using System;
using System.Threading.Tasks;

namespace nRpc.Protocol
{
    public class Http2ProtocolProvider : TransferProtocol
    {
        public TransferProtocolReceiver GetReceiverProtocol(Func<Task> serverHandle, int port)
        {
            return new Http2ProtocolReceiver(serverHandle, port);
        }

        public TransferProtocolSender GetSenderProtocol(string host)
        {
            return new Http2ProtocolSender(new Http2Client(host));
        }
    }
}
