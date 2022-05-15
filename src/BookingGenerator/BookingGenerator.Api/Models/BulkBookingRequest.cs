namespace BookingGenerator.Api.Models;

public record BulkBookingRequest (List<BookingRequest> BookingRequests);
