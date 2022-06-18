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
        => await _mediator.Send(new MakeCarBookingCommand(MapToCarBooking(context)));

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
