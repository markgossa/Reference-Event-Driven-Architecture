using BookingGenerator.Domain.Models;
using System.Net.Http;

namespace BookingGenerator.Infrastructure.HttpClients;
public interface IWebBffHttpClient
{
    Task<HttpResponseMessage> PostAsync(Booking booking, string correlationId);
}
