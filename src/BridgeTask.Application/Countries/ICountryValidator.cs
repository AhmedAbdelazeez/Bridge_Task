namespace BridgeTask.Application.Countries;

public interface ICountryValidator
{
    Task ValidateAsync(string name, string code, int? existingCountryId, CancellationToken cancellationToken);
    Task ValidateCanDeleteAsync(int countryId, CancellationToken cancellationToken);
}
