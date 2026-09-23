using BridgeTask.Application.Common.Exceptions;
using BridgeTask.Domain.Entities;
using BridgeTask.Infrastructure.Persistence;
using BridgeTask.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BridgeTask.Tests.Api;

// These go straight to the DbContext, skipping the validators, to prove the database
// still protects integrity when a concurrent request slips past the application checks.
public class DatabaseConstraintTests : ApiTestBase
{
    public DatabaseConstraintTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task DuplicateCountryCode_IsRejectedByUniqueIndex()
    {
        await CreateCountryAsync("Egypt", "EG");

        var exception = await SaveAsync(context => context.Countries.Add(new Country { Name = "Other", Code = "EG" }));

        Assert.Equal("A country with this code already exists.", exception.Message);
    }

    [Fact]
    public async Task DuplicateCityInSameCountry_IsRejectedByCompositeUniqueIndex()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");
        await CreateCityAsync("Cairo", egypt.Id);

        var exception = await SaveAsync(context => context.Cities.Add(new City { Name = "Cairo", CountryId = egypt.Id }));

        Assert.Equal("A city with this name already exists in this country.", exception.Message);
    }

    [Fact]
    public async Task DeletingCountryWithCities_IsRejectedByRestrictForeignKey()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");
        await CreateCityAsync("Cairo", egypt.Id);

        var exception = await SaveAsync(context => context.Countries.Remove(context.Countries.Single(c => c.Id == egypt.Id)));

        Assert.Equal("The record cannot be deleted because other records depend on it.", exception.Message);
    }

    private async Task<ConflictException> SaveAsync(Action<AppDbContext> change)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        change(context);

        return await Assert.ThrowsAsync<ConflictException>(() => context.SaveChangesAsync());
    }
}
