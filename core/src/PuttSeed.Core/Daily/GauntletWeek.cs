namespace PuttSeed.Core.Daily
{
    /// <summary>
    /// The weekly gauntlet: seven consecutive daily courses played as one
    /// round, cumulative strokes, one score. It needs no new content — the
    /// seven holes are dailies that already existed, which is the whole point.
    ///
    /// A gauntlet is identified by its week index, so every player everywhere
    /// runs the same seven holes, and only fully elapsed weeks are playable:
    /// a week still in progress would let one player face courses another has
    /// not reached.
    /// </summary>
    public static class GauntletWeek
    {
        /// <summary>Holes in a gauntlet.</summary>
        public const int Length = 7;

        /// <summary>The week a day number belongs to.</summary>
        public static int WeekOf(int dayNumber) => dayNumber / Length;

        /// <summary>The first day number of a week.</summary>
        public static int FirstDay(int weekIndex) => weekIndex * Length;

        /// <summary>
        /// The newest week whose seven days have all happened, or -1 before
        /// any week is complete.
        /// </summary>
        public static int LatestCompleteWeek(int todayDayNumber)
            => WeekOf(todayDayNumber) - 1;

        /// <summary>True when a week index is a real, fully elapsed week.</summary>
        public static bool IsPlayable(int weekIndex, int todayDayNumber)
            => weekIndex >= 0 && weekIndex <= LatestCompleteWeek(todayDayNumber);

        /// <summary>The day number of a hole (0-based) in a week.</summary>
        public static int DayOfHole(int weekIndex, int hole) => FirstDay(weekIndex) + hole;

        /// <summary>The course seed for a hole (0-based) in a week.</summary>
        public static ulong SeedForHole(int weekIndex, int hole)
            => DailyCalendar.SeedForDay(DayOfHole(weekIndex, hole));
    }
}
