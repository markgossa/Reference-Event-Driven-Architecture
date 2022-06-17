using Contracts.Messages.Enums;
 
namespace Contracts.Messages;

public record CarBookingEventData(string PickUpLocation, Size Size, Transmission Transmission);
