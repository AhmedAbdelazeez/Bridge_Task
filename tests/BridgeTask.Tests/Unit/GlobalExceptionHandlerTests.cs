using System.Text.Json;
using BridgeTask.Api.ExceptionHandling;
using BridgeTask.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BridgeTask.Tests.Unit;

public class GlobalExceptionHandlerTests
{
    public static TheoryData<Exception, int, string> KnownExceptions => new()
    {
        { new NotFoundException("Country", 7), StatusCodes.Status404NotFound, "Country with id 7 was not found." },
        { new ConflictException("Duplicate code."), StatusCodes.Status409Conflict, "Duplicate code." },
        { new BusinessValidationException("Invalid country."), StatusCodes.Status400BadRequest, "Invalid country." }
    };

    [Theory]
    [MemberData(nameof(KnownExceptions))]
    public async Task KnownException_IsMappedToItsStatusWithMessageAsDetail(Exception exception, int expectedStatus, string expectedDetail)
    {
        var (handled, context, body) = await HandleAsync(exception);

        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal(expectedStatus, body.GetProperty("status").GetInt32());
        Assert.Equal(expectedDetail, body.GetProperty("detail").GetString());
        Assert.Equal("/api/countries/7", body.GetProperty("instance").GetString());
        Assert.True(body.TryGetProperty("type", out _));
    }

    [Fact]
    public async Task UnexpectedException_Returns500WithoutLeakingItsMessage()
    {
        var exception = new InvalidOperationException("Connection string: Server=prod;Password=hunter2");

        var (handled, context, body) = await HandleAsync(exception);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("An unexpected error occurred.", body.GetProperty("detail").GetString());
        Assert.DoesNotContain("hunter2", body.GetRawText());
        Assert.DoesNotContain(nameof(InvalidOperationException), body.GetRawText());
    }

    private static async Task<(bool Handled, HttpContext Context, JsonElement Body)> HandleAsync(Exception exception)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Path = "/api/countries/7";
        context.Response.Body = new MemoryStream();

        var handler = new GlobalExceptionHandler(
            services.GetRequiredService<IProblemDetailsService>(),
            NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);

        return (handled, context, body);
    }
}
