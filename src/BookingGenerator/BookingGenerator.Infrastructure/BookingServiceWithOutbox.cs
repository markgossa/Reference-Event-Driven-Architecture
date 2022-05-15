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
            await _messageOutbox.FailAsync(correlationId);
            LogWarning(correlationId, ex);

            return;
        }

        await _messageOutbox.RemoveAsync(correlationId);
    }

    public async Task ReplayBookingsAsync()
    {
        var messages = await _messageOutbox.GetAsync();
        await ReplayMessagesAsync(messages);
    }

    private void LogWarning(string correlationId, Exception ex) 
        => _logger.LogWarning(ex, "Message could not be processed. CorrelationId: {correlationId}",
            correlationId);

    private OutboxMessage<Booking> BuildOutboxMessage(Booking booking)
        => new(_correlationIdGenerator.CorrelationId, booking);

    private async Task ReplayMessagesAsync(IEnumerable<OutboxMessage<Booking>> messages)
    {
        foreach (var message in messages)
        {
            try
            {
                await _bookingService.BookAsync(message.MessageObject, message.CorrelationId);
                await _messageOutbox.RemoveAsync(message.CorrelationId);
            }
            catch (Exception)
            {
            }
        }
    }
}
