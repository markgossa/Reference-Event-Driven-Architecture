namespace WebBff.Domain.Models;

public record FlightBooking(DateTime OutboundFlightTime, string OutboundFlightNumber,
    DateTime InboundFlightTime, string InboundFlightNumber);
