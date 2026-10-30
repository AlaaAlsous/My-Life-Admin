using My_Life_Admin.Services;

namespace My_Life_Admin.Tests.Services;

public class EventDatesTests
{
    [Fact]
    public void FromExpiry_DateOnly_IsStoredAtNoonUtc()
    {
        var result = EventDates.FromExpiry(new DateTime(2026, 12, 1));

        Assert.Equal(new DateTime(2026, 12, 1, 12, 0, 0), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void FromExpiry_UnspecifiedWithTime_IsTreatedAsUtc()
    {
        var result = EventDates.FromExpiry(new DateTime(2026, 12, 1, 9, 30, 0));

        Assert.Equal(new DateTime(2026, 12, 1, 9, 30, 0), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void FromExpiry_Utc_IsKept()
    {
        var utc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(utc, EventDates.FromExpiry(utc));
    }

    [Fact]
    public void FromExpiry_Local_IsConvertedToUtc()
    {
        var local = new DateTime(2026, 12, 1, 9, 0, 0, DateTimeKind.Local);

        var result = EventDates.FromExpiry(local);

        Assert.Equal(local.ToUniversalTime(), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void AsUtc_MarksValueAsUtcWithoutShiftingIt()
    {
        var result = EventDates.AsUtc(new DateTime(2026, 12, 1, 12, 0, 0, DateTimeKind.Unspecified));

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(12, result.Hour);
    }
}
