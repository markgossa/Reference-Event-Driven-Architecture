namespace BookingGenerator.Api.Models;

public record BookingResponse(string BookingId, BookingSummaryRequest BookingSummary, 
    CarBookingRequest Car, HotelBookingRequest Hotel, FlightBookingRequest Flight);
