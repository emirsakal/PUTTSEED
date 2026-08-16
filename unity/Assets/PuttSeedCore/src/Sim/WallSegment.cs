using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// Static wall: a line segment the ball (a circle) collides with.
    /// </summary>
    public readonly struct WallSegment
    {
        /// <summary>Segment start point.</summary>
        public Vec2Fix A { get; }

        /// <summary>Segment end point.</summary>
        public Vec2Fix B { get; }

        /// <summary>Creates a wall segment between two points.</summary>
        public WallSegment(Vec2Fix a, Vec2Fix b)
        {
            A = a;
            B = b;
        }
    }
}
