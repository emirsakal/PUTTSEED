namespace PuttSeed.Unity
{
    /// <summary>
    /// The Journey campaign: 100 curated fixed seeds, identical for every
    /// player, unlocked in order. Levels are just seeds — the generator's
    /// solvability proof and the replay codec apply unchanged.
    ///
    /// Re-curated 2026-08-19 from a 3000-seed v4 scan, the first generator
    /// whose holes can be worth three strokes. The ramp now moves on three
    /// axes at once, and the par axis was the point: par 3 is 20% of the
    /// opening twenty levels and 80% of the last ten, but it is WEIGHTED, not
    /// staged — there is no level where par 2 stops and par 3 begins, and the
    /// two stay interleaved the whole way.
    ///
    ///   L1-3     bare ground, at most one hazard: the game explaining itself
    ///   L1-20    Easy, par 3 one level in five, ~2.4 hazards
    ///   L21-45   Easy and Normal, two in five, ~4.1 hazards
    ///   L46-70   Normal, three in five, ~6.0 hazards
    ///   L71-90   Normal and Hard, seven in ten, ~6.7 hazards
    ///   L91-100  Hard throughout, four in five, ~8.5 hazards
    ///
    /// Seeds are spread across the scanned range rather than taken in order,
    /// so neighbouring levels share nothing but their band — and the scan runs
    /// under the GAME's physics, not core's defaults, because acceptance
    /// depends on solvability and solvability depends on friction: the same
    /// seed grows a different hole under the two.
    ///
    /// Existing entries must NEVER be reordered or replaced once players have
    /// them: progress (journeyStars) is stored by level index. This wholesale
    /// re-curation was possible exactly once — before the game shipped.
    /// </summary>
    public static class JourneyConfig
    {
        /// <summary>The level seeds, in play order (index = level).</summary>
        public static readonly ulong[] Seeds =
        {
            238UL, 1110UL, 2175UL, 9UL, 277UL, 502UL, 721UL, 201UL, 905UL, 1241UL,
            1489UL, 1093UL, 1660UL, 1916UL, 2135UL, 1836UL, 2378UL, 2587UL, 2811UL, 2331UL,
            4UL, 195UL, 10UL, 392UL, 281UL, 594UL, 773UL, 630UL, 1010UL, 931UL,
            1218UL, 1418UL, 1263UL, 1588UL, 1592UL, 1782UL, 1998UL, 1943UL, 2177UL, 2223UL,
            2403UL, 2625UL, 2442UL, 2815UL, 2676UL, 2UL, 11UL, 273UL, 186UL, 382UL,
            564UL, 652UL, 903UL, 785UL, 983UL, 1163UL, 1170UL, 1493UL, 1278UL, 1463UL,
            1821UL, 1636UL, 2090UL, 1885UL, 2138UL, 2361UL, 2371UL, 2684UL, 2638UL, 2828UL,
            3UL, 13UL, 190UL, 504UL, 359UL, 638UL, 992UL, 824UL, 1095UL, 1294UL,
            1439UL, 1520UL, 1709UL, 1957UL, 1933UL, 2152UL, 2470UL, 2370UL, 2615UL, 2798UL,
            1UL, 7UL, 370UL, 757UL, 1277UL, 1404UL, 1686UL, 2002UL, 2384UL, 2746UL,
        };
    }
}
