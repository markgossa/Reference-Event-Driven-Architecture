namespace BookingGenerator.Infrastructure.Models;
public class WebBffBookingRequest
{
    public string FirstName { get; }
    public string LastName { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public string Destination { get; }
    public decimal Price { get; }

    public WebBffBookingRequest(string firstName, string lastName, DateOnly startDate, 
        DateOnly endDate, string destination, decimal price)
    {
        FirstName = firstName;
        LastName = lastName;
        StartDate = startDate.ToDateTime(default);
        EndDate = endDate.ToDateTime(default);
        Destination = destination;
        Price = price;
    }
}
