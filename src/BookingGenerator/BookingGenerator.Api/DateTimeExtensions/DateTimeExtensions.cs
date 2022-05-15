namespace BookingGenerator.Api.DateTimeExtensions;

public static class DateTimeExtensions
{
    public static DateOnly ToDateOnly(this DateTime dateTime)
        => new(dateTime.Year, dateTime.Month, dateTime.Day);
}
