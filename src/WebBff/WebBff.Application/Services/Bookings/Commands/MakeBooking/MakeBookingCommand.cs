using MediatR;

namespace WebBff.Application.Services.Bookings.Commands.MakeBooking;

public record MakeBookingCommand(string FirstName, string LastName, DateTime StartDate, DateTime EndDate, string Destination, decimal Price) : IRequest;
