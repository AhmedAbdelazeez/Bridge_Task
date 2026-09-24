using System.Linq.Expressions;
using BridgeTask.Application.Common;
using BridgeTask.Application.Common.Caching;
using BridgeTask.Application.Common.Exceptions;
using BridgeTask.Domain.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace BridgeTask.Application.Cities;

public class CityService : ICityService
{
    private static readonly Expression<Func<City, CityDto>> ToDto = city => new CityDto
    {
        Id = city.Id,
        Name = city.Name,
        CountryId = city.CountryId,
        CountryName = city.Country.Name,
        CountryCode = city.Country.Code
    };

    private readonly IAppDbContext _context;
    private readonly ICityValidator _validator;
    private readonly ReferenceDataCache _cache;

    public CityService(IAppDbContext context, ICityValidator validator, ReferenceDataCache cache)
    {
        _context = context;
        _validator = validator;
        _cache = cache;
    }

    public Task<CityDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _cache.GetOrLoadAsync(CityCacheKeys.ById(id), token => LoadByIdAsync(id, token), cancellationToken);
    }

    public Task<PagedResult<CityDto>> GetPagedAsync(CityFilterDto filter, CancellationToken cancellationToken)
    {
        return _cache.GetOrLoadAsync(CityCacheKeys.List(filter), token => LoadPagedAsync(filter, token), cancellationToken);
    }

    public Task<PagedResult<CityDto>> GetByCountryIdAsync(int countryId, PagedRequest paging, CancellationToken cancellationToken)
    {
        return _cache.GetOrLoadAsync(
            CityCacheKeys.ByCountry(countryId, paging),
            token => LoadByCountryIdAsync(countryId, paging, token),
            cancellationToken);
    }

    public async Task<CityDto> CreateAsync(CityCreateDto dto, CancellationToken cancellationToken)
    {
        var city = dto.Adapt<City>();

        await _validator.ValidateAsync(city.Name, city.CountryId, null, cancellationToken);

        _context.Cities.Add(city);
        await _context.SaveChangesAsync(cancellationToken);

        await _cache.InvalidateAsync(CityCacheKeys.Region);

        return await LoadByIdAsync(city.Id, cancellationToken);
    }

    public async Task<CityDto> UpdateAsync(int id, CityUpdateDto dto, CancellationToken cancellationToken)
    {
        var city = await FindAsync(id, cancellationToken);

        dto.Adapt(city);

        await _validator.ValidateAsync(city.Name, city.CountryId, city.Id, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        // One region covers the city itself, the general lists and both the old and new country's lists.
        await _cache.InvalidateAsync(CityCacheKeys.Region);

        return await LoadByIdAsync(city.Id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var city = await FindAsync(id, cancellationToken);

        _context.Cities.Remove(city);
        await _context.SaveChangesAsync(cancellationToken);

        await _cache.InvalidateAsync(CityCacheKeys.Region);
    }

    private async Task<CityDto> LoadByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Cities
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(City), id);
    }

    private async Task<PagedResult<CityDto>> LoadPagedAsync(CityFilterDto filter, CancellationToken cancellationToken)
    {
        var query = _context.Cities.AsNoTracking();

        if (filter.CountryId.HasValue)
        {
            query = query.Where(c => c.CountryId == filter.CountryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            var name = filter.Name.Trim();
            query = query.Where(c => c.Name == name);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(c => c.Name.Contains(search));
        }

        return await query
            .Select(ToDto)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .ToPagedResultAsync(filter, cancellationToken);
    }

    private async Task<PagedResult<CityDto>> LoadByCountryIdAsync(int countryId, PagedRequest paging, CancellationToken cancellationToken)
    {
        var countryExists = await _context.Countries.AnyAsync(c => c.Id == countryId, cancellationToken);

        if (!countryExists)
        {
            throw new NotFoundException(nameof(Country), countryId);
        }

        var filter = new CityFilterDto
        {
            CountryId = countryId,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize
        };

        return await LoadPagedAsync(filter, cancellationToken);
    }

    private async Task<City> FindAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Cities.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(City), id);
    }
}
