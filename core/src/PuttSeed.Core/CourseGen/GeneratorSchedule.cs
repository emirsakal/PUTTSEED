namespace PuttSeed.Core.CourseGen
{
    /// <summary>
    /// Maps a daily day number (days since 2020-01-01 UTC, the epoch the Unity
    /// layer's streak arithmetic already uses) to the generator config version
    /// that day regenerates with. This is how the daily archive stays stable
    /// while the generator evolves: days before a cutover keep the config they
    /// shipped with, forever. Pure integer math — the epoch-to-date conversion
    /// lives outside core.
    /// </summary>
    public static class GeneratorSchedule
    {
        /// <summary>
        /// First day number generated with <see cref="GeneratorConfig.V4"/> —
        /// the first generator whose holes can be worth three strokes.
        ///
        /// Zero, which is to say all of them. The point of this class is that
        /// a day never changes once it has been played, and the v2 cutover at
        /// day 2430 was set with exactly that in mind — but nothing has
        /// shipped, nobody has a streak, and no archive day has ever been
        /// answered. The whole calendar can move to the newest generator
        /// exactly once, and this is that once. The NEXT change will need a
        /// real cutover here, and this constant is where it goes.
        /// </summary>
        public const int V4FromDay = 0;

        /// <summary>Generator config version for a day number.</summary>
        public static int VersionForDay(int dayNumber)
            => dayNumber >= V4FromDay ? 4 : 1;
    }
}
