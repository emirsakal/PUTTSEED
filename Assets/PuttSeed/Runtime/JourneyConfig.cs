namespace PuttSeed.Unity
{
    /// <summary>
    /// The Journey campaign: 50 curated fixed seeds, identical for every
    /// player, unlocked in order. Levels are just seeds — the generator's
    /// solvability proof and the replay codec apply unchanged.
    ///
    /// Curated from a 1500-seed scan (CourseViewer --scan) on a difficulty
    /// ramp: L1-10 gentle Easy courses (the first five hazardless), L11-25
    /// Normal sampled across the band, L26-40 the hardest Normals interleaved
    /// with entry Hards, L41-50 the meanest Hards (7-8 hazards, all four
    /// element types in play).
    /// </summary>
    public static class JourneyConfig
    {
        /// <summary>The level seeds, in play order (index = level).</summary>
        public static readonly ulong[] Seeds =
        {
            528UL, 544UL, 659UL, 1046UL, 1104UL, 214UL, 1451UL, 126UL, 1028UL, 477UL,
            57UL, 583UL, 655UL, 1383UL, 1140UL, 152UL, 564UL, 931UL, 1219UL, 190UL,
            911UL, 1387UL, 412UL, 1362UL, 1274UL, 135UL, 782UL, 883UL, 1464UL, 688UL,
            1305UL, 106UL, 409UL, 673UL, 830UL, 1119UL, 421UL, 848UL, 587UL, 173UL,
            1417UL, 1422UL, 157UL, 344UL, 590UL, 778UL, 786UL, 961UL, 1351UL, 1479UL,
        };
    }
}
