using nRpc.Protocol;
using System.Net.Http;
using System.Threading.Tasks;

namespace nRpc
{
    public class Client
    {
        //TODO: review what should be public and what should be internal   
        public Client(string hostUri, TransferProtocolSender protocol, Serializer serializer)
        {
            Host = hostUri;
            Serializer = serializer;
            TransferProtocol = protocol;
        }

        public string Host { get; }
        public Serializer Serializer { get; private set; }
        public TransferProtocolSender TransferProtocol { get; private set; }

        public async Task<TResponse> Call<TRequest, TResponse>(Procedure<TRequest, TResponse> function, TRequest request)
            where TRequest : class
            where TResponse : class, new()
        {
            ThrowIf.IsNull(nameof(function), function);
            ThrowIf.IsNull(nameof(request), request);

            string uri = $"{Host}/{function.Name}";
            var response = await TransferProtocol.Send(uri, Serializer.Serialize(request));
            // TODO: Handle the no reponse scenario
            if(response.Length == 0) return new TResponse();
            return Serializer.Deserialize<TResponse>(response);       
        }
    }
}