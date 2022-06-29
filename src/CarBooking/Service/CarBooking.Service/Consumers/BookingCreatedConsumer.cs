using CarBooking.Application.Services.CarBookings.Commands.MakeCarBooking;
using Contracts.Messages;
using MassTransit;
using MediatR;

namespace CarBooking.Service.Consumers;

public class BookingCreatedConsumer : IConsumer<BookingCreated>
{
    private readonly IMediator _mediator;

    public BookingCreatedConsumer(IMediator mediator)
        => _mediator = mediator;

    public async Task Consume(ConsumeContext<BookingCreated> context)
    {
        var bookingCreated = context.Message;
        await MakeCarBookingAsync(bookingCreated);
        await PublishCarBookedAsync(context, bookingCreated);
    }

    private async Task MakeCarBookingAsync(BookingCreated bookingCreated) 
        => await _mediator.Send(new MakeCarBookingCommand(MapToCarBooking(bookingCreated)));

    private static async Task PublishCarBookedAsync(ConsumeContext<BookingCreated> context, BookingCreated bookingCreated)
        => await context.Publish(MapToCarBooked(bookingCreated));
    
    private static CarBooked MapToCarBooked(BookingCreated bookingCreated) 
        => new(bookingCreated.BookingId, bookingCreated.BookingSummary,
                bookingCreated.CarBooking, bookingCreated.HotelBooking, bookingCreated.FlightBooking);

    private static Domain.Models.CarBooking MapToCarBooking(BookingCreated bookingCreated)
    {
        var carBooking = bookingCreated.CarBooking;
        var bookingSummary = bookingCreated.BookingSummary;

        return new(bookingCreated.BookingId, bookingSummary.FirstName, bookingSummary.LastName,
            DateTime.UtcNow, DateTime.UtcNow, carBooking.PickUpLocation, 100,
            Enum.Parse<Domain.Enums.Size>(carBooking.Size.ToString()),
            Enum.Parse<Domain.Enums.Transmission>(carBooking.Transmission.ToString()));
    }
}
