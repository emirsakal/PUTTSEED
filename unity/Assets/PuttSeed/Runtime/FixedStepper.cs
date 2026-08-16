namespace PuttSeed.Unity
{
    /// <summary>
    /// Fixed-timestep accumulator: converts variable frame deltas into a number
    /// of 120 Hz sim ticks plus an interpolation alpha. Plain class (no Unity
    /// types) so the stepping logic is EditMode-testable. The sim is NEVER
    /// stepped by deltaTime directly — only whole ticks come out of this.
    /// </summary>
    public sealed class FixedStepper
    {
        /// <summary>Render-side tick length; must mirror the core's fixed dt.</summary>
        public const double TickSeconds = 1.0 / 120.0;

        private readonly int _maxCatchUpTicks;
        private double _accumulator;

        /// <summary>Creates a stepper. Catch-up is capped to avoid a death spiral after hitches.</summary>
        public FixedStepper(int maxCatchUpTicks = 12)
        {
            _maxCatchUpTicks = maxCatchUpTicks;
        }

        /// <summary>
        /// Feeds a frame delta; returns how many whole ticks to advance now.
        /// Backlog beyond the catch-up cap is dropped (time slows rather than
        /// spiraling), keeping the remainder for interpolation.
        /// </summary>
        public int Advance(double deltaSeconds)
        {
            if (deltaSeconds > 0)
            {
                _accumulator += deltaSeconds;
            }

            int ticks = 0;
            while (_accumulator >= TickSeconds && ticks < _maxCatchUpTicks)
            {
                _accumulator -= TickSeconds;
                ticks++;
            }

            if (_accumulator >= TickSeconds)
            {
                _accumulator %= TickSeconds;
            }

            return ticks;
        }

        /// <summary>Interpolation factor in [0,1): fraction of the next tick already elapsed.</summary>
        public float Alpha => (float)(_accumulator / TickSeconds);
    }
}
