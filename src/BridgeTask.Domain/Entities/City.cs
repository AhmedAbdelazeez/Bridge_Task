namespace BridgeTask.Domain.Entities;

public class City
{
    public const int NameMaxLength = 100;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int CountryId { get; set; }
    public Country Country { get; set; } = null!;
}
