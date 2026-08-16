using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// Snapshot of the ball's kinematic state. Value struct; the Unity layer
    /// keeps the last two snapshots and interpolates between them for rendering.
    /// </summary>
    public readonly struct BallState
    {
        /// <summary>Ball center position.</summary>
        public Vec2Fix Position { get; }

        /// <summary>Ball velocity in units/s.</summary>
        public Vec2Fix Velocity { get; }

        /// <summary>Creates a snapshot.</summary>
        public BallState(Vec2Fix position, Vec2Fix velocity)
        {
            Position = position;
            Velocity = velocity;
        }
    }
}
