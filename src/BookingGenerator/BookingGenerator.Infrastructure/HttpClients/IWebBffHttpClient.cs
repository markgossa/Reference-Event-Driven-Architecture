using BookingGenerator.Domain.Models;

namespace BookingGenerator.Infrastructure.HttpClients;
public interface IWebBffHttpClient
{
    Task PostAsync(Booking booking, string correlationId);
}
