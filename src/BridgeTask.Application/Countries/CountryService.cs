using System.Linq.Expressions;
using BridgeTask.Application.Common;
using BridgeTask.Application.Common.Exceptions;
using BridgeTask.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace BridgeTask.Application.Countries;

public class CountryService : ICountryService
{
    private static readonly Expression<Func<Country, CountryDto>> ToDto = country => new CountryDto
    {
        Id = country.Id,
        Name = country.Name,
        Code = country.Code
    };

    private readonly IAppDbContext _context;
    private readonly ICountryValidator _validator;

    public CountryService(IAppDbContext context, ICountryValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<CountryDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Countries
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Country), id);
    }

    public async Task<PagedResult<CountryDto>> GetPagedAsync(CountryFilterDto filter, CancellationToken cancellationToken)
    {
        var query = _context.Countries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            var name = filter.Name.Trim();
            query = query.Where(c => c.Name == name);
        }

        if (!string.IsNullOrWhiteSpace(filter.Code))
        {
            var code = filter.Code.Trim();
            query = query.Where(c => c.Code == code);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(c => c.Name.Contains(search) || c.Code.Contains(search));
        }

        return await query
            .Select(ToDto)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .ToPagedResultAsync(filter, cancellationToken);
    }

    public async Task<CountryDto> CreateAsync(CountryCreateDto dto, CancellationToken cancellationToken)
    {
        var country = dto.Adapt<Country>();

        await _validator.ValidateAsync(country.Name, country.Code, null, cancellationToken);

        _context.Countries.Add(country);
        await _context.SaveChangesAsync(cancellationToken);

        return country.Adapt<CountryDto>();
    }

    public async Task<CountryDto> UpdateAsync(int id, CountryUpdateDto dto, CancellationToken cancellationToken)
    {
        var country = await FindAsync(id, cancellationToken);

        dto.Adapt(country);

        await _validator.ValidateAsync(country.Name, country.Code, country.Id, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return country.Adapt<CountryDto>();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var country = await FindAsync(id, cancellationToken);

        await _validator.ValidateCanDeleteAsync(country.Id, cancellationToken);

        _context.Countries.Remove(country);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Country> FindAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Countries.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Country), id);
    }
}
