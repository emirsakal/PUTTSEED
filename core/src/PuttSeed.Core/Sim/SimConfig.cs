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

        /// <summary>
        /// A constant acceleration applied everywhere on the course (units/s²).
        /// Zero on an ordinary day; a windy day (see
        /// <see cref="Daily.DailyMutators"/>) gives it a direction. Adding a
        /// zero vector is exact in fixed point, so a plain day is bit-identical
        /// to a build that never had wind.
        /// </summary>
        public Vec2Fix Wind { get; }

        /// <summary>Per-tick damping while the ball center is inside sand (much stronger).</summary>
        public Fix64 SandDamping { get; }

        /// <summary>Per-tick damping on ice (barely below 1: the ball slides on).</summary>
        public Fix64 IceDamping { get; }

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

        /// <summary>Hole cup radius; the ball drops when its center is inside.</summary>
        public Fix64 HoleRadius { get; }

        /// <summary>Squared speed limit for capture; faster overlap rims out.</summary>
        public Fix64 HoleCaptureSpeedSq { get; }

        /// <summary>Restitution of a rim-out bounce (&lt; 1: the rim eats energy).</summary>
        public Fix64 RimRestitution { get; }

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
            Fix64 iceDamping,
            Fix64 wallRestitution,
            Fix64 maxTravelPerSubStep,
            Fix64 bumperRestitution,
            Fix64 bumperMaxExitSpeed,
            Fix64 holeRadius,
            Fix64 holeCaptureSpeedSq,
            Fix64 rimRestitution,
            Fix64 restSpeedEpsSq,
            int restTicksRequired,
            Vec2Fix wind = default)
        {
            Wind = wind;
            Dt = dt;
            BallRadius = ballRadius;
            MaxShotSpeed = maxShotSpeed;
            RollDamping = rollDamping;
            SandDamping = sandDamping;
            IceDamping = iceDamping;
            WallRestitution = wallRestitution;
            MaxTravelPerSubStep = maxTravelPerSubStep;
            BumperRestitution = bumperRestitution;
            BumperMaxExitSpeed = bumperMaxExitSpeed;
            HoleRadius = holeRadius;
            HoleCaptureSpeedSq = holeCaptureSpeedSq;
            RimRestitution = rimRestitution;
            RestSpeedEpsSq = restSpeedEpsSq;
            RestTicksRequired = restTicksRequired;
        }

        /// <summary>
        /// Builds a custom configuration. Callers (e.g. the Unity feel-tuning
        /// asset) must quantize their values to <see cref="Fix64"/> BEFORE this
        /// boundary; two devices given the same raw values simulate identically.
        /// </summary>
        public static SimConfig Create(
            Fix64 dt,
            Fix64 ballRadius,
            Fix64 maxShotSpeed,
            Fix64 rollDamping,
            Fix64 sandDamping,
            Fix64 iceDamping,
            Fix64 wallRestitution,
            Fix64 maxTravelPerSubStep,
            Fix64 bumperRestitution,
            Fix64 bumperMaxExitSpeed,
            Fix64 holeRadius,
            Fix64 holeCaptureSpeedSq,
            Fix64 rimRestitution,
            Fix64 restSpeedEpsSq,
            int restTicksRequired)
            => new SimConfig(dt, ballRadius, maxShotSpeed, rollDamping, sandDamping, iceDamping,
                wallRestitution, maxTravelPerSubStep, bumperRestitution, bumperMaxExitSpeed,
                holeRadius, holeCaptureSpeedSq, rimRestitution, restSpeedEpsSq, restTicksRequired,
                Vec2Fix.Zero);

        /// <summary>This config with a different rolling friction.</summary>
        public SimConfig WithRollDamping(Fix64 rollDamping)
            => new SimConfig(Dt, BallRadius, MaxShotSpeed, rollDamping, SandDamping, IceDamping,
                WallRestitution, MaxTravelPerSubStep, BumperRestitution, BumperMaxExitSpeed,
                HoleRadius, HoleCaptureSpeedSq, RimRestitution, RestSpeedEpsSq, RestTicksRequired,
                Wind);

        /// <summary>This config with a different bumper kick.</summary>
        public SimConfig WithBumperRestitution(Fix64 bumperRestitution)
            => new SimConfig(Dt, BallRadius, MaxShotSpeed, RollDamping, SandDamping, IceDamping,
                WallRestitution, MaxTravelPerSubStep, bumperRestitution, BumperMaxExitSpeed,
                HoleRadius, HoleCaptureSpeedSq, RimRestitution, RestSpeedEpsSq, RestTicksRequired,
                Wind);

        /// <summary>This config with a different wind.</summary>
        public SimConfig WithWind(Vec2Fix wind)
            => new SimConfig(Dt, BallRadius, MaxShotSpeed, RollDamping, SandDamping, IceDamping,
                WallRestitution, MaxTravelPerSubStep, BumperRestitution, BumperMaxExitSpeed,
                HoleRadius, HoleCaptureSpeedSq, RimRestitution, RestSpeedEpsSq, RestTicksRequired,
                wind);

        /// <summary>The tuned default configuration.</summary>
        public static SimConfig Default { get; } = new SimConfig(
            dt: Fix64.FromFraction(1, 120),
            ballRadius: Fix64.FromFraction(1, 10),
            maxShotSpeed: Fix64.FromInt(8),
            rollDamping: Fix64.FromFraction(988, 1000),
            sandDamping: Fix64.FromFraction(94, 100),
            iceDamping: Fix64.FromFraction(997, 1000),
            wallRestitution: Fix64.FromFraction(8, 10),
            maxTravelPerSubStep: Fix64.FromFraction(1, 20), // half the ball radius
            bumperRestitution: Fix64.FromFraction(12, 10),
            bumperMaxExitSpeed: Fix64.FromInt(8),
            holeRadius: Fix64.FromFraction(15, 100),
            holeCaptureSpeedSq: Fix64.FromFraction(36, 25), // capture below 1.2 u/s
            rimRestitution: Fix64.FromFraction(4, 10),
            restSpeedEpsSq: Fix64.FromFraction(1, 2500),    // speed < 0.02 u/s
            restTicksRequired: 6);
    }
}
