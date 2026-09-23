using BridgeTask.Application.Common;

namespace BridgeTask.Application.Countries;

public interface ICountryService
{
    Task<CountryDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<PagedResult<CountryDto>> GetPagedAsync(CountryFilterDto filter, CancellationToken cancellationToken);
    Task<CountryDto> CreateAsync(CountryCreateDto dto, CancellationToken cancellationToken);
    Task<CountryDto> UpdateAsync(int id, CountryUpdateDto dto, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
