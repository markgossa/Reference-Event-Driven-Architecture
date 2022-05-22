namespace BookingGenerator.Api.Models;

public record BookingResponse(string FirstName, string LastName, DateTime StartDate, DateTime EndDate, string Destination, decimal Price);
