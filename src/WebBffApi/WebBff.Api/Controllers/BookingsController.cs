using WebBff.Api.Models;
using WebBff.Application.Services.Bookings.Commands.MakeBooking;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebBff.Api.Controllers;

[ApiVersion("1.0")]
[Route("[controller]")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class BookingsController : Controller
{
    private readonly IMediator _mediator;

    public BookingsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Makes a new booking
    /// </summary>
    /// <param name="bookingRequest"></param>
    /// <response code="202">Returns 202 ACCEPTED if the booking was received successfully</response>
    /// <response code="400">Returns 400 BAD REQUEST if the booking request was invalid</response>
    /// <response code="500">Returns 500 INTERNAL SERVER ERROR if the booking request was unsuccessful due to a server side error</response>
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> MakeBookingAsync([FromBody] BookingRequest bookingRequest)
    {
        await MakeBookingsAsync(bookingRequest);

        return Accepted(string.Empty, GenerateBookingResponse(bookingRequest));
    }

    private async Task MakeBookingsAsync(BookingRequest bookingRequest)
        => await _mediator.Send(MapToMakeBookingCommand(bookingRequest));

    private static MakeBookingCommand MapToMakeBookingCommand(BookingRequest bookingRequest)
        => new(bookingRequest.FirstName, bookingRequest.LastName, bookingRequest.StartDate,
            bookingRequest.EndDate, bookingRequest.Destination, bookingRequest.Price);

    private static BookingResponse GenerateBookingResponse(BookingRequest bookingRequest) 
        => new(bookingRequest.FirstName, bookingRequest.LastName,
            bookingRequest.StartDate, bookingRequest.EndDate, bookingRequest.Destination, bookingRequest.Price);
}
