using MediatR;
using WebBff.Domain.Models;

namespace WebBff.Application.Services.Bookings.Commands.MakeBooking;

public record MakeBookingCommand(Booking Booking) : IRequest;
