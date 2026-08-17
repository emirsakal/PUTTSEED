using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// A valve segment: the ball crosses freely when moving with
    /// <see cref="PassNormal"/> and collides as with a wall when moving
    /// against it. Added 2026-08-18 as the first post-MVP element wave;
    /// courses without gates are bit-identical to before.
    /// </summary>
    public readonly struct OneWayGate
    {
        /// <summary>Segment start point.</summary>
        public Vec2Fix A { get; }

        /// <summary>Segment end point.</summary>
        public Vec2Fix B { get; }

        /// <summary>
        /// Unit normal of the allowed crossing direction. A ball whose velocity
        /// has a positive dot with it ignores the gate entirely; any other ball
        /// treats the segment as a solid wall (a resting ball leans on it).
        /// </summary>
        public Vec2Fix PassNormal { get; }

        /// <summary>Creates a gate; the caller supplies a unit-length normal.</summary>
        public OneWayGate(Vec2Fix a, Vec2Fix b, Vec2Fix passNormal)
        {
            A = a;
            B = b;
            PassNormal = passNormal;
        }
    }
}
