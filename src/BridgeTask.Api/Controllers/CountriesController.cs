using BridgeTask.Application.Common;
using BridgeTask.Application.Countries;
using Microsoft.AspNetCore.Mvc;

namespace BridgeTask.Api.Controllers;

[ApiController]
[Route("api/countries")]
public class CountriesController : ControllerBase
{
    private readonly ICountryService _countryService;

    public CountriesController(ICountryService countryService)
    {
        _countryService = countryService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CountryDto>>> GetCountries(
        [FromQuery] CountryFilterDto filter,
        CancellationToken cancellationToken)
    {
        return Ok(await _countryService.GetPagedAsync(filter, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CountryDto>> GetCountry(int id, CancellationToken cancellationToken)
    {
        return Ok(await _countryService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CountryDto>> CreateCountry(CountryCreateDto dto, CancellationToken cancellationToken)
    {
        var country = await _countryService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetCountry), new { id = country.Id }, country);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CountryDto>> UpdateCountry(int id, CountryUpdateDto dto, CancellationToken cancellationToken)
    {
        return Ok(await _countryService.UpdateAsync(id, dto, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCountry(int id, CancellationToken cancellationToken)
    {
        await _countryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
