using CarBooking.Application.Repositories;
using MediatR;

namespace CarBooking.Application.Services.Bookings.Commands.MakeBooking;

internal class MakeCarBookingCommandHandler : IRequestHandler<MakeCarBookingCommand>
{
    private readonly ICarBookingRepository _carBookingRepository;

    public MakeCarBookingCommandHandler(ICarBookingRepository carBookingRepository)
        => _carBookingRepository = carBookingRepository;

    public async Task<Unit> Handle(MakeCarBookingCommand makeCarBookingCommand, CancellationToken cancellationToken)
    {
        await _carBookingRepository.SaveAsync(MapToBooking(makeCarBookingCommand));

        return Unit.Value;
    }

    private static Domain.Models.CarBooking MapToBooking(MakeCarBookingCommand makeCarBookingCommand)
        => new(makeCarBookingCommand.Id, makeCarBookingCommand.FirstName, makeCarBookingCommand.LastName, makeCarBookingCommand.StartDate,
            makeCarBookingCommand.EndDate, makeCarBookingCommand.PickUpLocation, makeCarBookingCommand.Price,
            makeCarBookingCommand.Size, makeCarBookingCommand.Transmission);
}
