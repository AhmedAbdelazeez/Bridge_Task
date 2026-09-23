using BridgeTask.Application.Cities;
using BridgeTask.Application.Countries;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace BridgeTask.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);

        services.AddScoped<ICountryValidator, CountryValidator>();
        services.AddScoped<ICityValidator, CityValidator>();

        services.AddScoped<ICountryService, CountryService>();
        services.AddScoped<ICityService, CityService>();

        return services;
    }
}
