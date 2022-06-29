using CarBooking.Api.Models;
using CarBooking.Application.Services.Bookings.Commands.MakeBooking;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CarBooking.Api.Controllers;

[ApiVersion("1.0")]
[Route("[controller]")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class CarBookingsController : Controller
{
    private readonly IMediator _mediator;

    public CarBookingsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Makes a new booking
    /// </summary>
    /// <param name="carBookingRequest"></param>
    /// <response code="200">Returns 200 OK if the booking was received successfully</response>
    /// <response code="400">Returns 400 BAD REQUEST if the booking request was invalid</response>
    /// <response code="500">Returns 500 INTERNAL SERVER ERROR if the booking request was unsuccessful due to a server side error</response>
    [HttpPost]
    public async Task<ActionResult<CarBookingResponse>> MakeBookingAsync([FromBody] CarBookingRequest carBookingRequest)
    {
        await MakeBookingsAsync(carBookingRequest);

        SetCorrelationIdHeader(carBookingRequest);

        return new OkObjectResult(GenerateBookingResponse(carBookingRequest));
    }

    private void SetCorrelationIdHeader(CarBookingRequest carBookingRequest) 
        => HttpContext.Response.Headers.Add("X-Correlation-Id", carBookingRequest.BookingId);

    private async Task MakeBookingsAsync(CarBookingRequest carBookingRequest)
        => await _mediator.Send(MapToMakeBookingCommand(carBookingRequest));

    private static MakeCarBookingCommand MapToMakeBookingCommand(CarBookingRequest carBookingRequest)
        => new(carBookingRequest.BookingId, carBookingRequest.FirstName, carBookingRequest.LastName, carBookingRequest.StartDate,
            carBookingRequest.EndDate, carBookingRequest.PickUpLocation, carBookingRequest.Price,
            carBookingRequest.Size, carBookingRequest.Transmission);

    private static CarBookingResponse GenerateBookingResponse(CarBookingRequest carBookingRequest)
        => new(carBookingRequest.BookingId, carBookingRequest.FirstName, carBookingRequest.LastName,
            carBookingRequest.StartDate, carBookingRequest.EndDate, carBookingRequest.PickUpLocation,
            carBookingRequest.Price, carBookingRequest.Size, carBookingRequest.Transmission);
}
