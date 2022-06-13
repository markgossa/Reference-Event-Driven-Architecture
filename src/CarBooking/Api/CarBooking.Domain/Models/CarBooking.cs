﻿using CarBooking.Domain.Enums;

namespace CarBooking.Domain.Models;

public record CarBooking(string Id, string FirstName, string LastName, DateTime StartDate, 
    DateTime EndDate, string PickUpLocation, decimal Price, Size Size, Transmission Transmission);
