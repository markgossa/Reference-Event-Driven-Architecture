using MediatR;
using WebBff.Application.Infrastructure;

namespace WebBff.Application.Services.Bookings.Commands.MakeBooking;

internal class MakeBookingCommandHandler : IRequestHandler<MakeBookingCommand>
{
    private readonly IMessageBusOutbox _messageBus;

    public MakeBookingCommandHandler(IMessageBusOutbox messageBus) 
        => _messageBus = messageBus;

    public async Task<Unit> Handle(MakeBookingCommand makeBookingCommand, CancellationToken cancellationToken)
    {
        await _messageBus.PublishBookingCreatedAsync(makeBookingCommand.Booking);

        return Unit.Value;
    }
}
