namespace WebBff.Domain.Models;

public record Booking(string FirstName, string LastName, DateTime StartDate, DateTime EndDate, string Destination, decimal Price);
