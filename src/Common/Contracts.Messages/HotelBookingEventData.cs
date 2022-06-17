namespace Contracts.Messages;

public record HotelBookingEventData(int NumberOfBeds, bool BreakfastIncluded, bool LunchIncluded, bool DinnerIncluded);