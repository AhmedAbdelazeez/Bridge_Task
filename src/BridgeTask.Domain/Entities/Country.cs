namespace BridgeTask.Domain.Entities;

public class Country
{
    public const int NameMaxLength = 100;
    public const int CodeMinLength = 2;
    public const int CodeMaxLength = 3;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public ICollection<City> Cities { get; set; } = new List<City>();
}
