using CarBooking.Domain.Enums;

namespace CarBooking.Api.Models;

public record CarBookingResponse(string BookingId, string FirstName, string LastName, DateTime StartDate, 
    DateTime EndDate, string PickUpLocation, decimal Price, Size Size, Transmission Transmission);
