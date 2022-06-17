using Contracts.Messages;
using Contracts.Messages.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;
using WebBff.Application.Infrastructure;
using WebBff.Domain.Models;

namespace WebBff.Infrastructure;
public class AzureMessageBus : IMessageBus
{
    private readonly IBus _bus;
    private readonly ILogger<AzureMessageBus> _logger;

    public AzureMessageBus(IBus bus, ILogger<AzureMessageBus> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public async Task PublishBookingCreatedAsync(Booking booking)
    {
        try
        {
            await PublishEventAsync(booking);
        }
        catch (Exception ex)
        {
            LogError(ex, booking.BookingId);
            throw;
        }
    }

    private async Task PublishEventAsync(Booking booking) 
        => await _bus.Publish(MapToBookingCreated(booking), context =>
        {
            var correlationIdAsGuid = Guid.Parse(booking.BookingId);
            context.CorrelationId = correlationIdAsGuid;
            context.MessageId = correlationIdAsGuid;
        }, CancellationToken.None);

    private static BookingCreated MapToBookingCreated(Booking booking)
        => new(booking.BookingId, MapToBookingSummary(booking),MapToCarBooking(booking),MapToHotelBookingBooking(booking),
           MapToFlightBooking(booking));

    private static FlightBookingEventData MapToFlightBooking(Booking booking)
        => new(booking.FlightBooking.OutboundFlightTime,
            booking.FlightBooking.OutboundFlightNumber, booking.FlightBooking.InboundFlightTime,
            booking.FlightBooking.InboundFlightNumber);

    private static HotelBookingEventData MapToHotelBookingBooking(Booking booking)
        => new(booking.HotelBooking.NumberOfBeds, booking.HotelBooking.BreakfastIncluded,
            booking.HotelBooking.LunchIncluded, booking.HotelBooking.DinnerIncluded);

    private static CarBookingEventData MapToCarBooking(Booking booking)
        => new(booking.CarBooking.PickUpLocation,
           MapToAnotherEnum<Size>(booking.CarBooking.Size.ToString()),
           MapToAnotherEnum<Transmission>(booking.CarBooking.Transmission.ToString()));

    private static BookingSummaryEventData MapToBookingSummary(Booking booking)
        => new(booking.BookingSummary.FirstName,
            booking.BookingSummary.LastName, booking.BookingSummary.StartDate,
            booking.BookingSummary.EndDate, booking.BookingSummary.Destination,
            booking.BookingSummary.Price);

    private static T MapToAnotherEnum<T>(string value) where T : struct
        => Enum.Parse<T>(value);

    private void LogError(Exception ex, string bookingId)
        => _logger.LogError(ex, "CorrelationId: {correlationId}. Error sending BookingCreated message",
            bookingId);
}
