using BridgeTask.Domain.Entities;
using Mapster;

namespace BridgeTask.Application.Countries;

public class CountryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CountryCreateDto, Country>()
            .Map(dest => dest.Name, src => src.Name.Trim())
            .Map(dest => dest.Code, src => src.Code.Trim().ToUpperInvariant());

        config.NewConfig<CountryUpdateDto, Country>()
            .Map(dest => dest.Name, src => src.Name.Trim())
            .Map(dest => dest.Code, src => src.Code.Trim().ToUpperInvariant());
    }
}
