using WebBff.Domain.Models;

namespace WebBff.Application.Repositories;

public interface IBookingService
{
    Task BookAsync(Booking booking, string? correlationId = null);
}
