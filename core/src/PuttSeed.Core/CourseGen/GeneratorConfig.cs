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

        /// <summary>Generation attempts per relaxation level before decorations are reduced.</summary>
        public int AttemptsPerLevel { get; }

        private GeneratorConfig(
            int minSegments, int maxSegments,
            Fix64 minSegmentLength, Fix64 maxSegmentLength,
            int minTurnSteps, int maxTurnSteps,
            Fix64 halfWidth, Vec2Fix boundsMin, Vec2Fix boundsMax,
            int maxBumpers, int maxSand, int maxWater, int attemptsPerLevel)
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
            AttemptsPerLevel = attemptsPerLevel;
        }

        /// <summary>The tuned default configuration.</summary>
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
            attemptsPerLevel: 12);
    }
}
