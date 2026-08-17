namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// Star scoring for a holed run (GDD "Scoring &amp; retention surface"):
    /// 3 = par or better, 2 = one over, 1 = finished within the stroke limit.
    ///
    /// Recalibrated 2026-08-18. The original curve (3 = under par) was written
    /// for courses of varying par, but generation clamps par to at least 2 and
    /// the solver reaches the cup in one or two shots on every layout — a
    /// 3000-seed scan produced par 2 for all 3000. Under par therefore meant
    /// "hole in one", a tier no line even exists for on most courses, while a
    /// single star lumped together three, four and five strokes. Par now
    /// carries the top tier — golf's own standard of good play — and the ace
    /// keeps its own celebration (the hole-out vocabulary and the Ace
    /// achievement) rather than being paid twice.
    /// </summary>
    public static class Scoring
    {
        /// <summary>
        /// Stars for a completed hole. Callers only invoke this once
        /// <see cref="GolfSim.IsHoled"/> is true, so any stroke count beyond
        /// par + 1 that reached the cup within the limit earns one star.
        /// </summary>
        /// <param name="strokes">Strokes taken to hole out.</param>
        /// <param name="par">The course par.</param>
        public static int Stars(int strokes, int par)
        {
            return strokes <= par ? 3 : strokes == par + 1 ? 2 : 1;
        }
    }
}
