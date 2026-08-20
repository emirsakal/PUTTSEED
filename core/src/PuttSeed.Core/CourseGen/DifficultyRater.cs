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
        /// Score a hole is forgiven for each stroke of par beyond the second.
        /// Measured, not guessed: over a 220-seed scan the median par 3 scores
        /// exactly 2 above the median par 2 (21.9 mean against 20.2). The first
        /// guess here was 8, and the data said otherwise.
        /// </summary>
        public const int ParAllowance = 2;

        /// <summary>
        /// Rates a course. <paramref name="captureShots"/> /
        /// <paramref name="sampledShots"/> is the solver's tightness ratio;
        /// <paramref name="turnCount"/> is corridor joints;
        /// <paramref name="hazardCount"/> is bumpers + sand + ice + water.
        /// </summary>
        /// <summary>
        /// Rates a course against holes of ITS OWN SIZE.
        /// <paramref name="captureShots"/> / <paramref name="sampledShots"/> is
        /// the solver's tightness ratio; <paramref name="turnCount"/> is
        /// corridor joints; <paramref name="hazardCount"/> is every element on
        /// the course; <paramref name="par"/> is what the hole is worth.
        /// </summary>
        public static Difficulty Rate(int captureShots, int sampledShots, int turnCount,
            int hazardCount, int par = 2)
        {
            // A longer hole is not a harder hole. Turns and hazards both grow
            // with the corridor, so a par 3 scores systematically above a par
            // 2: under fixed cuts a 200-seed par-2-and-3 scan rated 19/29/52
            // Easy/Normal/Hard, and "Easy" had quietly come to mean "short" —
            // which would have made choosing Easy in practice silently choose
            // par 2, taking the variety back out of the mode that shows it off.
            int score = Score(captureShots, sampledShots, turnCount, hazardCount)
                - ParAllowance * (par - 2);

            // Recalibrated three times, each against a scan: 2026-08-16 when
            // ice joined the hazard pool, 2026-08-18 for the element wave, and
            // 2026-08-19 when corridors grew long enough to hold a par 3 — that
            // last one lifted the whole score distribution by about two points
            // (mean 20.2 for par 2), which under the old 16/20 cuts rated a
            // 200-seed scan 19/29/52 Easy/Normal/Hard. At 18/22 with the par
            // allowance the same scan rates 35/38/25 overall AND per par:
            // 35/39/25 for par 2, 35/37/27 for par 3.
            if (score <= 18)
            {
                return Difficulty.Easy;
            }

            return score <= 22 ? Difficulty.Normal : Difficulty.Hard;
        }

        /// <summary>
        /// The raw difficulty score, before the par allowance. Exposed because
        /// the thresholds above are tuned from scans of it, and a constant
        /// tuned from data nobody can reproduce is a constant nobody can trust.
        /// </summary>
        public static int Score(int captureShots, int sampledShots, int turnCount, int hazardCount)
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

            return tightness + turnCount + 2 * hazardCount;
        }
    }
}
