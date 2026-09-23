using System.ComponentModel.DataAnnotations;
using BridgeTask.Application.Common;
using BridgeTask.Domain.Entities;

namespace BridgeTask.Application.Countries;

public class CountryFilterDto : PagedRequest
{
    [StringLength(Country.NameMaxLength)]
    public string? Search { get; set; }

    [StringLength(Country.NameMaxLength)]
    public string? Name { get; set; }

    [StringLength(Country.CodeMaxLength)]
    public string? Code { get; set; }
}
