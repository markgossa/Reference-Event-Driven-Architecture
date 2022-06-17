#nullable disable

namespace WebBff.Api.Models;

public record BookingRequest(string BookingId, BookingSummaryRequest BookingSummary, CarBookingRequest Car,
    HotelBookingRequest Hotel, FlightBookingRequest Flight);
