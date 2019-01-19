#pragma warning disable xUnit1026
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using nRpc.Protocol;
using nRpc.Tests.Data;
using nRpc.Tests.TransferProtocol.Extensions;
using Xunit;

namespace nRpc.Tests.TransferProtocol
{
    public class ReceiveShould
    {
        private static readonly Uri _uri = new Uri("http://anyhost.com/AnyFunction");
        private static nRpc.Serializer TestSerializer = new TestSerializer();
        private static AnyObject _anyObject = new AnyObject() 
                                                    {
                                                        anyString = "anyString Value",
                                                        anyInt = int.MaxValue
                                                    };
        private static byte[] _anyByte = new byte[1];
        private static readonly Dictionary<string, string> DefaultStatusHeadersKeyValues = new Dictionary<string, string>
        {
           { ":status", "200" },
           { "xyz", "ghi" }
        };

        [Theory]
        [MemberData(nameof(Protocols))]
        public async Task ReturnTheRequest(TransferProtocolReceiver protocol, TransferProtocolReceiverVefify protocolVerify)
        {
            protocolVerify.SetupRequest(_anyObject);
            var requestBytes = await protocol.ReadRequestAsync();
            var request = TestSerializer.Deserialize<AnyObject>(requestBytes);

            request.Should().BeOfType<AnyObject>();
            request.anyInt.Should().Be(_anyObject.anyInt);
            request.anyString.Should().Be(_anyObject.anyString);
        }

        [Theory]
        [MemberData(nameof(Protocols))]
        public void ThrowTimeoutException_WhenNoRequestData(TransferProtocolReceiver protocol, TransferProtocolReceiverVefify protocolVerify)
        {            
            Func<Task> action = async () => await protocol.ReadRequestAsync();
            action.Should().Throw<TimeoutException>();
        }

        [Theory]
        [MemberData(nameof(Protocols))]
        public async Task ReturnTheFunction(TransferProtocolReceiver protocol, TransferProtocolReceiverVefify protocolVerify)
        {
            protocolVerify.SetupRequest(_anyObject);
            var functionName = await protocol.GetRequestFunctionAsync();
            var expectedFunctionName = _uri.PathAndQuery.Replace("/", string.Empty);
            functionName.Should().Be(expectedFunctionName);
        }

        [Theory]
        [MemberData(nameof(Protocols))]
        public async Task SendResponse(TransferProtocolReceiver protocol, TransferProtocolReceiverVefify protocolVerify)
        {
            await protocol.SendResponseAsync(DefaultStatusHeadersKeyValues, _anyByte);
            protocolVerify.DrainResponse();
        }

        [Theory]
        [MemberData(nameof(Protocols))]
        public void ThrowArgumentNullException_WhenContentIsEmpty(TransferProtocolReceiver protocol, TransferProtocolReceiverVefify protocolVerify)
        {
            Func<Task> action = async () => await protocol.SendResponseAsync(DefaultStatusHeadersKeyValues, new byte[0]);
            action.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [MemberData(nameof(Protocols))]
        public void ThrowArgumentNullException_WhenHeaderIsNull(TransferProtocolReceiver protocol, TransferProtocolReceiverVefify protocolVerify)
        {
            Func<Task> action = async () => await protocol.SendResponseAsync(null, _anyByte);
            action.Should().Throw<ArgumentNullException>();
        }

        public static IEnumerable<object[]> Protocols()
        {
            var getHeaderValues = new Dictionary<string, string>
                {
                   { ":method", "GET" },
                   { ":scheme", _uri.Scheme.ToLowerInvariant() },
                   { ":path", _uri.PathAndQuery }
                };

            var inPipe = new Http2BufferedPipe(1024);
            var outPipe = new Http2BufferedPipe(1024);
            var stream = Http2StreamCreator.GetServerStream(getHeaderValues, inPipe, outPipe).GetAwaiter().GetResult();
            var http2ProtocolReceiver = new Http2ProtocolReceiver();
            http2ProtocolReceiver.Stream = stream;
            var http2ProtocolVerify = new Http2ProtocolVerify(inPipe, outPipe);

            return new List<object[]> 
                    {
                        new object[] { http2ProtocolReceiver, http2ProtocolVerify }
                    };            
        }

        public class Http2ProtocolVerify : TransferProtocolReceiverVefify
        {
            Http2BufferedPipe _inPipe;
            Http2BufferedPipe _outPipe;

            public Http2ProtocolVerify(Http2BufferedPipe inPipe, Http2BufferedPipe outPipe)
            {
                _inPipe = inPipe;
                _outPipe = outPipe;
            }

            public void DrainResponse()
            {
                _outPipe.ReadAndDiscardHeaders(1, false).GetAwaiter().GetResult();
                _outPipe.ReadAllWithTimeout(new ArraySegment<byte>(new byte[_anyByte.Length])).GetAwaiter().GetResult();                
            }

            public void SetupRequest(object content)
            {
                var requestMessage = TestSerializer.Serialize(content);
                Http2BufferedPipe.WriteData(requestMessage, _inPipe).GetAwaiter().GetResult();
            }
        }        

        public interface TransferProtocolReceiverVefify
        {
            void DrainResponse();
            void SetupRequest(object content);
        } 
    }
}
#pragma warning restore xUnit1026