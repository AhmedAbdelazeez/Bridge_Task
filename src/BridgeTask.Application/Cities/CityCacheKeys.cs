using BridgeTask.Application.Common;
using BridgeTask.Application.Common.Caching;

namespace BridgeTask.Application.Cities;

public static class CityCacheKeys
{
    public const string Region = "cities";

    public static CacheKey ById(int id) => new(Region, $"id:{id}");

    public static CacheKey List(CityFilterDto filter) => new(Region, "list:" + CacheKey.Hash(new
    {
        filter.PageNumber,
        filter.PageSize,
        filter.CountryId,
        Search = CacheKey.Normalize(filter.Search),
        Name = CacheKey.Normalize(filter.Name)
    }));

    public static CacheKey ByCountry(int countryId, PagedRequest paging) =>
        new(Region, $"country:{countryId}:list:" + CacheKey.Hash(new { paging.PageNumber, paging.PageSize }));
}
