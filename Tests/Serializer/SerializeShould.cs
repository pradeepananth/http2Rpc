using System;
using System.Collections.Generic;
using FluentAssertions;
using nRpc.Serializers;
using nRpc.Tests.Data;
using Xunit;

namespace nRpc.Tests.Serializer
{
    public class SerializeShould
    {
        [Theory]
        [MemberData(nameof(Data))]
        public void ThrowArgumentNullException_WhenObjectIsNull(nRpc.Serializer serializer)
        {
            Action action = () => serializer.Serialize(null);
            action.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void ReturnBytes(nRpc.Serializer serializer)
        {
            var result = serializer.Serialize(new AnyObject());
            result.Should().NotBeNull();
            result.Length.Should().NotBe(0);
        }

        public static IEnumerable<object[]> Data => new List<object[]>
                                                            {
                                                                new object[] { new BinarySerializer() },
                                                                new object[] { new nRpc.Serializers.MessagePackSerializer() }
                                                            };
    }     
}