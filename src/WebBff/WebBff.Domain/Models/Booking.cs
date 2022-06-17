namespace WebBff.Domain.Models;

public record Booking(string BookingId, BookingSummary BookingSummary, 
    CarBooking CarBooking, HotelBooking HotelBooking, FlightBooking FlightBooking);
