namespace WebBff.Api.Models;

public record HotelBookingRequest(int NumberOfBeds, bool BreakfastIncluded, bool LunchIncluded, bool DinnerIncluded);