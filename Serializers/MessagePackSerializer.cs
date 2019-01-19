using MessagePack;

namespace nRpc.Serializers
{
    public class MessagePackSerializer : Serializer
    {
        public T Deserialize<T>(byte[] bytes) where T : class, new()
        {
            ThrowIf.IsNull(nameof(bytes), bytes);
            return LZ4MessagePackSerializer.Deserialize<T>(bytes);
        }

        public byte[] Serialize(object obj)
        {
            ThrowIf.IsNull(nameof(obj), obj);
            return LZ4MessagePackSerializer.Serialize(obj);
        }
    }
}