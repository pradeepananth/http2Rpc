using nRpc.Protocol;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Tests")]

namespace nRpc.Server
{    
    //TODO: Add logger provision
    //TODO: get interface mappings
    //TODO: error handling
    //TODO: Server load balancing, VIP mappers
    //TODO: Serialization/deserialization
    public class Server
    {
        internal readonly TransferProtocolReceiver _protocolReceiver;

        public Server(TransferProtocol transferprotocol, int port)
        {
            _protocolReceiver = transferprotocol.GetReceiverProtocol(HandleIncomingStream, port);
        }

        internal Server(TransferProtocolReceiver protocolReceiver)
        {
            _protocolReceiver = protocolReceiver;
        }

        internal async Task HandleIncomingStream()
        {
            var requestBytes = await _protocolReceiver.ReadRequestAsync();
            var functionName = await _protocolReceiver.GetRequestFunctionAsync();

            //TODO: replace with right server actions (below for sample only)
            //Console.WriteLine($"Function to call: {functionName}");
            //Console.WriteLine($"req message: {Encoding.ASCII.GetString(requestBytes)}");
            byte[] responseBytes = Encoding.ASCII.GetBytes("hello world");
            var headers = new Dictionary<string, string>
            {
                {":status", "200" },
                {"info", "pass" }
            };
            await _protocolReceiver.SendResponseAsync(headers, responseBytes);
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            //TODO: Server keep alive and running container to be implemented in Host
            if (cancellationToken.IsCancellationRequested) return;
            await _protocolReceiver.Receive();                        
        }
    }
}
