namespace BookingGenerator.Domain.Models;

public record BookingSummary(string FirstName, string LastName, DateTime StartDate, DateTime EndDate, string Destination, int Price);
