namespace BookingGenerator.Api.Models;

public record FlightBookingRequest(DateTime OutboundFlightTime, string OutboundFlightNumber, 
    DateTime InboundFlightTime, string InboundFlightNumber);
