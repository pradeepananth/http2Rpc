#pragma warning disable xUnit1026
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using nRpc.Protocol;
using nRpc.Tests.Data;
using Xunit;

namespace nRpc.Tests.TransferProtocol
{
    public class SendShould
    {
        private const string _uri = "http://anyhost.com/AnyFunction";
        private static nRpc.Serializer TestSerializer = new TestSerializer();
        private static AnyObject _anyObject = new AnyObject() 
                                                    {
                                                        anyString = "anyString Value",
                                                        anyInt = int.MaxValue
                                                    };
        private static byte[] _anyByte = new byte[1];

        [Theory]
        [MemberData(nameof(Protocols))]
        public async Task SendToTheUri(Protocol.TransferProtocolSender protocol, TransferProtocolVefify protocolVerify)
        {
            await protocol.Send(_uri, _anyByte);
            protocolVerify.Uri(_uri);
        }

        [Theory]
        [MemberData(nameof(Protocols))]
        public async Task ReturnTheResponse(Protocol.TransferProtocolSender protocol, TransferProtocolVefify protocolVerify)
        {
            protocolVerify.SetupResponse(_anyObject);
            var responseBytes = await protocol.Send(_uri, _anyByte);
            var response = TestSerializer.Deserialize<AnyObject>(responseBytes);

            response.Should().BeOfType<AnyObject>();
            response.anyInt.Should().Be(_anyObject.anyInt);
            response.anyString.Should().Be(_anyObject.anyString);
        }

        [Theory]
        [MemberData(nameof(Protocols))]
        public async Task SendTheContent(Protocol.TransferProtocolSender protocol, TransferProtocolVefify protocolVerify)
        {
            await protocol.Send(_uri, TestSerializer.Serialize(_anyObject));
            protocolVerify.Content<AnyObject>(_anyObject);
        }
        
        [Theory]
        [MemberData(nameof(Protocols))]
        public void ThrowArgumentNullException_WhenUriIsNull(Protocol.TransferProtocolSender protocol, TransferProtocolVefify protocolVerify)
        {
            Func<Task> action = () => protocol.Send(null, new byte[1]);
            action.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [MemberData(nameof(Protocols))]
        public void ThrowArgumentNullException_WhenUriIsEmpty(Protocol.TransferProtocolSender protocol, TransferProtocolVefify protocolVerify)
        {
            Func<Task> action = () => protocol.Send(string.Empty, new byte[1]);
            action.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [MemberData(nameof(Protocols))]
        public void ThrowArgumentNullException_WhenContentIsNull(Protocol.TransferProtocolSender protocol, TransferProtocolVefify protocolVerify)
        {
            Func<Task> action = () => protocol.Send(_uri, null);
            action.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [MemberData(nameof(Protocols))]
        public void ThrowArgumentNullException_WhenContentIsEmpty(Protocol.TransferProtocolSender protocol, TransferProtocolVefify protocolVerify)
        {
            Func<Task> action = () => protocol.Send(_uri, new byte[0]);
            action.Should().Throw<ArgumentNullException>();
        }

        public static IEnumerable<object[]> Protocols()
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.ReturnsOkReponse();
            var http2clientHandler = new Mock<Http2Client>(MockBehavior.Default, new object[] { _uri });            
            return new List<object[]> 
                    {
                        new object[] { new HttpClientProtocol(new HttpClient(mockHandler.Object)), new HttpProtocolVerify(mockHandler) },
                        new object[] { new Http2ProtocolSender(http2clientHandler.Object), new Http2ProtocolVerify(http2clientHandler) }
                    };            
        }
       
        public class HttpProtocolVerify : TransferProtocolVefify
        {
            Mock<HttpMessageHandler> _mockHandler;

            public HttpProtocolVerify(Mock<HttpMessageHandler> handler)
            {
                _mockHandler = handler;
            }

            public void Content<T>(T content)
                where T : class, new()
            {
                _mockHandler.VerifyRequest(r => TestSerializer.Deserialize<T>(
                                                r.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()).Equals((T)content));
            }

            public void SetupResponse(object content)
            {
                _mockHandler.ReturnsOkReponse(new ByteArrayContent(TestSerializer.Serialize(content)));                
            }

            public void Uri(string uri)
            {
                _mockHandler.VerifyRequest(r => r.RequestUri == new Uri(uri));
            }
        }

        public class Http2ProtocolVerify : TransferProtocolVefify
        {
            Mock<Http2Client> _mockHandler;

            public Http2ProtocolVerify(Mock<Http2Client> handler)
            {
                _mockHandler = handler;
            }

            public void Content<T>(T content) where T : class, new()
            {
                //TODO: Set up content verification
            }

            public void SetupResponse(object content)
            {
                _mockHandler.Setup(c => c.SendAsync(It.IsAny<Uri>(), It.IsAny<byte[]>())).ReturnsAsync(TestSerializer.Serialize(content));
            }

            public void Uri(string uri)
            {
                //TODO: Set up Uri verification
            }
        }

        public interface TransferProtocolVefify
        {
            void Content<T> (T content) where T : class, new();
            void Uri (string uri);
            void SetupResponse(object content);
        } 
    }
}
#pragma warning restore xUnit1026