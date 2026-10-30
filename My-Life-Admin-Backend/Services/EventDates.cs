namespace My_Life_Admin.Services;

public static class EventDates
{
    // A date-only value (midnight, no timezone) is stored at noon UTC so it
    // shows on the same calendar day in any timezone from UTC-12 to UTC+11.
    public static DateTime FromExpiry(DateTime expiry)
    {
        return expiry.Kind switch
        {
            DateTimeKind.Utc => expiry,
            DateTimeKind.Local => expiry.ToUniversalTime(),
            _ when expiry.TimeOfDay == TimeSpan.Zero =>
                DateTime.SpecifyKind(expiry.Date.AddHours(12), DateTimeKind.Utc),
            _ => DateTime.SpecifyKind(expiry, DateTimeKind.Utc)
        };
    }

    // The database stores UTC without a kind; a range bound as Local or Unspecified
    // would otherwise be shifted by the server's timezone.
    public static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
    }

    // The database returns DateTime values without a kind; they are always UTC.
    public static DateTime AsUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
