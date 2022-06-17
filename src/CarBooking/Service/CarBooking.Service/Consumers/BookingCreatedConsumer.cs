using CarBooking.Application.Repositories;
using Contracts.Messages;
using MassTransit;

namespace CarBooking.Service.Consumers;

public class BookingCreatedConsumer
{
    private readonly ICarBookingService _carBookingService;

    public BookingCreatedConsumer(ICarBookingService carBookingService)
        => _carBookingService = carBookingService;

    public async Task Consume(ConsumeContext<BookingCreated> context) 
        => await _carBookingService.SendAsync(MapToCarBooking(context));

    private static Domain.Models.CarBooking MapToCarBooking(ConsumeContext<BookingCreated> context)
    {
        var carBooking = context.Message.CarBooking;
        var bookingSummary = context.Message.BookingSummary;

        return new(context.Message.BookingId, bookingSummary.FirstName, bookingSummary.LastName,
            DateTime.UtcNow, DateTime.UtcNow, carBooking.PickUpLocation, 100,
            Enum.Parse<Domain.Enums.Size>(carBooking.Size.ToString()),
            Enum.Parse<Domain.Enums.Transmission>(carBooking.Transmission.ToString()));
    }
}
