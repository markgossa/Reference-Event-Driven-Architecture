using CarBooking.Application.Repositories;
using MediatR;

namespace CarBooking.Application.Services.CarBookings.Commands.MakeCarBooking;

internal class MakeCarBookingCommandHandler : IRequestHandler<MakeCarBookingCommand>
{
    private readonly ICarBookingService _carBookingService;

    public MakeCarBookingCommandHandler(ICarBookingService carBookingService)
        => _carBookingService = carBookingService;

    public async Task<Unit> Handle(MakeCarBookingCommand makeCarBookingCommand, CancellationToken cancellationToken)
    {
        await _carBookingService.SendAsync(MapToBooking(makeCarBookingCommand));

        return Unit.Value;
    }

    private static Domain.Models.CarBooking MapToBooking(MakeCarBookingCommand makeCarBookingCommand)
    {
        var carBooking = makeCarBookingCommand.CarBooking;
        return new(carBooking.BookingId, carBooking.FirstName, carBooking.LastName, carBooking.StartDate,
                carBooking.EndDate, carBooking.PickUpLocation, carBooking.Price,
                carBooking.Size, carBooking.Transmission);
    }
}
