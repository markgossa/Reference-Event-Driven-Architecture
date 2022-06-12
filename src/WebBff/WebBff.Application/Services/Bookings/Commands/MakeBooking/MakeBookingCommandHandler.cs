using MediatR;
using WebBff.Application.Infrastructure;
using WebBff.Domain.Models;

namespace WebBff.Application.Services.Bookings.Commands.MakeBooking;

internal class MakeBookingCommandHandler : IRequestHandler<MakeBookingCommand>
{
    private readonly IMessageBusOutbox _messageBus;

    public MakeBookingCommandHandler(IMessageBusOutbox messageBus) 
        => _messageBus = messageBus;

    public async Task<Unit> Handle(MakeBookingCommand makeBookingCommand, CancellationToken cancellationToken)
    {
        await _messageBus.PublishBookingCreatedAsync(MapToBooking(makeBookingCommand));

        return Unit.Value;
    }

    private static Booking MapToBooking(MakeBookingCommand makeBookingCommand) 
        => new(makeBookingCommand.FirstName, makeBookingCommand.LastName, 
            makeBookingCommand.StartDate, makeBookingCommand.EndDate,
            makeBookingCommand.Destination, makeBookingCommand.Price);
}
