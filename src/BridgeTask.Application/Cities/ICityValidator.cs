namespace BridgeTask.Application.Cities;

public interface ICityValidator
{
    Task ValidateAsync(string name, int countryId, int? existingCityId, CancellationToken cancellationToken);
}
