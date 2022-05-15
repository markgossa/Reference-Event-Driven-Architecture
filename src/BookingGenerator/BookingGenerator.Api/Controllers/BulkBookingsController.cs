using BookingGenerator.Api.DateTimeExtensions;
using BookingGenerator.Api.Models;
using BookingGenerator.Application.Services.Bookings.Commands.MakeBooking;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookingGenerator.Api.Controllers;

[ApiVersion("1.0")]
[Route("[controller]")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class BulkBookingsController : Controller
{
    private readonly IMediator _mediator;

    public BulkBookingsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Makes a new booking
    /// </summary>
    /// <param name="bulkBookingRequest"></param>
    /// <response code="202">Returns 202 ACCEPTED if the booking was received successfully</response>
    /// <response code="400">Returns 400 BAD REQUEST if the booking request was invalid</response>
    /// <response code="500">Returns 500 INTERNAL SERVER ERROR if the booking request was unsuccessful due to a server side error</response>
    [HttpPost]
    public async Task<ActionResult<BulkBookingResponse>> MakeBulkBookingAsync([FromBody] BulkBookingRequest bulkBookingRequest)
    {
        await MakeBookingsAsync(bulkBookingRequest.BookingRequests);

        return Accepted(string.Empty, GenerateBulkBookingResponse(bulkBookingRequest.BookingRequests));
    }

    private async Task MakeBookingsAsync(IEnumerable<BookingRequest> bookingRequests)
    {
        var tasks = new List<Task>();
        foreach (var bookingRequest in bookingRequests)
        {
            tasks.Add(_mediator.Send(MapToMakeBookingCommand(bookingRequest)));
        }

        await Task.WhenAll(tasks);
    }

    private static MakeBookingCommand MapToMakeBookingCommand(BookingRequest bookingRequest)
        => new(bookingRequest.FirstName, bookingRequest.LastName, bookingRequest.StartDate.ToDateOnly(),
            bookingRequest.EndDate.ToDateOnly(), bookingRequest.Destination, bookingRequest.Price);

    private static BulkBookingResponse GenerateBulkBookingResponse(List<BookingRequest> bookingRequests)
    {
        var bookingResponses = new List<BookingResponse>();
        foreach (var bookingRequest in bookingRequests)
        {
            bookingResponses.Add(new BookingResponse(bookingRequest.FirstName, bookingRequest.LastName,
                bookingRequest.StartDate, bookingRequest.EndDate , bookingRequest.Destination, bookingRequest.Price));
        }

        return new(bookingResponses);
    }
}
