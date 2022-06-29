using CarBooking.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CarBooking.Api.Models;

public record CarBookingRequest([Required] string BookingId, string FirstName, string LastName, 
    DateTime StartDate, DateTime EndDate, string PickUpLocation, 
    decimal Price, Size Size, Transmission Transmission);
