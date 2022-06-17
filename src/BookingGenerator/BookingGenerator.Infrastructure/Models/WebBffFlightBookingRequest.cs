namespace BookingGenerator.Infrastructure.Models;

public record WebBffFlightBookingRequest(DateTime OutboundFlightTime, string OutboundFlightNumber,
    DateTime InboundFlightTime, string InboundFlightNumber);
