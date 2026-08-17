using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// Rotating blades around a pivot. The phase is a pure function of ticks
    /// since the CURRENT shot (re-arming to <see cref="Phase0"/> on every
    /// stroke), which keeps two invariants intact: a rest state fully
    /// determines the future (the solver's BFS stays sound), and replays need
    /// no timing data (the codec stays (seed, shots)). The mill therefore
    /// turns only while the ball rolls — a planning puzzle, not a reflex test.
    /// Added 2026-08-18 with the post-MVP element wave.
    /// </summary>
    public readonly struct Windmill
    {
        /// <summary>Rotation center; every blade grows from here.</summary>
        public Vec2Fix Pivot { get; }

        /// <summary>Blade length, pivot to tip.</summary>
        public Fix64 BladeLength { get; }

        /// <summary>Number of evenly spaced blades (2 = a full diameter bar).</summary>
        public int BladeCount { get; }

        /// <summary>Rotation per tick in 1024-step angle units (sign = direction).</summary>
        public int OmegaSteps { get; }

        /// <summary>Blade base angle at every shot start (1024-step units).</summary>
        public int Phase0 { get; }

        /// <summary>Creates a windmill.</summary>
        public Windmill(Vec2Fix pivot, Fix64 bladeLength, int bladeCount, int omegaSteps, int phase0)
        {
            Pivot = pivot;
            BladeLength = bladeLength;
            BladeCount = bladeCount;
            OmegaSteps = omegaSteps;
            Phase0 = phase0;
        }
    }
}
