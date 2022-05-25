using WebBff.Domain.Models;

namespace WebBff.Application.Repositories;
public interface IBookingRepository
{
    Task SendBookingAsync(Booking booking);
}
