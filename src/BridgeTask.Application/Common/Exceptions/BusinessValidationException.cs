namespace BridgeTask.Application.Common.Exceptions;

public class BusinessValidationException : Exception
{
    public BusinessValidationException(string message) : base(message)
    {
    }
}
