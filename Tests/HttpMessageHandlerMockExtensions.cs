using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

namespace nRpc.Tests
{
    public static class HttpMessageHandlerMockExtensions
    {
        public static void ReturnsOkReponse(this Mock<HttpMessageHandler> mock)
        {
            mock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync"
                                            , ItExpr.IsAny<HttpRequestMessage>()
                                            , ItExpr.IsAny<CancellationToken>())
                            .Returns(Task.FromResult(
                                new HttpResponseMessage(HttpStatusCode.OK)));
        }
        
        public static void ReturnsOkReponse(this Mock<HttpMessageHandler> mock, HttpContent content)
        {
               mock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync"
                                        , ItExpr.IsAny<HttpRequestMessage>()
                                        , ItExpr.IsAny<CancellationToken>())
                                    .Returns(Task.FromResult(
                                        new HttpResponseMessage(HttpStatusCode.OK)
                                        {
                                            Content = content
                                        }));
        }

        public static void VerifyRequest(this Mock<HttpMessageHandler> mock, Func<HttpRequestMessage, bool> predicate)
        {
            mock.Protected().Verify("SendAsync",
                                    Times.Once(),
                                    ItExpr.Is<HttpRequestMessage>(r => predicate(r)),
                                    ItExpr.IsAny<CancellationToken>());
        }
    }
}