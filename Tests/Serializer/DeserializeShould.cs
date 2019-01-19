using System;
using System.Collections.Generic;
using FluentAssertions;
using nRpc.Serializers;
using nRpc.Tests.Data;
using Xunit;

namespace nRpc.Tests.Serializer
{
    public class DeserializeShould
    {
        [Theory]
        [MemberData(nameof(Data))]
        public void ThrowArgumentNullException_WhenObjectIsNull(nRpc.Serializer serializer)
        {
            Action action = () => serializer.Deserialize<object>(null);
            action.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void ReturnType(nRpc.Serializer serializer)
        {
            var serialized = serializer.Serialize(new AnyObject());
            var result = serializer.Deserialize<AnyObject>(serialized);
            result.Should().BeOfType<AnyObject>();
            result.Should().NotBeNull();
        }

        public static IEnumerable<object[]> Data => new List<object[]>
                                                            {
                                                                new object[] { new BinarySerializer() },
                                                                new object[] { new nRpc.Serializers.MessagePackSerializer() }
                                                            };
    }     
}