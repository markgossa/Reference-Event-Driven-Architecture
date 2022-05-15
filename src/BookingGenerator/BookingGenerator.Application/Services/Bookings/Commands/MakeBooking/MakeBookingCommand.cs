using MediatR;

namespace BookingGenerator.Application.Services.Bookings.Commands.MakeBooking;

public record MakeBookingCommand(string FirstName, string LastName, DateOnly StartDate, DateOnly EndDate, string Destination, decimal Price) : IRequest;
