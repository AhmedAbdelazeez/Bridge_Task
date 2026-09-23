using System.ComponentModel.DataAnnotations;
using BridgeTask.Application.Common;
using BridgeTask.Domain.Entities;

namespace BridgeTask.Application.Cities;

public class CityFilterDto : PagedRequest
{
    [StringLength(City.NameMaxLength)]
    public string? Search { get; set; }

    [StringLength(City.NameMaxLength)]
    public string? Name { get; set; }

    [Range(1, int.MaxValue)]
    public int? CountryId { get; set; }
}
