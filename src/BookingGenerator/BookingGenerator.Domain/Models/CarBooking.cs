using BookingGenerator.Domain.Enums;

namespace BookingGenerator.Domain.Models;

public record CarBooking(string PickUpLocation, Size Size, Transmission Transmission);
