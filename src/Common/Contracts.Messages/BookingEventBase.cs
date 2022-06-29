namespace Contracts.Messages;
public record BookingEventBase(string BookingId, BookingSummaryEventData BookingSummary,
    CarBookingEventData CarBooking, HotelBookingEventData HotelBooking, FlightBookingEventData FlightBooking);
