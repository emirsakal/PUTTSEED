#nullable enable

namespace PuttSeed.Unity
{
    /// <summary>
    /// Notices the shot that ALMOST went in.
    ///
    /// The lip-out is golf's best moment and this game had no idea it ever
    /// happened: a ball that grazed the cup and rolled on produced exactly the
    /// same silence as one that missed by a metre. Under touch capture — Easy
    /// and Normal — a real rim-out cannot even occur, so the near miss is
    /// entirely a thing of the presentation layer or it does not exist at all.
    ///
    /// It reads positions and nothing else. No rule moves, no determinism is
    /// touched, and a replay of the same shots on another device produces the
    /// same misses because it produces the same positions.
    /// </summary>
    public sealed class NearMissWatch
    {
        /// <summary>How close counts as close: cup radii from the centre.</summary>
        public const float CupRadii = 2f;

        private bool _armed;

        /// <summary>Forgets the current pass (a new run, or a reset ball).</summary>
        public void Reset() => _armed = false;

        /// <summary>
        /// Feeds one observation and returns true exactly once per pass that
        /// misses. It arms while the ball is MOVING inside the ring and fires
        /// when the pass ends — by leaving, or by stopping there, which is the
        /// crueller of the two and the one a player talks about. Holing out
        /// disarms it: a ball in the cup did not miss.
        /// </summary>
        public bool Observe(float distanceToCup, float cupRadius, bool holed, bool moving)
        {
            if (holed)
            {
                _armed = false;
                return false;
            }

            bool near = distanceToCup <= cupRadius * CupRadii;
            if (moving && near)
            {
                _armed = true;
                return false;
            }

            if (!_armed)
            {
                return false;
            }

            _armed = false;
            return true;
        }
    }
}
