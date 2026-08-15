using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// Circular bumper: bounces the ball with restitution &gt; 1 (speed boost)
    /// up to a capped exit speed.
    /// </summary>
    public readonly struct Bumper
    {
        /// <summary>Bumper center.</summary>
        public Vec2Fix Center { get; }

        /// <summary>Bumper radius.</summary>
        public Fix64 Radius { get; }

        /// <summary>Creates a bumper.</summary>
        public Bumper(Vec2Fix center, Fix64 radius)
        {
            Center = center;
            Radius = radius;
        }
    }
}
