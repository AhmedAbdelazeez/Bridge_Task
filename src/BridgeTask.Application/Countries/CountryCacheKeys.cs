using BridgeTask.Application.Common.Caching;

namespace BridgeTask.Application.Countries;

public static class CountryCacheKeys
{
    public const string Region = "countries";

    public static CacheKey ById(int id) => new(Region, $"id:{id}");

    public static CacheKey List(CountryFilterDto filter) => new(Region, "list:" + CacheKey.Hash(new
    {
        filter.PageNumber,
        filter.PageSize,
        Search = CacheKey.Normalize(filter.Search),
        Name = CacheKey.Normalize(filter.Name),
        Code = CacheKey.Normalize(filter.Code)
    }));
}
