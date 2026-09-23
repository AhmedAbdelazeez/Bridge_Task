using System.Net;
using System.Net.Http.Json;
using BridgeTask.Application.Cities;
using BridgeTask.Application.Countries;
using BridgeTask.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BridgeTask.Tests.Infrastructure;

[Collection(ApiCollection.Name)]
public abstract class ApiTestBase : IAsyncLifetime
{
    protected ApiTestBase(ApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected ApiFactory Factory { get; }
    protected HttpClient Client { get; }

    public async Task InitializeAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Cities.ExecuteDeleteAsync();
        await context.Countries.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task<CountryDto> CreateCountryAsync(string name, string code)
    {
        var response = await Client.PostAsJsonAsync("/api/countries", new CountryCreateDto { Name = name, Code = code });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CountryDto>())!;
    }

    protected async Task<CityDto> CreateCityAsync(string name, int countryId)
    {
        var response = await Client.PostAsJsonAsync("/api/cities", new CityCreateDto { Name = name, CountryId = countryId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CityDto>())!;
    }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    protected static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.Equal((int)expectedStatus, problem.Status);
        Assert.False(string.IsNullOrEmpty(problem.Title));
        Assert.True(problem.Extensions.ContainsKey("traceId"));
        return problem;
    }

    protected static async Task<ValidationProblemDetails> ReadValidationProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        Assert.Equal(400, problem.Status);
        Assert.NotEmpty(problem.Errors);
        return problem;
    }
}
