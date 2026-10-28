namespace TimeTracker.Application.Common.Exceptions;

/// <summary>Public holidays couldn't be looked up; the message is safe to show to an employer.</summary>
public class PublicHolidayLookupException(string message, Exception? inner = null) : Exception(message, inner);
