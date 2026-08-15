using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// All tuning constants of the simulation, in fixed point. A course played
    /// with the same config, seed and inputs is bit-identical everywhere; the
    /// config is part of the determinism contract (changing it invalidates
    /// golden hashes).
    /// </summary>
    public sealed class SimConfig
    {
        /// <summary>Fixed timestep: 1/120 s.</summary>
        public Fix64 Dt { get; }

        /// <summary>Ball radius in course units.</summary>
        public Fix64 BallRadius { get; }

        /// <summary>Speed of a full-power shot (units/s).</summary>
        public Fix64 MaxShotSpeed { get; }

        /// <summary>Per-tick exponential velocity damping on normal ground (&lt; 1).</summary>
        public Fix64 RollDamping { get; }

        private SimConfig(Fix64 dt, Fix64 ballRadius, Fix64 maxShotSpeed, Fix64 rollDamping)
        {
            Dt = dt;
            BallRadius = ballRadius;
            MaxShotSpeed = maxShotSpeed;
            RollDamping = rollDamping;
        }

        /// <summary>The tuned default configuration.</summary>
        public static SimConfig Default { get; } = new SimConfig(
            dt: Fix64.FromFraction(1, 120),
            ballRadius: Fix64.FromFraction(1, 10),
            maxShotSpeed: Fix64.FromInt(8),
            rollDamping: Fix64.FromFraction(988, 1000));
    }
}
