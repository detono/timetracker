namespace TimeTracker.Application.Common.Exceptions;

/// <summary>Email could not be delivered. The message is written to be shown to an employer as-is.</summary>
public class EmailDeliveryException : Exception
{
    public EmailDeliveryException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}
