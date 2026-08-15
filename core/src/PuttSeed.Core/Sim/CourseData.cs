using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// Immutable description of one course: geometry and par. Produced by the
    /// generator (Week 2) or hand-authored in tests; consumed by
    /// <see cref="GolfSim"/>.
    /// </summary>
    public sealed class CourseData
    {
        /// <summary>Ball start position.</summary>
        public Vec2Fix StartPosition { get; }

        /// <summary>Hole center position.</summary>
        public Vec2Fix HolePosition { get; }

        /// <summary>Target stroke count.</summary>
        public int Par { get; }

        /// <summary>Wall segments the ball collides with.</summary>
        public WallSegment[] Walls { get; }

        /// <summary>Creates a course. Arrays are stored as-is (caller must not mutate).</summary>
        public CourseData(Vec2Fix startPosition, Vec2Fix holePosition, int par, WallSegment[] walls)
        {
            StartPosition = startPosition;
            HolePosition = holePosition;
            Par = par;
            Walls = walls;
        }
    }
}
