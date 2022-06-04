using AspNet.CorrelationIdGenerator;
using Contracts.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;
using WebBff.Application.Repositories;
using WebBff.Domain.Models;

namespace WebBff.Infrastructure;
public class BookingServiceBusRepository : IBookingRepository
{
    private readonly IBus _bus;
    private readonly ICorrelationIdGenerator _correlationIdGenerator;
    private readonly ILogger<BookingServiceBusRepository> _logger;

    public BookingServiceBusRepository(IBus bus, ICorrelationIdGenerator correlationIdGenerator,
        ILogger<BookingServiceBusRepository> logger)
    {
        _bus = bus;
        _correlationIdGenerator = correlationIdGenerator;
        _logger = logger;
    }

    public async Task SendBookingAsync(Booking booking)
    {
        try
        {
            await PublishBookingCreatedAsync(booking);
        }
        catch (Exception ex)
        {
            LogError(ex);
            throw;
        }
    }

    private async Task PublishBookingCreatedAsync(Booking booking) 
        => await _bus.Publish(MapToBookingCreated(booking), AddCorrelationId, CancellationToken.None);

    private static BookingCreated MapToBookingCreated(Booking booking)
        => new(booking.FirstName, booking.LastName, booking.StartDate, booking.EndDate,
            booking.Destination, booking.Price);

    private void AddCorrelationId(PublishContext<BookingCreated> context)
    {
        context.CorrelationId = Guid.Parse(_correlationIdGenerator.Get());
        context.MessageId = Guid.Parse(_correlationIdGenerator.Get());
    }

    private void LogError(Exception ex)
        => _logger.LogError(ex, "CorrelationId: {correlationId}. Error sending BookingCreated message",
            _correlationIdGenerator.Get());
}
