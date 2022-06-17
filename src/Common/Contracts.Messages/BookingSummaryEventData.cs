namespace Contracts.Messages;

public record BookingSummaryEventData(string FirstName, string LastName, DateTime StartDate, DateTime EndDate, string Destination, int Price);
