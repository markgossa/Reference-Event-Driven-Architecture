namespace BookingGenerator.Domain.Models;

public record Booking(string FirstName, string LastName, DateOnly StartDate, DateOnly EndDate, string Destination, decimal Price);
