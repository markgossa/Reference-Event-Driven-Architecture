namespace BookingGenerator.Domain.Models;

public record HotelBooking(int NumberOfBeds, bool BreakfastIncluded, bool LunchIncluded, bool DinnerIncluded);