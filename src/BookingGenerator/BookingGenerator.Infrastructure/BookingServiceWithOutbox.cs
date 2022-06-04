using AspNet.CorrelationIdGenerator;
using BookingGenerator.Application.Repositories;
using BookingGenerator.Domain.Models;
using Common.Messaging.Folder;
using Common.Messaging.Folder.Models;

namespace BookingGenerator.Infrastructure;
public class BookingServiceWithOutbox : IBookingService
{
    private readonly ICorrelationIdGenerator _correlationIdGenerator;
    private readonly IMessageOutbox<Booking> _messageOutbox;

    public BookingServiceWithOutbox(ICorrelationIdGenerator correlationIdGenerator, 
        IMessageOutbox<Booking> messageOutbox)
    {
        _correlationIdGenerator = correlationIdGenerator;
        _messageOutbox = messageOutbox;
    }

    public async Task BookAsync(Booking booking, string? correlationId = null) 
        => await _messageOutbox.AddAsync(BuildOutboxMessage(booking));

    private Message<Booking> BuildOutboxMessage(Booking booking)
        => new(_correlationIdGenerator.Get(), booking);
}
