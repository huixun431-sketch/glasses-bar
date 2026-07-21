using System;

namespace GlassesBar.Domain;

public static class MyopiaProgression
{
    public const int CampaignDays = 30;
    public const float InitialDegrees = 50f;
    public const float MaximumDegrees = 400f;

    public static float DegreesForDay(int day)
    {
        var clampedDay = Math.Clamp(day, 1, CampaignDays);
        var degrees = clampedDay <= 21
            ? InitialDegrees + ((clampedDay - 1) / 3) * 25f
            : 200f + (((clampedDay - 22) / 3) + 1) * 50f;
        return Math.Min(degrees, MaximumDegrees);
    }
}
