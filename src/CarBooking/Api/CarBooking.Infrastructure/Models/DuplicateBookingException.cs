using System.Runtime.Serialization;

namespace CarBooking.Infrastructure.Models;
public class DuplicateBookingException : Exception
{
    public DuplicateBookingException()
    {
    }

    public DuplicateBookingException(string? message) : base(message)
    {
    }

    public DuplicateBookingException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected DuplicateBookingException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
