using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace nRpc.Tests
{
    public class TestSerializer : nRpc.Serializer
    {
        public byte[] Serialize(Object obj)
        {
            if (obj == null)
            return null;
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }   

        public T Deserialize<T>(byte[] arrBytes)
            where T : class, new()
        {
            var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            stream.Write(arrBytes, 0, arrBytes.Length);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(stream);
        }
    }
}