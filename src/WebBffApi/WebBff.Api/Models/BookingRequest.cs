namespace BookingGenerator.Api.Models;

public record BookingRequest(string FirstName, string LastName, DateTime StartDate, DateTime EndDate, string Destination, decimal Price);
