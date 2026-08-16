#nullable enable
using System;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Time remaining until the next daily hole (UTC midnight — the moment
    /// <see cref="PuttSeed.Core.Daily.DailySeed"/> derives a new seed).
    /// </summary>
    public static class DailyCountdown
    {
        /// <summary>Span from <paramref name="utcNow"/> to the next UTC midnight.</summary>
        public static TimeSpan UntilNextHole(DateTime utcNow)
            => utcNow.Date.AddDays(1) - utcNow;

        /// <summary>Formats a remaining span as HH:MM:SS (clamped at zero).</summary>
        public static string Format(TimeSpan remaining)
        {
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            return $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }
}
