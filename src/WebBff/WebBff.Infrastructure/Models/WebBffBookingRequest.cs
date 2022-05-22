namespace WebBff.Infrastructure.Models;
public class WebBffBookingRequest
{
    public string FirstName { get; }
    public string LastName { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public string Destination { get; }
    public decimal Price { get; }

    public WebBffBookingRequest(string firstName, string lastName, DateTime startDate,
        DateTime endDate, string destination, decimal price)
    {
        FirstName = firstName;
        LastName = lastName;
        StartDate = startDate;
        EndDate = endDate;
        Destination = destination;
        Price = price;
    }
}
