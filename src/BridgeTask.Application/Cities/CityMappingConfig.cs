using BridgeTask.Domain.Entities;
using Mapster;

namespace BridgeTask.Application.Cities;

public class CityMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CityCreateDto, City>()
            .Map(dest => dest.Name, src => src.Name.Trim());

        config.NewConfig<CityUpdateDto, City>()
            .Map(dest => dest.Name, src => src.Name.Trim());
    }
}
