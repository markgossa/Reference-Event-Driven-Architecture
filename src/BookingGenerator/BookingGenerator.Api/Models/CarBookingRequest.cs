using BookingGenerator.Api.Enums;

namespace BookingGenerator.Api.Models;

public record CarBookingRequest(string PickUpLocation, Size Size, Transmission Transmission);
