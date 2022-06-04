using AspNet.CorrelationIdGenerator;
using BookingGenerator.Application.Repositories;
using BookingGenerator.Domain.Models;
using Common.Messaging.Folder;
using Common.Messaging.Folder.Models;
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
        correlationId = _correlationIdGenerator.Get();
        var outboxMessage = BuildOutboxMessage(booking);
        await _messageOutbox.AddAsync(outboxMessage);
        
        try
        {
            await _bookingService.BookAsync(booking, correlationId);
        }
        catch (Exception ex)
        {
            await _messageOutbox.FailAsync(new List<Message<Booking>> { outboxMessage });
            LogWarning(correlationId, ex);

            return;
        }

        await _messageOutbox.RemoveAsync(new List<string> { correlationId });
    }

    private void LogWarning(string correlationId, Exception ex) 
        => _logger.LogWarning(ex, "Message could not be processed. CorrelationId: {correlationId}",
            correlationId);

    private Message<Booking> BuildOutboxMessage(Booking booking)
        => new(_correlationIdGenerator.Get(), booking);
}
