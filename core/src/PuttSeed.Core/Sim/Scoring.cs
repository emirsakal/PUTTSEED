namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// Star scoring for a holed run (GDD "Scoring &amp; retention surface"):
    /// 3 = under par, 2 = par, 1 = finished within the stroke limit.
    /// </summary>
    public static class Scoring
    {
        /// <summary>
        /// Stars for a completed hole. Callers only invoke this once
        /// <see cref="GolfSim.IsHoled"/> is true, so any stroke count at or
        /// over par that reached the cup within the limit earns one star.
        /// </summary>
        /// <param name="strokes">Strokes taken to hole out.</param>
        /// <param name="par">The course par.</param>
        public static int Stars(int strokes, int par)
        {
            return strokes < par ? 3 : strokes == par ? 2 : 1;
        }
    }
}
