using BridgeTask.Application.Cities;
using BridgeTask.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace BridgeTask.Api.Controllers;

[ApiController]
[Route("api/cities")]
[Produces("application/json")]
public class CitiesController : ControllerBase
{
    private readonly ICityService _cityService;

    public CitiesController(ICityService cityService)
    {
        _cityService = cityService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CityDto>>> GetCities(
        [FromQuery] CityFilterDto filter,
        CancellationToken cancellationToken)
    {
        return Ok(await _cityService.GetPagedAsync(filter, cancellationToken));
    }

    [HttpGet("/api/countries/{countryId:int}/cities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<CityDto>>> GetCitiesByCountry(
        int countryId,
        [FromQuery] PagedRequest paging,
        CancellationToken cancellationToken)
    {
        return Ok(await _cityService.GetByCountryIdAsync(countryId, paging, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CityDto>> GetCity(int id, CancellationToken cancellationToken)
    {
        return Ok(await _cityService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CityDto>> CreateCity(CityCreateDto dto, CancellationToken cancellationToken)
    {
        var city = await _cityService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetCity), new { id = city.Id }, city);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CityDto>> UpdateCity(int id, CityUpdateDto dto, CancellationToken cancellationToken)
    {
        return Ok(await _cityService.UpdateAsync(id, dto, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCity(int id, CancellationToken cancellationToken)
    {
        await _cityService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
