namespace PuttSeed.Unity
{
    /// <summary>
    /// The Journey campaign: 100 curated fixed seeds, identical for every
    /// player, unlocked in order. Levels are just seeds — the generator's
    /// solvability proof and the replay codec apply unchanged.
    ///
    /// L1-50 were curated from a 1500-seed scan (CourseViewer --scan) on a
    /// difficulty ramp: L1-10 gentle Easy courses (the first five hazardless),
    /// L11-25 Normal sampled across the band, L26-40 the hardest Normals
    /// interleaved with entry Hards, L41-50 the meanest Hards (7-8 hazards,
    /// all four element types in play).
    ///
    /// L51-100 came from a wider 3000-seed scan, restarting the ramp above
    /// the midpoint after the L41-50 gauntlet: L51-60 hard Normals as a
    /// breather, L61-75 entry Hards, L76-90 solid Hards (6-7 hazards),
    /// L91-100 the meanest of the pool (7-8 hazards, every element type).
    ///
    /// Existing entries must NEVER be reordered or replaced: player progress
    /// (journeyStars) is stored by level index. Append only.
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
            2533UL, 2535UL, 945UL, 1107UL, 2615UL, 2093UL, 1894UL, 2159UL, 537UL, 1364UL,
            226UL, 2286UL, 2748UL, 307UL, 1000UL, 1597UL, 2147UL, 2904UL, 921UL, 2266UL,
            967UL, 2761UL, 2667UL, 956UL, 860UL, 693UL, 610UL, 1263UL, 2088UL, 2992UL,
            975UL, 2274UL, 461UL, 2738UL, 1403UL, 2811UL, 1536UL, 1190UL, 2924UL, 1596UL,
            482UL, 1947UL, 1862UL, 2531UL, 2201UL, 1734UL, 100UL, 143UL, 331UL, 707UL,
        };
    }
}
