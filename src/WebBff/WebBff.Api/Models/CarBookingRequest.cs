using WebBff.Api.Enums;

namespace WebBff.Api.Models;

public record CarBookingRequest(string PickUpLocation, Size Size, Transmission Transmission);
