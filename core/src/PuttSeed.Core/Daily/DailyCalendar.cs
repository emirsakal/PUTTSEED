namespace PuttSeed.Core.Daily
{
    /// <summary>
    /// Day numbers to calendar dates, in pure integer math. Core may not touch
    /// <c>DateTime</c> — it is banned by the purity grep along with floats —
    /// so the civil-from-days conversion is spelled out here. Day 0 is
    /// 2020-01-01 UTC, the epoch the streak arithmetic already counts from.
    /// </summary>
    public static class DailyCalendar
    {
        /// <summary>Days from the Unix epoch (1970-01-01) to day number 0.</summary>
        private const int UnixDaysToEpoch = 18262;

        /// <summary>
        /// The UTC calendar date of a day number. Hinnant's civil_from_days:
        /// shift the year to start in March so the leap day lands last, then
        /// walk eras of 400 years, which is the exact cycle of the Gregorian
        /// calendar.
        /// </summary>
        public static void ToDate(int dayNumber, out int year, out int month, out int day)
        {
            int z = dayNumber + UnixDaysToEpoch + 719468;
            int era = (z >= 0 ? z : z - 146096) / 146097;
            int doe = z - era * 146097;                                     // [0, 146096]
            int yoe = (doe - doe / 1460 + doe / 36524 - doe / 146096) / 365; // [0, 399]
            int y = yoe + era * 400;
            int doy = doe - (365 * yoe + yoe / 4 - yoe / 100);              // [0, 365]
            int mp = (5 * doy + 2) / 153;                                   // [0, 11]

            day = doy - (153 * mp + 2) / 5 + 1;                             // [1, 31]
            month = mp + (mp < 10 ? 3 : -9);                                // [1, 12]
            year = y + (month <= 2 ? 1 : 0);
        }

        /// <summary>The daily course seed for a day number.</summary>
        public static ulong SeedForDay(int dayNumber)
        {
            ToDate(dayNumber, out int year, out int month, out int day);
            return DailySeed.FromUtcDate(year, month, day);
        }
    }
}
