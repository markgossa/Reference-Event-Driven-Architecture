using FluentValidation;

namespace CarBooking.Application.Services.CarBookings.Commands.MakeCarBooking;
public class MakeCarBookingCommandValidator : AbstractValidator<MakeCarBookingCommand>
{
    public MakeCarBookingCommandValidator() 
        => RuleFor(v => v.CarBooking.BookingId).MinimumLength(4).WithMessage("BookingId cannot be empty");
}
