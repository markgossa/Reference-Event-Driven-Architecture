using BookingGenerator.Application.Repositories;
using BookingGenerator.Domain.Models;
using Common.Messaging.Folder;
using Common.Messaging.Folder.Models;
using Microsoft.Extensions.Logging;

namespace BookingGenerator.Infrastructure;
public class BookingReplayService : IBookingReplayService
{
    private readonly IBookingService _bookingService;
    private readonly IMessageOutbox<Booking> _messageOutbox;
    private readonly ILogger<BookingReplayService> _logger;

    public BookingReplayService(IBookingService bookingService, IMessageOutbox<Booking> messageOutbox, 
        ILogger<BookingReplayService> logger)
    {
        _bookingService = bookingService;
        _messageOutbox = messageOutbox;
        _logger = logger;
    }

    public async Task ReplayBookingsAsync()
    {
        var successfulCorrelationIds = new List<string>();
        var failedMessages = new List<Message<Booking>>();
        foreach (var message in await _messageOutbox.GetAsync())
        {
            await TryReplayMessagesAsync(message, successfulCorrelationIds, failedMessages);
        }

        await _messageOutbox.RemoveAsync(successfulCorrelationIds);
        await _messageOutbox.FailAsync(failedMessages);
    }

    private async Task TryReplayMessagesAsync(Message<Booking> message,
        List<string> successfulCorrelationIds, List<Message<Booking>> failedMessages)
    {
        try
        {
            await _bookingService.BookAsync(message.MessageObject, message.CorrelationId);
            successfulCorrelationIds.Add(message.CorrelationId);
        }
        catch (Exception ex)
        {
            failedMessages.Add(message);
            LogWarning(message.CorrelationId, ex);
        }
    }

    private void LogWarning(string correlationId, Exception ex)
       => _logger.LogWarning(ex, "Message could not be processed. CorrelationId: {correlationId}",
           correlationId);
}
