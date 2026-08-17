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

        /// <summary>Circular bumpers (may be empty).</summary>
        public Bumper[] Bumpers { get; }

        /// <summary>High-friction sand polygons (may be empty).</summary>
        public ZonePolygon[] SandZones { get; }

        /// <summary>Water polygons: entering costs a stroke and resets the ball (may be empty).</summary>
        public ZonePolygon[] WaterZones { get; }

        /// <summary>Low-friction ice polygons: the ball slides much farther (may be empty).</summary>
        public ZonePolygon[] IceZones { get; }

        /// <summary>One-way gate segments: pass one way, wall the other (may be empty).</summary>
        public OneWayGate[] Gates { get; }

        /// <summary>Sloped zones accelerating the ball while inside (may be empty).</summary>
        public RampZone[] Ramps { get; }

        /// <summary>Creates a course. Arrays are stored as-is (caller must not mutate).</summary>
        public CourseData(
            Vec2Fix startPosition,
            Vec2Fix holePosition,
            int par,
            WallSegment[] walls,
            Bumper[]? bumpers = null,
            ZonePolygon[]? sandZones = null,
            ZonePolygon[]? waterZones = null,
            ZonePolygon[]? iceZones = null,
            OneWayGate[]? gates = null,
            RampZone[]? ramps = null)
        {
            StartPosition = startPosition;
            HolePosition = holePosition;
            Par = par;
            Walls = walls;
            Bumpers = bumpers ?? System.Array.Empty<Bumper>();
            SandZones = sandZones ?? System.Array.Empty<ZonePolygon>();
            WaterZones = waterZones ?? System.Array.Empty<ZonePolygon>();
            IceZones = iceZones ?? System.Array.Empty<ZonePolygon>();
            Gates = gates ?? System.Array.Empty<OneWayGate>();
            Ramps = ramps ?? System.Array.Empty<RampZone>();
        }
    }
}
