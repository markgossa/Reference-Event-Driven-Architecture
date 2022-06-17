using BookingGenerator.Domain.Models;
using MediatR;

namespace BookingGenerator.Application.Services.Bookings.Commands.MakeBooking;

public record MakeBookingCommand(Booking Booking) : IRequest;
