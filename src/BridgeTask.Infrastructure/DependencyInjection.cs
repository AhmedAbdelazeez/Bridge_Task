using BridgeTask.Application.Common;
using BridgeTask.Application.Common.Caching;
using BridgeTask.Infrastructure.Caching;
using BridgeTask.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BridgeTask.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        return services;
    }

    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(RedisSettings.SectionName);
        var settings = section.Get<RedisSettings>() ?? new RedisSettings();

        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException("Redis:ConnectionString is not configured.");
        }

        if (settings.DefaultExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("Redis:DefaultExpirationMinutes must be greater than zero.");
        }

        if (settings.FailureCooldownSeconds < 0)
        {
            throw new InvalidOperationException("Redis:FailureCooldownSeconds cannot be negative.");
        }

        services.Configure<RedisSettings>(section);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = settings.ConnectionString;
            options.InstanceName = settings.InstanceName;
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}
