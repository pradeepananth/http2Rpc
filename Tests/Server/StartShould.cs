using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using nRpc.Protocol;
using Xunit;

namespace nRpc.Tests.Server
{
    public class StartShould
    {
        [Fact]
        public async Task Receive()
        {
            var protocolMock = new Mock<TransferProtocolReceiver>();
            var server = new nRpc.Server.Server(protocolMock.Object);
            await server.Start(CancellationToken.None);
            protocolMock.Verify(p => p.Receive());
        }

        [Fact]
        public async Task HandleIncomingRequest()
        {
            var protocolMock = new Mock<TransferProtocolReceiver>();            
            var server = new nRpc.Server.Server(protocolMock.Object);
            await server.HandleIncomingStream();
            protocolMock.Verify(p => p.GetRequestFunctionAsync());
            protocolMock.Verify(p => p.ReadRequestAsync());
            protocolMock.Verify(p => p.SendResponseAsync(It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<byte[]>()));
        }
    }
}