using System.ComponentModel.DataAnnotations;
using BridgeTask.Domain.Entities;

namespace BridgeTask.Application.Cities;

public class CityUpdateDto
{
    [Required]
    [StringLength(City.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CountryId { get; set; }
}
