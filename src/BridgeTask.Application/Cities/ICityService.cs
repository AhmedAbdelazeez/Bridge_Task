using BridgeTask.Application.Common;

namespace BridgeTask.Application.Cities;

public interface ICityService
{
    Task<CityDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<PagedResult<CityDto>> GetPagedAsync(CityFilterDto filter, CancellationToken cancellationToken);
    Task<PagedResult<CityDto>> GetByCountryIdAsync(int countryId, PagedRequest paging, CancellationToken cancellationToken);
    Task<CityDto> CreateAsync(CityCreateDto dto, CancellationToken cancellationToken);
    Task<CityDto> UpdateAsync(int id, CityUpdateDto dto, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
