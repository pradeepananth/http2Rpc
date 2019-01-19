using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace nRpc.Serializers
{
    public class BinarySerializer : Serializer
    {
        public T Deserialize<T>(byte[] bytes) where T : class, new()
        {
            ThrowIf.IsNull(nameof(bytes), bytes);
            var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            stream.Write(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(stream);
        }

        public byte[] Serialize(object obj)
        {
            ThrowIf.IsNull(nameof(obj), obj);
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }
    }
}