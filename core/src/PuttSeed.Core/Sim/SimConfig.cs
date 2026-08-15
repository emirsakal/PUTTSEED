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

        /// <summary>Per-tick damping while the ball center is inside sand (much stronger).</summary>
        public Fix64 SandDamping { get; }

        /// <summary>Wall bounce restitution (normal component scale, &lt; 1).</summary>
        public Fix64 WallRestitution { get; }

        /// <summary>
        /// Max distance the ball may travel in one sub-step, as guard against
        /// tunneling: a tick is split so each sub-step moves at most this far.
        /// </summary>
        public Fix64 MaxTravelPerSubStep { get; }

        /// <summary>Bumper bounce restitution (&gt; 1: bumpers add energy).</summary>
        public Fix64 BumperRestitution { get; }

        /// <summary>Speed cap applied right after a bumper bounce.</summary>
        public Fix64 BumperMaxExitSpeed { get; }

        /// <summary>Squared speed below which a tick counts toward rest.</summary>
        public Fix64 RestSpeedEpsSq { get; }

        /// <summary>Consecutive slow ticks required before the ball is at rest.</summary>
        public int RestTicksRequired { get; }

        private SimConfig(
            Fix64 dt,
            Fix64 ballRadius,
            Fix64 maxShotSpeed,
            Fix64 rollDamping,
            Fix64 sandDamping,
            Fix64 wallRestitution,
            Fix64 maxTravelPerSubStep,
            Fix64 bumperRestitution,
            Fix64 bumperMaxExitSpeed,
            Fix64 restSpeedEpsSq,
            int restTicksRequired)
        {
            Dt = dt;
            BallRadius = ballRadius;
            MaxShotSpeed = maxShotSpeed;
            RollDamping = rollDamping;
            SandDamping = sandDamping;
            WallRestitution = wallRestitution;
            MaxTravelPerSubStep = maxTravelPerSubStep;
            BumperRestitution = bumperRestitution;
            BumperMaxExitSpeed = bumperMaxExitSpeed;
            RestSpeedEpsSq = restSpeedEpsSq;
            RestTicksRequired = restTicksRequired;
        }

        /// <summary>The tuned default configuration.</summary>
        public static SimConfig Default { get; } = new SimConfig(
            dt: Fix64.FromFraction(1, 120),
            ballRadius: Fix64.FromFraction(1, 10),
            maxShotSpeed: Fix64.FromInt(8),
            rollDamping: Fix64.FromFraction(988, 1000),
            sandDamping: Fix64.FromFraction(94, 100),
            wallRestitution: Fix64.FromFraction(8, 10),
            maxTravelPerSubStep: Fix64.FromFraction(1, 20), // half the ball radius
            bumperRestitution: Fix64.FromFraction(12, 10),
            bumperMaxExitSpeed: Fix64.FromInt(8),
            restSpeedEpsSq: Fix64.FromFraction(1, 2500),    // speed < 0.02 u/s
            restTicksRequired: 6);
    }
}
