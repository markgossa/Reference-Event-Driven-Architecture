using WebBff.Domain.Models;

namespace WebBff.Application.Infrastructure;
public interface IMessageBus
{
    Task PublishBookingCreatedAsync(Booking booking);
}
