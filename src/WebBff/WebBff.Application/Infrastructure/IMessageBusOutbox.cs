using WebBff.Domain.Models;

namespace WebBff.Application.Infrastructure;
public interface IMessageBusOutbox
{
    Task PublishBookingCreatedAsync(Booking booking);
}
