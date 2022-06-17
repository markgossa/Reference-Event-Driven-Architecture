namespace Contracts.Messages;

public record FlightBookingEventData(DateTime OutboundFlightTime, string OutboundFlightNumber,
    DateTime InboundFlightTime, string InboundFlightNumber);
