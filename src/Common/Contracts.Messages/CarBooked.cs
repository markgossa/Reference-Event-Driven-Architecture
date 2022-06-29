namespace Contracts.Messages;
public record CarBooked(string BookingId, BookingSummaryEventData BookingSummary,
    CarBookingEventData CarBooking, HotelBookingEventData HotelBooking, FlightBookingEventData FlightBooking)
        : BookingEventBase(BookingId, BookingSummary, CarBooking, HotelBooking, FlightBooking);
