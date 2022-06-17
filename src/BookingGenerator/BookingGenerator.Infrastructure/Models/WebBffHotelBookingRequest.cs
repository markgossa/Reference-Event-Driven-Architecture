namespace BookingGenerator.Infrastructure.Models;

public record WebBffHotelBookingRequest(int NumberOfBeds, bool BreakfastIncluded, bool LunchIncluded, bool DinnerIncluded);