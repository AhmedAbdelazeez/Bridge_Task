namespace BridgeTask.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, int id)
        : base($"{entityName} with id {id} was not found.")
    {
    }
}
