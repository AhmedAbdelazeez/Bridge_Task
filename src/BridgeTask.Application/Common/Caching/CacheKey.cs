using System.Security.Cryptography;
using System.Text.Json;

namespace BridgeTask.Application.Common.Caching;

public readonly record struct CacheKey(string Region, string Name)
{
    public static string Hash(object filter)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(filter);
        return Convert.ToHexString(SHA256.HashData(json)).ToLowerInvariant();
    }

    public static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
