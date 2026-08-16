#nullable enable
namespace PuttSeed.Unity
{
    /// <summary>
    /// Golf vocabulary for the hole-out status line. Presentation only — the
    /// star rule lives in core (<see cref="PuttSeed.Core.Sim.Scoring"/>).
    /// </summary>
    public static class GolfTerms
    {
        /// <summary>The status line for a holed run.</summary>
        public static string SuccessLine(int strokes, int par)
        {
            if (strokes == 1)
            {
                return "Ace!";
            }

            int diff = strokes - par;
            return diff <= -2 ? "Eagle!"
                 : diff == -1 ? "Birdie!"
                 : diff == 0 ? "Par — well played!"
                 : diff == 1 ? "Bogey — holed!"
                 : "Holed!";
        }
    }
}
