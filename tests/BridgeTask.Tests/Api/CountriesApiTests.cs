using System.Net;
using System.Net.Http.Json;
using BridgeTask.Application.Common;
using BridgeTask.Application.Countries;
using BridgeTask.Tests.Infrastructure;

namespace BridgeTask.Tests.Api;

public class CountriesApiTests : ApiTestBase
{
    public CountriesApiTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_ValidCountry_Returns201WithLocationAndNormalizedValues()
    {
        var response = await Client.PostAsJsonAsync("/api/countries", new { name = "  Egypt ", code = "eg" });

        var country = await ReadAsync<CountryDto>(response, HttpStatusCode.Created);
        Assert.Equal("Egypt", country.Name);
        Assert.Equal("EG", country.Code);
        Assert.Equal($"/api/countries/{country.Id}", response.Headers.Location?.AbsolutePath);
    }

    [Theory]
    [InlineData(null, "EG", "Name")]
    [InlineData("   ", "EG", "Name")]
    [InlineData("Egypt", null, "Code")]
    [InlineData("Egypt", "E", "Code")]
    [InlineData("Egypt", "EGYP", "Code")]
    [InlineData("Egypt", "E1", "Code")]
    public async Task Create_InvalidPayload_Returns400WithFieldError(string? name, string? code, string invalidField)
    {
        var response = await Client.PostAsJsonAsync("/api/countries", new { name, code });

        var problem = await ReadValidationProblemAsync(response);
        Assert.Contains(invalidField, problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_DuplicateCodeIgnoringCase_Returns409()
    {
        await CreateCountryAsync("Egypt", "EG");

        var response = await Client.PostAsJsonAsync("/api/countries", new { name = "Another", code = "eg" });

        var problem = await ReadProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("A country with code 'EG' already exists.", problem.Detail);
    }

    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        await CreateCountryAsync("Egypt", "EG");

        var response = await Client.PostAsJsonAsync("/api/countries", new { name = "EGYPT", code = "EGY" });

        await ReadProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_SameCodeConcurrently_OnlyOneSucceedsAndOthersConflict()
    {
        var requests = Enumerable.Range(1, 8)
            .Select(i => Client.PostAsJsonAsync("/api/countries", new { name = $"Country {i}", code = "XX" }));

        var responses = await Task.WhenAll(requests);

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [Fact]
    public async Task GetById_ExistingCountry_Returns200()
    {
        var created = await CreateCountryAsync("Egypt", "EG");

        var response = await Client.GetAsync($"/api/countries/{created.Id}");

        var country = await ReadAsync<CountryDto>(response, HttpStatusCode.OK);
        Assert.Equal(created.Id, country.Id);
        Assert.Equal("Egypt", country.Name);
        Assert.Equal("EG", country.Code);
    }

    [Fact]
    public async Task GetById_MissingCountry_Returns404ProblemDetails()
    {
        var response = await Client.GetAsync("/api/countries/999999");

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Equal("Country with id 999999 was not found.", problem.Detail);
        Assert.Equal("/api/countries/999999", problem.Instance);
    }

    [Fact]
    public async Task GetPaged_NoFilters_ReturnsAllOrderedByName()
    {
        await CreateCountryAsync("France", "FR");
        await CreateCountryAsync("Egypt", "EG");
        await CreateCountryAsync("Germany", "DE");

        var response = await Client.GetAsync("/api/countries");

        var page = await ReadAsync<PagedResult<CountryDto>>(response, HttpStatusCode.OK);
        Assert.Equal(new[] { "Egypt", "France", "Germany" }, page.Items.Select(c => c.Name));
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(10, page.PageSize);
    }

    [Fact]
    public async Task GetPaged_SecondPage_ReturnsRemainingItemsAndMetadata()
    {
        await CreateCountryAsync("Egypt", "EG");
        await CreateCountryAsync("France", "FR");
        await CreateCountryAsync("Germany", "DE");

        var response = await Client.GetAsync("/api/countries?pageNumber=2&pageSize=2");

        var page = await ReadAsync<PagedResult<CountryDto>>(response, HttpStatusCode.OK);
        Assert.Equal("Germany", Assert.Single(page.Items).Name);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.True(page.HasPreviousPage);
        Assert.False(page.HasNextPage);
    }

    [Theory]
    [InlineData("egy", "Egypt")]
    [InlineData("de", "Germany")]
    public async Task GetPaged_Search_MatchesPartOfNameOrCode(string search, string expected)
    {
        await CreateCountryAsync("Egypt", "EG");
        await CreateCountryAsync("France", "FR");
        await CreateCountryAsync("Germany", "DE");

        var response = await Client.GetAsync($"/api/countries?search={search}");

        var page = await ReadAsync<PagedResult<CountryDto>>(response, HttpStatusCode.OK);
        Assert.Equal(expected, Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task GetPaged_NameFilter_IsExactMatchIgnoringCase()
    {
        await CreateCountryAsync("Egypt", "EG");
        await CreateCountryAsync("France", "FR");

        var exact = await ReadAsync<PagedResult<CountryDto>>(
            await Client.GetAsync("/api/countries?name=egypt"), HttpStatusCode.OK);
        var partial = await ReadAsync<PagedResult<CountryDto>>(
            await Client.GetAsync("/api/countries?name=egy"), HttpStatusCode.OK);

        Assert.Equal("Egypt", Assert.Single(exact.Items).Name);
        Assert.Empty(partial.Items);
    }

    [Fact]
    public async Task GetPaged_CodeFilter_ReturnsMatchingCountry()
    {
        await CreateCountryAsync("Egypt", "EG");
        await CreateCountryAsync("France", "FR");

        var response = await Client.GetAsync("/api/countries?code=fr");

        var page = await ReadAsync<PagedResult<CountryDto>>(response, HttpStatusCode.OK);
        Assert.Equal("France", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task GetPaged_CombinedFilters_AllMustMatch()
    {
        await CreateCountryAsync("Egypt", "EG");
        await CreateCountryAsync("France", "FR");

        var match = await ReadAsync<PagedResult<CountryDto>>(
            await Client.GetAsync("/api/countries?name=Egypt&code=EG&pageNumber=1&pageSize=10"), HttpStatusCode.OK);
        var mismatch = await ReadAsync<PagedResult<CountryDto>>(
            await Client.GetAsync("/api/countries?name=Egypt&code=FR"), HttpStatusCode.OK);

        Assert.Equal("Egypt", Assert.Single(match.Items).Name);
        Assert.Empty(mismatch.Items);
        Assert.Equal(0, mismatch.TotalCount);
    }

    [Theory]
    [InlineData("pageNumber=0", "PageNumber")]
    [InlineData("pageSize=0", "PageSize")]
    [InlineData("pageSize=101", "PageSize")]
    public async Task GetPaged_InvalidPaging_Returns400(string query, string invalidField)
    {
        var response = await Client.GetAsync($"/api/countries?{query}");

        var problem = await ReadValidationProblemAsync(response);
        Assert.Contains(invalidField, problem.Errors.Keys);
    }

    [Fact]
    public async Task Update_ValidChange_Returns200WithUpdatedCountry()
    {
        var created = await CreateCountryAsync("Egypt", "EG");

        var response = await Client.PutAsJsonAsync($"/api/countries/{created.Id}", new { name = "Arab Republic of Egypt", code = "egy" });

        var country = await ReadAsync<CountryDto>(response, HttpStatusCode.OK);
        Assert.Equal("Arab Republic of Egypt", country.Name);
        Assert.Equal("EGY", country.Code);
    }

    [Fact]
    public async Task Update_KeepingOwnCode_IsNotTreatedAsDuplicate()
    {
        var created = await CreateCountryAsync("Egypt", "EG");

        var response = await Client.PutAsJsonAsync($"/api/countries/{created.Id}", new { name = "Egypt", code = "EG" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_MissingCountry_Returns404()
    {
        var response = await Client.PutAsJsonAsync("/api/countries/999999", new { name = "Nowhere", code = "NW" });

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ToAnotherCountrysCode_Returns409()
    {
        await CreateCountryAsync("Egypt", "EG");
        var france = await CreateCountryAsync("France", "FR");

        var response = await Client.PutAsJsonAsync($"/api/countries/{france.Id}", new { name = "France", code = "EG" });

        var problem = await ReadProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("A country with code 'EG' already exists.", problem.Detail);
    }

    [Fact]
    public async Task Delete_ExistingCountry_Returns204AndRemovesIt()
    {
        var created = await CreateCountryAsync("Egypt", "EG");

        var response = await Client.DeleteAsync($"/api/countries/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/countries/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_MissingCountry_Returns404()
    {
        var response = await Client.DeleteAsync("/api/countries/999999");

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_CountryWithCities_Returns409AndKeepsData()
    {
        var egypt = await CreateCountryAsync("Egypt", "EG");
        var cairo = await CreateCityAsync("Cairo", egypt.Id);

        var response = await Client.DeleteAsync($"/api/countries/{egypt.Id}");

        var problem = await ReadProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("Country cannot be deleted because it contains cities.", problem.Detail);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync($"/api/countries/{egypt.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync($"/api/cities/{cairo.Id}")).StatusCode);
    }
}
