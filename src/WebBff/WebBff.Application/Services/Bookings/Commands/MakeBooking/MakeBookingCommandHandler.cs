using MediatR;
using WebBff.Application.Infrastructure;

namespace WebBff.Application.Services.Bookings.Commands.MakeBooking;

internal class MakeBookingCommandHandler : IRequestHandler<MakeBookingCommand>
{
    private readonly IMessageBusOutbox _messageBusOutbox;

    public MakeBookingCommandHandler(IMessageBusOutbox messageBus) 
        => _messageBusOutbox = messageBus;

    public async Task<Unit> Handle(MakeBookingCommand makeBookingCommand, CancellationToken cancellationToken)
    {
        await _messageBusOutbox.PublishBookingCreatedAsync(makeBookingCommand.Booking);

        return Unit.Value;
    }
}
