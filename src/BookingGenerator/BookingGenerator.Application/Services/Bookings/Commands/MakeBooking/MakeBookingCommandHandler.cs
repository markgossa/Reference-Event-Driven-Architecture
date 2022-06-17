using BookingGenerator.Application.Repositories;
using MediatR;

namespace BookingGenerator.Application.Services.Bookings.Commands.MakeBooking;

internal class MakeBookingCommandHandler : IRequestHandler<MakeBookingCommand>
{
    private readonly IBookingService _bookingService;

    public MakeBookingCommandHandler(IBookingService bookingService)
        => _bookingService = bookingService;

    public async Task<Unit> Handle(MakeBookingCommand makeBookingCommand, CancellationToken cancellationToken)
    {
        await _bookingService.BookAsync(makeBookingCommand.Booking);

        return Unit.Value;
    }
}
