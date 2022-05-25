using MediatR;
using WebBff.Application.Repositories;
using WebBff.Domain.Models;

namespace WebBff.Application.Services.Bookings.Commands.MakeBooking;

internal class MakeBookingCommandHandler : IRequestHandler<MakeBookingCommand>
{
    private readonly IBookingRepository _bookingRepository;

    public MakeBookingCommandHandler(IBookingRepository bookingRepository) 
        => _bookingRepository = bookingRepository;

    public async Task<Unit> Handle(MakeBookingCommand makeBookingCommand, CancellationToken cancellationToken)
    {
        await _bookingRepository.SendBookingAsync(MapToBooking(makeBookingCommand));

        return Unit.Value;
    }

    private static Booking MapToBooking(MakeBookingCommand makeBookingCommand) 
        => new(makeBookingCommand.FirstName, makeBookingCommand.LastName, 
            makeBookingCommand.StartDate, makeBookingCommand.EndDate,
            makeBookingCommand.Destination, makeBookingCommand.Price);
}
