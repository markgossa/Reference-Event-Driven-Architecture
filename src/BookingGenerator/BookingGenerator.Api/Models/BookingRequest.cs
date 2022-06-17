#nullable disable

namespace BookingGenerator.Api.Models;

public record BookingRequest(BookingSummaryRequest BookingSummary, CarBookingRequest Car,
    HotelBookingRequest Hotel, FlightBookingRequest Flight);
