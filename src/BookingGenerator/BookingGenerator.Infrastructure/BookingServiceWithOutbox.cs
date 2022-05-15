using BookingGenerator.Application.Repositories;
using BookingGenerator.Domain.Models;
using Common.Messaging.CorrelationIdGenerator;
using Common.Messaging.Outbox;
using Common.Messaging.Outbox.Models;
using Microsoft.Extensions.Logging;

namespace BookingGenerator.Infrastructure;
public class BookingServiceWithOutbox : IBookingService
{
    private readonly IBookingService _bookingService;
    private readonly ICorrelationIdGenerator _correlationIdGenerator;
    private readonly IMessageOutbox<Booking> _messageOutbox;
    private readonly ILogger<BookingServiceWithOutbox> _logger;

    public BookingServiceWithOutbox(IBookingService bookingService,
        ICorrelationIdGenerator correlationIdGenerator, IMessageOutbox<Booking> messageOutbox,
        ILogger<BookingServiceWithOutbox> logger)
    {
        _bookingService = bookingService;
        _correlationIdGenerator = correlationIdGenerator;
        _messageOutbox = messageOutbox;
        _logger = logger;
    }

    public async Task BookAsync(Booking booking, string? correlationId = null)
    {
        correlationId = _correlationIdGenerator.CorrelationId;
        await _messageOutbox.AddAsync(BuildOutboxMessage(booking));
        
        try
        {
            await _bookingService.BookAsync(booking, correlationId);
        }
        catch (Exception ex)
        {
            await _messageOutbox.FailAsync(new List<string> { correlationId });
            LogWarning(correlationId, ex);

            return;
        }

        await _messageOutbox.RemoveAsync(new List<string> { correlationId });
    }

    public async Task ReplayBookingsAsync()
    {
        var successfulCorrelationIds = new List<string>();
        var failedCorrelationIds = new List<string>();
        foreach (var message in await _messageOutbox.GetAsync())
        {
            await TryReplayMessagesAsync(message, successfulCorrelationIds, failedCorrelationIds);
        }

        await _messageOutbox.RemoveAsync(successfulCorrelationIds);
        await _messageOutbox.FailAsync(failedCorrelationIds);
    }

    private void LogWarning(string correlationId, Exception ex) 
        => _logger.LogWarning(ex, "Message could not be processed. CorrelationId: {correlationId}",
            correlationId);

    private OutboxMessage<Booking> BuildOutboxMessage(Booking booking)
        => new(_correlationIdGenerator.CorrelationId, booking);

    private async Task TryReplayMessagesAsync(OutboxMessage<Booking> message,
        List<string> successfulCorrelationIds, List<string> failedCorrelationIds)
    {
        try
        {
            await _bookingService.BookAsync(message.MessageObject, message.CorrelationId);
            successfulCorrelationIds.Add(message.CorrelationId);
        }
        catch (Exception ex)
        {
            failedCorrelationIds.Add(message.CorrelationId);
            LogWarning(message.CorrelationId, ex);
        }
    }
}
