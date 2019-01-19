namespace nRpc
{
    public interface Serializer
    {
        byte[] Serialize(object obj);
        T Deserialize<T>(byte[] bytes) where T : class, new();
    }
}