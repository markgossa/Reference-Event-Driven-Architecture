namespace WebBff.Api.Models;

public record BookingSummaryRequest(string FirstName, string LastName, DateTime StartDate, DateTime EndDate, string Destination, int Price);
