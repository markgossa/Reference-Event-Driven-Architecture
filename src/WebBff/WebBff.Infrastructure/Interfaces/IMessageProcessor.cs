namespace WebBff.Infrastructure.Interfaces;

public interface IMessageProcessor
{
    Task PublishBookingCreatedMessagesAsync();
}
