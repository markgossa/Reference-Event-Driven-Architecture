#nullable disable

using CarBooking.Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarBooking.Infrastructure.Models;

[Index(nameof(CarBookingId), IsUnique = true)]
public class CarBookingRow
{
    public int Id { get; set; }
    public string CarBookingId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PickUpLocation { get; set; }
    public decimal Price { get; set; }
    public CarSize Size { get; set; }
    public CarTransmission Transmission { get; set; }
}