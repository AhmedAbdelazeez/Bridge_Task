using BridgeTask.Application.Cities;
using BridgeTask.Application.Countries;
using Microsoft.Extensions.DependencyInjection;

namespace BridgeTask.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICountryValidator, CountryValidator>();
        services.AddScoped<ICityValidator, CityValidator>();

        return services;
    }
}
