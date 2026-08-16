using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.CourseGen
{
    /// <summary>
    /// The generated corridor skeleton: a centerline polyline plus half width,
    /// with per-segment direction angle indices kept so wall offsets use exact
    /// table lookups instead of runtime normalization.
    /// </summary>
    public readonly struct Corridor
    {
        /// <summary>Centerline vertices (segment count + 1).</summary>
        public Vec2Fix[] Centerline { get; }

        /// <summary>Direction angle index (FixTrig steps) of each segment.</summary>
        public int[] SegmentAngles { get; }

        /// <summary>Half width of the corridor.</summary>
        public Fix64 HalfWidth { get; }

        /// <summary>Creates a corridor (arrays stored as-is).</summary>
        public Corridor(Vec2Fix[] centerline, int[] segmentAngles, Fix64 halfWidth)
        {
            Centerline = centerline;
            SegmentAngles = segmentAngles;
            HalfWidth = halfWidth;
        }
    }
}
