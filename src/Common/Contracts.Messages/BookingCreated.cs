namespace Contracts.Messages;
public record BookingCreated(string BookingId, BookingSummaryEventData BookingSummary, 
    CarBookingEventData CarBooking, HotelBookingEventData HotelBooking, FlightBookingEventData FlightBooking)
        : BookingEventBase(BookingId, BookingSummary, CarBooking, HotelBooking, FlightBooking);