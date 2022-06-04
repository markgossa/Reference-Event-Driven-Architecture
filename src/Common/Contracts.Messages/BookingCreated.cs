namespace Contracts.Messages;
public record BookingCreated(string FirstName, string LastName, DateTime StartDate, DateTime EndDate, string Destination, decimal Price);