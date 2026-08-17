using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// A slope: while the ball center is inside <see cref="Area"/> it gains
    /// <see cref="Accel"/> (units/s²) each tick, before friction damping —
    /// downhill lengthens the roll, uphill repels gentle shots. A finite zone
    /// with damping means the ball always exits and rests: no livelock.
    /// Added 2026-08-18 with the post-MVP element wave.
    /// </summary>
    public readonly struct RampZone
    {
        /// <summary>The sloped region.</summary>
        public ZonePolygon Area { get; }

        /// <summary>Downhill acceleration vector (units/s²).</summary>
        public Vec2Fix Accel { get; }

        /// <summary>Creates a ramp.</summary>
        public RampZone(ZonePolygon area, Vec2Fix accel)
        {
            Area = area;
            Accel = accel;
        }
    }
}
