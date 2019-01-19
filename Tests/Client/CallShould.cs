using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using nRpc.Tests.Data;
using Xunit;

namespace nRpc.Tests.Client
{
    public class CallShould
    {
        private const string _host = "anyhost.com";
        private const string _hostUri = "http://" + _host;
        private nRpc.Serializer _anySerializer = new Mock<nRpc.Serializer>().Object;
        private Protocol.TransferProtocolSender _anyProtocol = new Mock<Protocol.TransferProtocolSender>().Object;
        private nRpc.Serializer TestSerializer = new TestSerializer();
        
        private AnyObject _anyObject = new AnyObject() 
                                        {
                                            anyString = "anyString Value",
                                            anyInt = int.MaxValue
                                        };

        [Fact]
        public async Task BeSentToTheHost()
        {
            var protocolMock = new Mock<Protocol.TransferProtocolSender>();
            
            var client = new nRpc.Client(_hostUri, protocolMock.Object, _anySerializer);
            await client.Call(new Procedure<object, object>("AnyName"), new object());
            
            protocolMock.Verify(p => p.Send(It.Is<string>(u => (new Uri(u)).Host == _host), It.IsAny<byte[]>()));
        }

        [Fact]
        public async Task BeSentToTheFunction()
        {
            const string functionName = "AnyFunction";
            var protocolMock = new Mock<Protocol.TransferProtocolSender>();                            
            
            var client = new nRpc.Client(_hostUri, protocolMock.Object, _anySerializer);
            await client.Call(new Procedure<object, object>(functionName), new object());
            
             protocolMock.Verify(p => p.Send(It.Is<string>(u => u == $"{_hostUri}/{functionName}"), It.IsAny<byte[]>()));
        }

        [Fact]
        public async Task ReturnTheResponseMessage()
        {
            var protocolMock = new Mock<Protocol.TransferProtocolSender>();         
            protocolMock.Setup(p => p.Send(It.IsAny<string>(), It.IsAny<byte[]>())).ReturnsAsync(TestSerializer.Serialize(_anyObject));
            
            var client = new nRpc.Client(_hostUri, protocolMock.Object, TestSerializer);
            var response = await client.Call(new Procedure<object, AnyObject>("AnyName")                                                    
                                                , new object());

            response.Should().BeOfType<AnyObject>();
            response.anyInt.Should().Be(_anyObject.anyInt);
            response.anyString.Should().Be(_anyObject.anyString);
        }

        [Fact]
        public async Task SendTheRequestMessage()
        {
            var protocolMock = new Mock<Protocol.TransferProtocolSender>();
            var serializerMock = new Mock<nRpc.Serializer>();
            serializerMock.Setup(s => s.Serialize(It.IsAny<object>()))
                            .Returns(TestSerializer.Serialize(_anyObject));

            var client = new nRpc.Client(_hostUri, protocolMock.Object, serializerMock.Object);
                       
            await client.Call(new Procedure<AnyObject, object>("AnyName")
                                                    , _anyObject);

            protocolMock.Verify(p => p.Send(It.IsAny<string>(), It.Is<byte[]>(b => TestSerializer.Deserialize<AnyObject>(b) == _anyObject)));
        }

        [Fact]
        public void ThrowArgumentNullException_WhenFunctionIsNull()
        {
            var client = new nRpc.Client(_hostUri, _anyProtocol, _anySerializer);
            Func<Task> action = async () => await client.Call<object, object>((Procedure<object, object>)null, new AnyObject());
            action.Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void ThrowArgumentNullException_WhenRequestMessageIsNull()
        {
            var client = new nRpc.Client(_hostUri, _anyProtocol, _anySerializer);
            Func<Task> action = async () => await client.Call(new Procedure<object, object>("AnyName"), null);
            action.Should().Throw<ArgumentNullException>();
        }
    }
}