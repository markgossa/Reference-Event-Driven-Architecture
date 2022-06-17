namespace BookingGenerator.Infrastructure.Models;

public record WebBffBookingSummaryRequest(string FirstName, string LastName, DateTime StartDate, DateTime EndDate, string Destination, int Price);
