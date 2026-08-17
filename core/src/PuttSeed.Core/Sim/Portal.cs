using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// A directional teleport: when the ball center enters the disc at
    /// <see cref="Entry"/> it reappears just outside <see cref="Exit"/> along
    /// its velocity direction, velocity untouched. Pairs are two portals
    /// mirroring each other; the exit offset (radius + ball radius) lands the
    /// ball outside the twin's trigger disc, so a single pass can never
    /// ping-pong. Added 2026-08-18 with the post-MVP element wave.
    /// </summary>
    public readonly struct Portal
    {
        /// <summary>Trigger disc center.</summary>
        public Vec2Fix Entry { get; }

        /// <summary>Where the ball reappears.</summary>
        public Vec2Fix Exit { get; }

        /// <summary>Trigger disc radius.</summary>
        public Fix64 Radius { get; }

        /// <summary>Creates a portal.</summary>
        public Portal(Vec2Fix entry, Vec2Fix exit, Fix64 radius)
        {
            Entry = entry;
            Exit = exit;
            Radius = radius;
        }
    }
}
