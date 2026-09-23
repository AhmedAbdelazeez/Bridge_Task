using System.ComponentModel.DataAnnotations;
using BridgeTask.Domain.Entities;

namespace BridgeTask.Application.Countries;

public class CountryCreateDto
{
    [Required]
    [StringLength(Country.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(Country.CodeMaxLength, MinimumLength = Country.CodeMinLength)]
    [RegularExpression("^[A-Za-z]+$", ErrorMessage = "The Code field must contain letters only.")]
    public string Code { get; set; } = string.Empty;
}
