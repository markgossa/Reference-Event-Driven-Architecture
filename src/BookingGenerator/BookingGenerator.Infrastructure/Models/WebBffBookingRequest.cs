#nullable disable

namespace BookingGenerator.Infrastructure.Models;

public record WebBffBookingRequest(string BookingId, WebBffBookingSummaryRequest BookingSummary, 
    WebBffCarBookingRequest Car, WebBffHotelBookingRequest Hotel, WebBffFlightBookingRequest Flight);
