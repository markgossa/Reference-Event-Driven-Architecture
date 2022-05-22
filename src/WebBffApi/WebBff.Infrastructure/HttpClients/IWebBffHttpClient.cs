using WebBff.Domain.Models;

namespace WebBff.Infrastructure.HttpClients;
public interface IWebBffHttpClient
{
    Task PostAsync(Booking booking, string correlationId);
}
