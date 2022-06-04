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
        foreach (var message in await _messageOutbox.GetAndLockAsync(4))
        {
            await TryReplayMessagesAsync(message);
        }
    }

    private async Task TryReplayMessagesAsync(Message<Booking> message)
    {
        try
        {
            await ReplayMessageAsync(message);
        }
        catch (Exception ex)
        {
            await SetMessageAsFailedAsync(message, ex);
        }
    }

    private async Task ReplayMessageAsync(Message<Booking> message)
    {
        await _bookingService.BookAsync(message.MessageObject, message.CorrelationId);
        await _messageOutbox.RemoveAsync(new List<string> { message.CorrelationId });
    }
    
    private async Task SetMessageAsFailedAsync(Message<Booking> message, Exception ex)
    {
        await _messageOutbox.FailAsync(new List<Message<Booking>> { message });
        LogWarning(message.CorrelationId, ex);
    }

    private void LogWarning(string correlationId, Exception ex)
       => _logger.LogWarning(ex, "Message could not be processed. CorrelationId: {correlationId}",
            correlationId);
}
