using BookingGenerator.Infrastructure.Enums;

namespace BookingGenerator.Infrastructure.Models;

public record WebBffCarBookingRequest(string PickUpLocation, Size Size, Transmission Transmission);
