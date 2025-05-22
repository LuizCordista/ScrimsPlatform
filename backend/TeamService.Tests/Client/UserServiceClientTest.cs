using System.Net;
using JetBrains.Annotations;
using TeamService.Client;
using Xunit;

namespace TeamService.Tests.Client;

[TestSubject(typeof(UserServiceClient))]
public class UserServiceClientTest
{
    private static HttpClient CreateMockHttpClient(HttpStatusCode statusCode)
    {
        var handler = new MockHttpMessageHandler(statusCode);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    [Fact]
    public async Task UserExistsAsync_Should_Return_True_When_User_Exists()
    {
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK);
        var client = new UserServiceClient(httpClient);

        var result = await client.UserExistsAsync(Guid.NewGuid());

        Assert.True(result);
    }

    [Fact]
    public async Task UserExistsAsync_Should_Return_False_When_User_Does_Not_Exist()
    {
        var httpClient = CreateMockHttpClient(HttpStatusCode.NotFound);
        var client = new UserServiceClient(httpClient);

        var result = await client.UserExistsAsync(Guid.NewGuid());

        Assert.False(result);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public MockHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);
            return Task.FromResult(response);
        }
    }
}
