namespace nRpc.Protocol
{
    public static class TransferProtocols
    {
        public static TransferProtocol Http2 => new Http2ProtocolProvider();
    }   
}
