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
        /// First day number generated with <see cref="GeneratorConfig.V2"/>
        /// (2026-08-27 UTC): the first daily of the gates/ramps/portals/
        /// windmills wave. Committed before release, so no shipped daily ever
        /// changes retroactively.
        /// </summary>
        public const int V2FromDay = 2430;

        /// <summary>Generator config version for a day number.</summary>
        public static int VersionForDay(int dayNumber)
            => dayNumber >= V2FromDay ? 2 : 1;
    }
}
