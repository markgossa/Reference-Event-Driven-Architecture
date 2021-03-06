using AspNet.CorrelationIdGenerator;
using BookingGenerator.Api.Models;
using BookingGenerator.Application.Services.Bookings.Commands.MakeBooking;
using BookingGenerator.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookingGenerator.Api.Controllers;

[ApiVersion("1.0")]
[Route("[controller]")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class BookingsController : Controller
{
    private readonly IMediator _mediator;
    private readonly ICorrelationIdGenerator _correlationIdGenerator;

    public BookingsController(IMediator mediator, ICorrelationIdGenerator correlationIdGenerator)
    {
        _mediator = mediator;
        _correlationIdGenerator = correlationIdGenerator;
    }

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
        => await _mediator.Send(new MakeBookingCommand(MapToBooking(bookingRequest)));

    private BookingResponse GenerateBookingResponse(BookingRequest bookingRequest)
        => new(_correlationIdGenerator.Get(), bookingRequest.BookingSummary, bookingRequest.Car, 
            bookingRequest.Hotel, bookingRequest.Flight);

    private Booking MapToBooking(BookingRequest bookingRequest)
        => new(_correlationIdGenerator.Get(), MapToBookingSummary(bookingRequest), MapToCarBooking(bookingRequest), 
            MapToHotelBooking(bookingRequest), MapToFlightBooking(bookingRequest));

    private static FlightBooking MapToFlightBooking(BookingRequest bookingRequest)
        => new(bookingRequest.Flight.OutboundFlightTime,
            bookingRequest.Flight.OutboundFlightNumber, bookingRequest.Flight.InboundFlightTime,
            bookingRequest.Flight.InboundFlightNumber);

    private static HotelBooking MapToHotelBooking(BookingRequest bookingRequest)
        => new(bookingRequest.Hotel.NumberOfBeds, bookingRequest.Hotel.BreakfastIncluded,
            bookingRequest.Hotel.LunchIncluded, bookingRequest.Hotel.DinnerIncluded);

    private static CarBooking MapToCarBooking(BookingRequest bookingRequest)
        => new(bookingRequest.Car.PickUpLocation,
            MapToAnotherEnum<Domain.Enums.Size>(bookingRequest.Car.Size.ToString()),
            MapToAnotherEnum<Domain.Enums.Transmission>(bookingRequest.Car.Transmission.ToString()));

    private static BookingSummary MapToBookingSummary(BookingRequest bookingRequest)
        => new(bookingRequest.BookingSummary.FirstName,
            bookingRequest.BookingSummary.LastName, bookingRequest.BookingSummary.StartDate,
            bookingRequest.BookingSummary.EndDate, bookingRequest.BookingSummary.Destination,
            bookingRequest.BookingSummary.Price);

    private static T MapToAnotherEnum<T>(string value) where T : struct
        => Enum.Parse<T>(value);
}
