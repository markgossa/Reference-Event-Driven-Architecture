using MediatR;

namespace WebBff.Application.Services.Bookings.Commands.MakeBooking;

internal class MakeBookingCommandHandler : IRequestHandler<MakeBookingCommand>
{
    public Task<Unit> Handle(MakeBookingCommand request, CancellationToken cancellationToken) => throw new NotImplementedException();
}
