using System.Net;
using System.Net.Http.Json;
using BridgeTask.Application.Cities;
using BridgeTask.Application.Common;
using BridgeTask.Application.Countries;
using BridgeTask.Tests.Infrastructure;

namespace BridgeTask.Tests.Api;

public class CitiesApiTests : ApiTestBase
{
    public CitiesApiTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_ValidCity_Returns201WithCountryDetails()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");

        var response = await Client.PostAsJsonAsync("/api/cities", new { name = " Cairo ", countryId = egypt.Id });

        var city = await ReadAsync<CityDto>(response, HttpStatusCode.Created);
        Assert.Equal("Cairo", city.Name);
        Assert.Equal(egypt.Id, city.CountryId);
        Assert.Equal("Egypt", city.CountryName);
        Assert.Equal("EG", city.CountryCode);
        Assert.Equal($"/api/cities/{city.Id}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Create_NonexistentCountryId_Returns400BusinessRuleViolation()
    {
        var response = await Client.PostAsJsonAsync("/api/cities", new { name = "Nowhere", countryId = 999999 });

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Country with id 999999 does not exist.", problem.Detail);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_NonPositiveCountryId_Returns400ValidationError(int countryId)
    {
        var response = await Client.PostAsJsonAsync("/api/cities", new { name = "Cairo", countryId });

        var problem = await ReadValidationProblemAsync(response);
        Assert.Contains("CountryId", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_DuplicateNameInSameCountry_Returns409()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");
        await CreateCityAsync("Cairo", egypt.Id);

        var response = await Client.PostAsJsonAsync("/api/cities", new { name = "cairo", countryId = egypt.Id });

        var problem = await ReadProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("A city named 'cairo' already exists in this country.", problem.Detail);
    }

    [Fact]
    public async Task Create_SameNameInDifferentCountry_IsAllowed()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");
        var usa = await CreateCountryAsync("United States", "US");
        await CreateCityAsync("Alexandria", egypt.Id);

        var response = await Client.PostAsJsonAsync("/api/cities", new { name = "Alexandria", countryId = usa.Id });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingCity_Returns200()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");
        var created = await CreateCityAsync("Cairo", egypt.Id);

        var response = await Client.GetAsync($"/api/cities/{created.Id}");

        var city = await ReadAsync<CityDto>(response, HttpStatusCode.OK);
        Assert.Equal("Cairo", city.Name);
        Assert.Equal("Egypt", city.CountryName);
    }

    [Fact]
    public async Task GetById_MissingCity_Returns404()
    {
        var response = await Client.GetAsync("/api/cities/999999");

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Equal("City with id 999999 was not found.", problem.Detail);
    }

    [Fact]
    public async Task GetPaged_NoFilters_ReturnsAllCitiesOrderedByName()
    {
        await SeedAsync();

        var response = await Client.GetAsync("/api/cities");

        var page = await ReadAsync<PagedResult<CityDto>>(response, HttpStatusCode.OK);
        Assert.Equal(new[] { "Alexandria", "Cairo", "Giza", "Lyon", "Paris" }, page.Items.Select(c => c.Name));
        Assert.Equal(5, page.TotalCount);
    }

    [Fact]
    public async Task GetPaged_Pagination_ReturnsRequestedPage()
    {
        await SeedAsync();

        var response = await Client.GetAsync("/api/cities?pageNumber=2&pageSize=2");

        var page = await ReadAsync<PagedResult<CityDto>>(response, HttpStatusCode.OK);
        Assert.Equal(new[] { "Giza", "Lyon" }, page.Items.Select(c => c.Name));
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public async Task GetPaged_Search_MatchesPartOfCityName()
    {
        await SeedAsync();

        var response = await Client.GetAsync("/api/cities?search=cai");

        var page = await ReadAsync<PagedResult<CityDto>>(response, HttpStatusCode.OK);
        Assert.Equal("Cairo", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task GetPaged_NameFilter_IsExactMatch()
    {
        await SeedAsync();

        var exact = await ReadAsync<PagedResult<CityDto>>(await Client.GetAsync("/api/cities?name=cairo"), HttpStatusCode.OK);
        var partial = await ReadAsync<PagedResult<CityDto>>(await Client.GetAsync("/api/cities?name=cai"), HttpStatusCode.OK);

        Assert.Equal("Cairo", Assert.Single(exact.Items).Name);
        Assert.Empty(partial.Items);
    }

    [Fact]
    public async Task GetPaged_CountryIdFilter_ReturnsOnlyThatCountrysCities()
    {
        var (_, france) = await SeedAsync();

        var response = await Client.GetAsync($"/api/cities?countryId={france.Id}");

        var page = await ReadAsync<PagedResult<CityDto>>(response, HttpStatusCode.OK);
        Assert.Equal(new[] { "Lyon", "Paris" }, page.Items.Select(c => c.Name));
        Assert.All(page.Items, c => Assert.Equal(france.Id, c.CountryId));
    }

    [Fact]
    public async Task GetPaged_CountryIdAndName_AllMustMatch()
    {
        var (egypt, france) = await SeedAsync();

        var match = await ReadAsync<PagedResult<CityDto>>(
            await Client.GetAsync($"/api/cities?countryId={egypt.Id}&name=Cairo"), HttpStatusCode.OK);
        var mismatch = await ReadAsync<PagedResult<CityDto>>(
            await Client.GetAsync($"/api/cities?countryId={france.Id}&name=Cairo"), HttpStatusCode.OK);

        Assert.Equal("Cairo", Assert.Single(match.Items).Name);
        Assert.Empty(mismatch.Items);
    }

    [Fact]
    public async Task Update_ValidChange_Returns200()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");
        var city = await CreateCityAsync("Giza", egypt.Id);

        var response = await Client.PutAsJsonAsync($"/api/cities/{city.Id}", new { name = "Luxor", countryId = egypt.Id });

        var updated = await ReadAsync<CityDto>(response, HttpStatusCode.OK);
        Assert.Equal("Luxor", updated.Name);
    }

    [Fact]
    public async Task Update_MissingCity_Returns404()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");

        var response = await Client.PutAsJsonAsync("/api/cities/999999", new { name = "Luxor", countryId = egypt.Id });

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ToExistingNameInSameCountry_Returns409()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");
        await CreateCityAsync("Cairo", egypt.Id);
        var giza = await CreateCityAsync("Giza", egypt.Id);

        var response = await Client.PutAsJsonAsync($"/api/cities/{giza.Id}", new { name = "Cairo", countryId = egypt.Id });

        await ReadProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_ChangeCountry_MovesCityAndReturnsNewCountryDetails()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");
        var france = await CreateCountryAsync("France", "FR");
        var city = await CreateCityAsync("Nice", egypt.Id);

        var response = await Client.PutAsJsonAsync($"/api/cities/{city.Id}", new { name = "Nice", countryId = france.Id });

        var updated = await ReadAsync<CityDto>(response, HttpStatusCode.OK);
        Assert.Equal(france.Id, updated.CountryId);
        Assert.Equal("France", updated.CountryName);
        Assert.Equal("FR", updated.CountryCode);
    }

    [Fact]
    public async Task Update_ChangeToNonexistentCountry_Returns400()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");
        var city = await CreateCityAsync("Cairo", egypt.Id);

        var response = await Client.PutAsJsonAsync($"/api/cities/{city.Id}", new { name = "Cairo", countryId = 999999 });

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_ExistingCity_Returns204AndRemovesIt()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");
        var city = await CreateCityAsync("Cairo", egypt.Id);

        var response = await Client.DeleteAsync($"/api/cities/{city.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/cities/{city.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_MissingCity_Returns404()
    {
        var response = await Client.DeleteAsync("/api/cities/999999");

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByCountry_ExistingCountry_ReturnsOnlyItsCities()
    {
        var (egypt, _) = await SeedAsync();

        var response = await Client.GetAsync($"/api/countries/{egypt.Id}/cities");

        var page = await ReadAsync<PagedResult<CityDto>>(response, HttpStatusCode.OK);
        Assert.Equal(new[] { "Alexandria", "Cairo", "Giza" }, page.Items.Select(c => c.Name));
    }

    [Fact]
    public async Task GetByCountry_Pagination_ReturnsRequestedPage()
    {
        var (egypt, _) = await SeedAsync();

        var response = await Client.GetAsync($"/api/countries/{egypt.Id}/cities?pageNumber=2&pageSize=2");

        var page = await ReadAsync<PagedResult<CityDto>>(response, HttpStatusCode.OK);
        Assert.Equal("Giza", Assert.Single(page.Items).Name);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.False(page.HasNextPage);
    }

    [Fact]
    public async Task GetByCountry_MissingCountry_Returns404()
    {
        var response = await Client.GetAsync("/api/countries/999999/cities");

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Equal("Country with id 999999 was not found.", problem.Detail);
    }

    private async Task<(CountryDto Egypt, CountryDto France)> SeedAsync()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");
        var france = await CreateCountryAsync("France", "FR");

        await CreateCityAsync("Cairo", egypt.Id);
        await CreateCityAsync("Alexandria", egypt.Id);
        await CreateCityAsync("Giza", egypt.Id);
        await CreateCityAsync("Paris", france.Id);
        await CreateCityAsync("Lyon", france.Id);

        return (egypt, france);
    }
}
