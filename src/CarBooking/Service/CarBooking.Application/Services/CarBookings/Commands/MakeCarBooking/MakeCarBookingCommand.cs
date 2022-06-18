using MediatR;

namespace CarBooking.Application.Services.CarBookings.Commands.MakeCarBooking;

public record MakeCarBookingCommand(Domain.Models.CarBooking CarBooking) : IRequest;
