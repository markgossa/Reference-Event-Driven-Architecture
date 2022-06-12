using Common.Messaging.Folder;
using Common.Messaging.Folder.Models;
using Microsoft.Extensions.Logging;
using WebBff.Application.Infrastructure;
using WebBff.Domain.Models;
using WebBff.Infrastructure.Interfaces;

namespace WebBff.Infrastructure;
public class MessageProcessor : IMessageProcessor
{
    private readonly IMessageBus _messageBus;
    private readonly IMessageOutbox<Booking> _messageOutbox;
    private readonly ILogger<MessageProcessor> _logger;

    public MessageProcessor(IMessageBus messageBus, IMessageOutbox<Booking> messageOutbox, 
        ILogger<MessageProcessor> logger)
    {
        _messageBus = messageBus;
        _messageOutbox = messageOutbox;
        _logger = logger;
    }

    public async Task PublishBookingCreatedMessagesAsync()
    {
        var outboxMessages = await _messageOutbox.GetAndLockAsync(4);
        foreach (var outboxMessage in outboxMessages)
        {
            await PublishMessageAsync(outboxMessage);
        }
    }

    private async Task PublishMessageAsync(Message<Booking> outboxMessage)
    {
        try
        {
            await AttemptToPublishMessageAsync(outboxMessage);
        }
        catch (Exception ex)
        {
            await SetMessageAsFailedAsync(outboxMessage);
            LogWarning(outboxMessage, ex);
        }
    }

    private async Task AttemptToPublishMessageAsync(Message<Booking> outboxMessage)
    {
        await _messageBus.PublishBookingCreatedAsync(outboxMessage.MessageObject);
        await _messageOutbox.CompleteAsync(new List<Message<Booking>> { outboxMessage });
    }
    
    private async Task SetMessageAsFailedAsync(Message<Booking> outboxMessage) 
        => await _messageOutbox.FailAsync(new List<Message<Booking>> { outboxMessage });

    private void LogWarning(Message<Booking> outboxMessage, Exception ex) 
        => _logger.LogWarning(ex, "CorrelationId: {correlationId}. " +
            "Publish BookingCreated failed.", outboxMessage.CorrelationId);
}
