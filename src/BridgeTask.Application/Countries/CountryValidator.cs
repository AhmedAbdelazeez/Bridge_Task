using BridgeTask.Application.Common;
using BridgeTask.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BridgeTask.Application.Countries;

public class CountryValidator : ICountryValidator
{
    private readonly IAppDbContext _context;

    public CountryValidator(IAppDbContext context)
    {
        _context = context;
    }

    public async Task ValidateAsync(string name, string code, int? existingCountryId, CancellationToken cancellationToken)
    {
        var codeTaken = await _context.Countries
            .AnyAsync(c => c.Code == code && c.Id != existingCountryId, cancellationToken);

        if (codeTaken)
        {
            throw new ConflictException($"A country with code '{code}' already exists.");
        }

        var nameTaken = await _context.Countries
            .AnyAsync(c => c.Name == name && c.Id != existingCountryId, cancellationToken);

        if (nameTaken)
        {
            throw new ConflictException($"A country named '{name}' already exists.");
        }
    }

    public async Task ValidateCanDeleteAsync(int countryId, CancellationToken cancellationToken)
    {
        var hasCities = await _context.Cities.AnyAsync(c => c.CountryId == countryId, cancellationToken);

        if (hasCities)
        {
            throw new ConflictException("Country cannot be deleted because it contains cities.");
        }
    }
}
