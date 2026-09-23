using System.Net;
using BridgeTask.Tests.Infrastructure;
using Microsoft.AspNetCore.TestHost;

namespace BridgeTask.Tests.Api;

public class UnexpectedErrorTests : ApiTestBase
{
    private const string UnreachableDatabase =
        "Server=tcp:127.0.0.1,1;Database=SecretDb;User Id=secret_user;Password=secret_password;Connect Timeout=2;ConnectRetryCount=0";

    public UnexpectedErrorTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task DatabaseFailure_Returns500ProblemDetailsWithoutInternalDetails()
    {
        using var brokenApi = Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:DefaultConnection", UnreachableDatabase));
        using var client = brokenApi.CreateClient();

        var response = await client.GetAsync("/api/countries");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("An unexpected error occurred.", body);
        Assert.Contains("traceId", body);

        foreach (var secret in new[] { "secret_password", "secret_user", "SecretDb", "127.0.0.1", "SqlException", "Microsoft.Data", "   at " })
        {
            Assert.DoesNotContain(secret, body);
        }
    }
}
