using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.CourseGen
{
    /// <summary>
    /// Tuning constants for course generation. Like <see cref="Sim.SimConfig"/>,
    /// this is part of the determinism contract: the same seed and config always
    /// produce the same course.
    /// </summary>
    public sealed class GeneratorConfig
    {
        /// <summary>Minimum corridor segment count.</summary>
        public int MinSegments { get; }

        /// <summary>Maximum corridor segment count.</summary>
        public int MaxSegments { get; }

        /// <summary>Minimum corridor segment length.</summary>
        public Fix64 MinSegmentLength { get; }

        /// <summary>Maximum corridor segment length.</summary>
        public Fix64 MaxSegmentLength { get; }

        /// <summary>Minimum turn between segments, in 1024-step angle units.</summary>
        public int MinTurnSteps { get; }

        /// <summary>Maximum turn between segments, in 1024-step angle units.</summary>
        public int MaxTurnSteps { get; }

        /// <summary>Corridor half width (centerline to wall).</summary>
        public Fix64 HalfWidth { get; }

        /// <summary>Playfield bounding box, minimum corner.</summary>
        public Vec2Fix BoundsMin { get; }

        /// <summary>Playfield bounding box, maximum corner.</summary>
        public Vec2Fix BoundsMax { get; }

        /// <summary>Max bumpers to place (0..3 per GDD).</summary>
        public int MaxBumpers { get; }

        /// <summary>Max sand zones to place (0..2 per GDD).</summary>
        public int MaxSand { get; }

        /// <summary>Max water zones to place (0..1 per GDD).</summary>
        public int MaxWater { get; }

        /// <summary>Max ice zones to place (0..2).</summary>
        public int MaxIce { get; }

        /// <summary>Max one-way gates to place (0 in v1).</summary>
        public int MaxGates { get; }

        /// <summary>Max ramp zones to place (0 in v1).</summary>
        public int MaxRamps { get; }

        /// <summary>Max portal pairs to place (0 in v1).</summary>
        public int MaxPortals { get; }

        /// <summary>Max windmills to place (0 in v1).</summary>
        public int MaxWindmills { get; }

        /// <summary>Generation attempts per relaxation level before decorations are reduced.</summary>
        public int AttemptsPerLevel { get; }

        private GeneratorConfig(
            int minSegments, int maxSegments,
            Fix64 minSegmentLength, Fix64 maxSegmentLength,
            int minTurnSteps, int maxTurnSteps,
            Fix64 halfWidth, Vec2Fix boundsMin, Vec2Fix boundsMax,
            int maxBumpers, int maxSand, int maxWater, int maxIce, int attemptsPerLevel,
            int maxGates = 0, int maxRamps = 0, int maxPortals = 0, int maxWindmills = 0)
        {
            MinSegments = minSegments;
            MaxSegments = maxSegments;
            MinSegmentLength = minSegmentLength;
            MaxSegmentLength = maxSegmentLength;
            MinTurnSteps = minTurnSteps;
            MaxTurnSteps = maxTurnSteps;
            HalfWidth = halfWidth;
            BoundsMin = boundsMin;
            BoundsMax = boundsMax;
            MaxBumpers = maxBumpers;
            MaxSand = maxSand;
            MaxWater = maxWater;
            MaxIce = maxIce;
            AttemptsPerLevel = attemptsPerLevel;
            MaxGates = maxGates;
            MaxRamps = maxRamps;
            MaxPortals = maxPortals;
            MaxWindmills = maxWindmills;
        }

        /// <summary>
        /// The frozen v1 configuration (five elements). Journey levels and
        /// version-1 replay codes regenerate with this FOREVER — never retune
        /// it; new content goes into <see cref="V2"/> and beyond.
        /// </summary>
        public static GeneratorConfig Default { get; } = new GeneratorConfig(
            minSegments: 4,
            maxSegments: 8,
            minSegmentLength: Fix64.FromFraction(5, 4),
            maxSegmentLength: Fix64.FromFraction(5, 2),
            minTurnSteps: 64,   // 22.5 degrees
            maxTurnSteps: 192,  // 67.5 degrees
            halfWidth: Fix64.One,
            boundsMin: new Vec2Fix(Fix64.FromInt(-14), Fix64.FromInt(-14)),
            boundsMax: new Vec2Fix(Fix64.FromInt(14), Fix64.FromInt(14)),
            maxBumpers: 3,
            maxSand: 2,
            maxWater: 1,
            maxIce: 2,
            attemptsPerLevel: 12);

        /// <summary>Alias of <see cref="Default"/>: the frozen v1.</summary>
        public static GeneratorConfig V1 => Default;

        /// <summary>
        /// v2: the 2026-08 element wave (gates, ramps, portals, windmills) on
        /// top of unchanged v1 budgets. Dailies from
        /// <see cref="GeneratorSchedule.V2FromDay"/> and practice use this.
        /// </summary>
        public static GeneratorConfig V2 { get; } = new GeneratorConfig(
            minSegments: 4,
            maxSegments: 8,
            minSegmentLength: Fix64.FromFraction(5, 4),
            maxSegmentLength: Fix64.FromFraction(5, 2),
            minTurnSteps: 64,
            maxTurnSteps: 192,
            halfWidth: Fix64.One,
            boundsMin: new Vec2Fix(Fix64.FromInt(-14), Fix64.FromInt(-14)),
            boundsMax: new Vec2Fix(Fix64.FromInt(14), Fix64.FromInt(14)),
            maxBumpers: 3,
            maxSand: 2,
            maxWater: 1,
            maxIce: 2,
            attemptsPerLevel: 12,
            maxGates: 1,
            maxRamps: 1,
            maxPortals: 1,
            maxWindmills: 1);

        /// <summary>The config a generator version number maps to.</summary>
        /// <exception cref="System.ArgumentException">Unknown version.</exception>
        public static GeneratorConfig ForVersion(int version) => version switch
        {
            1 => V1,
            2 => V2,
            3 => V2, // wire v3: the same courses, shots carry their timing
            _ => throw new System.ArgumentException($"Unknown generator version {version}.", nameof(version)),
        };
    }
}
