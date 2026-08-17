namespace PuttSeed.Core.CourseGen
{
    /// <summary>Player-facing difficulty bucket of a generated course.</summary>
    public enum Difficulty
    {
        /// <summary>Forgiving line, few hazards.</summary>
        Easy = 0,

        /// <summary>Some hazards or a winding corridor.</summary>
        Normal = 1,

        /// <summary>Tight author line, many turns and hazards.</summary>
        Hard = 2,
    }

    /// <summary>
    /// Scores a course from author-solution tightness (how few sampled shots
    /// capture from the penultimate state), corridor turn count and hazard
    /// count (ARCHITECTURE.md). Integer math only; deterministic.
    /// </summary>
    public static class DifficultyRater
    {
        /// <summary>
        /// Rates a course. <paramref name="captureShots"/> /
        /// <paramref name="sampledShots"/> is the solver's tightness ratio;
        /// <paramref name="turnCount"/> is corridor joints;
        /// <paramref name="hazardCount"/> is bumpers + sand + ice + water.
        /// </summary>
        public static Difficulty Rate(int captureShots, int sampledShots, int turnCount, int hazardCount)
        {
            // Tightness points without division by zero: compare ratios via
            // cross-multiplication. >= 1/16 of shots capture: forgiving (0).
            // >= 1/64: some precision needed (2). Below: knife's edge (4).
            int tightness;
            if (captureShots * 16 >= sampledShots)
            {
                tightness = 0;
            }
            else if (captureShots * 64 >= sampledShots)
            {
                tightness = 2;
            }
            else
            {
                tightness = 4;
            }

            // Thresholds recalibrated 2026-08-16 after ice zones joined the
            // hazard pool (average score rose by ~8): a 300-seed scan under the
            // old cuts rated 1/39/260 Easy/Normal/Hard. These cuts target
            // roughly even thirds so every practice bucket actually generates.
            //
            // Recalibrated again 2026-08-18 for the v2 element wave (gates,
            // ramps, portals, windmills each score like a hazard): under the
            // 12/16 cuts a 300-seed v2 scan rated 29/75/196; at 16/20 the same
            // scan rates 104/106/90 — near-even thirds. v1 courses rate easier
            // under the new cuts, which only affects scan CSVs: practice (the
            // one difficulty-facing mode) generates v2 exclusively.
            int score = tightness + turnCount + 2 * hazardCount;
            if (score <= 16)
            {
                return Difficulty.Easy;
            }

            return score <= 20 ? Difficulty.Normal : Difficulty.Hard;
        }
    }
}
