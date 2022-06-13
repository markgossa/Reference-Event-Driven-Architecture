using CarBooking.Domain.Enums;
using MediatR;

namespace CarBooking.Application.Services.Bookings.Commands.MakeBooking;

public record MakeCarBookingCommand(string Id, string FirstName, string LastName, DateTime StartDate, 
    DateTime EndDate, string PickUpLocation, decimal Price, Size Size, Transmission Transmission) : IRequest;
