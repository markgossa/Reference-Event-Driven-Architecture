using WebBff.Application.Repositories;
using WebBff.Domain.Models;
using MediatR;

namespace WebBff.Application.Services.Bookings.Commands.MakeBooking;

internal class MakeBookingCommandHandler : IRequestHandler<MakeBookingCommand>
{
    private readonly IBookingService _bookingService;

    public MakeBookingCommandHandler(IBookingService bookingService)
        => _bookingService = bookingService;

    public async Task<Unit> Handle(MakeBookingCommand makeBookingCommand, CancellationToken cancellationToken)
    {
        await _bookingService.BookAsync(MapToBooking(makeBookingCommand));

        return Unit.Value;
    }

    private static Booking MapToBooking(MakeBookingCommand makeBookingCommand)
        => new(makeBookingCommand.FirstName, makeBookingCommand.LastName, makeBookingCommand.StartDate,
            makeBookingCommand.EndDate, makeBookingCommand.Destination, makeBookingCommand.Price);
}
