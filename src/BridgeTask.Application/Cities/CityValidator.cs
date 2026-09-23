using BridgeTask.Application.Common;
using BridgeTask.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BridgeTask.Application.Cities;

public class CityValidator : ICityValidator
{
    private readonly IAppDbContext _context;

    public CityValidator(IAppDbContext context)
    {
        _context = context;
    }

    public async Task ValidateAsync(string name, int countryId, int? existingCityId, CancellationToken cancellationToken)
    {
        // The country is referenced from the request body, not the URL, so a missing one is a bad request rather than a 404.
        var countryExists = await _context.Countries.AnyAsync(c => c.Id == countryId, cancellationToken);

        if (!countryExists)
        {
            throw new BusinessValidationException($"Country with id {countryId} does not exist.");
        }

        var duplicate = await _context.Cities
            .AnyAsync(c => c.CountryId == countryId && c.Name == name && c.Id != existingCityId, cancellationToken);

        if (duplicate)
        {
            throw new ConflictException($"A city named '{name}' already exists in this country.");
        }
    }
}
