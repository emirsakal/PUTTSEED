using System.Collections.Generic;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.CourseGen
{
    /// <summary>
    /// Places hazards inside a corridor under the GDD clearance rules: bumpers
    /// hug the centerline leaving a ball-wide passage, sand and water are side
    /// bands that never span the full corridor width, and nothing sits on the
    /// first segment (start pad) — water also avoids the hole segment.
    /// </summary>
    public static class CourseDecorator
    {
        private static readonly Fix64 BumperRadius = Fix64.FromFraction(3, 10);
        private static readonly Fix64 BumperMaxLateral = Fix64.FromFraction(35, 100);
        private static readonly Fix64 BumperMinSpacing = Fix64.FromFraction(8, 10);
        private static readonly Fix64 SandBandWidth = Fix64.FromFraction(12, 10);
        private static readonly Fix64 WaterBandWidth = Fix64.One;
        private const int PlacementTries = 8;

        /// <summary>
        /// Draws decoration from the RNG stream. Max counts are parameters (not
        /// read from config) so the generator can relax decoration on retries.
        /// </summary>
        public static void Decorate(
            FixRng rng,
            Corridor corridor,
            GeneratorConfig cfg,
            int maxBumpers,
            int maxSand,
            int maxWater,
            out Bumper[] bumpers,
            out ZonePolygon[] sand,
            out ZonePolygon[] water)
        {
            int segments = corridor.SegmentAngles.Length;

            int bumperCount = rng.NextInt(0, maxBumpers + 1);
            int sandCount = rng.NextInt(0, maxSand + 1);
            int waterCount = rng.NextInt(0, maxWater + 1);

            var placedBumpers = new List<Bumper>(bumperCount);
            for (int i = 0; i < bumperCount; i++)
            {
                for (int t = 0; t < PlacementTries; t++)
                {
                    // Segments 1..n-1: never on the start pad segment.
                    if (segments < 2)
                    {
                        break;
                    }

                    int seg = rng.NextInt(1, segments);
                    var along = Fix64.FromFraction(rng.NextInt(25, 76), 100);
                    var lateral = Fix64.FromFraction(rng.NextInt(-35, 36), 100);
                    var center = PointInSegmentFrame(corridor, seg, along, lateral);

                    if (!IsClearOfBumpers(placedBumpers, center))
                    {
                        continue;
                    }

                    placedBumpers.Add(new Bumper(center, BumperRadius));
                    break;
                }
            }

            var placedSand = new List<ZonePolygon>(sandCount);
            for (int i = 0; i < sandCount && segments >= 2; i++)
            {
                int seg = rng.NextInt(1, segments);
                placedSand.Add(SideBand(corridor, rng, seg, SandBandWidth));
            }

            var placedWater = new List<ZonePolygon>(waterCount);
            // Water: never on the start segment nor the hole (last) segment.
            for (int i = 0; i < waterCount && segments >= 3; i++)
            {
                int seg = rng.NextInt(1, segments - 1);
                placedWater.Add(SideBand(corridor, rng, seg, WaterBandWidth));
            }

            bumpers = placedBumpers.ToArray();
            sand = placedSand.ToArray();
            water = placedWater.ToArray();
        }

        private static bool IsClearOfBumpers(List<Bumper> placed, Vec2Fix candidate)
        {
            var minSq = BumperMinSpacing * BumperMinSpacing;
            for (int i = 0; i < placed.Count; i++)
            {
                if ((placed[i].Center - candidate).LengthSq() < minSq)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Maps (segment, along 0..1, lateral) to a world position.</summary>
        private static Vec2Fix PointInSegmentFrame(Corridor corridor, int seg, Fix64 along, Fix64 lateral)
        {
            var a = corridor.Centerline[seg];
            var b = corridor.Centerline[seg + 1];
            var dir = FixTrig.UnitVector(corridor.SegmentAngles[seg]);
            var normal = FixTrig.UnitVector(corridor.SegmentAngles[seg] + FixTrig.AngleSteps / 4);
            var length = (b - a).Length();
            return a + dir * (length * along) + normal * lateral;
        }

        /// <summary>
        /// A rectangular band hugging one wall of the given segment, spanning
        /// <paramref name="bandWidth"/> inward — the rest of the width stays
        /// open, so the corridor is never fully blocked.
        /// </summary>
        private static ZonePolygon SideBand(Corridor corridor, FixRng rng, int seg, Fix64 bandWidth)
        {
            var a = corridor.Centerline[seg];
            var b = corridor.Centerline[seg + 1];
            var dir = FixTrig.UnitVector(corridor.SegmentAngles[seg]);
            var normal = FixTrig.UnitVector(corridor.SegmentAngles[seg] + FixTrig.AngleSteps / 4);
            var segLength = (b - a).Length();

            int side = (rng.NextUInt() & 1) == 0 ? 1 : -1;
            var farLat = corridor.HalfWidth * Fix64.FromInt(side);
            var nearLat = (corridor.HalfWidth - bandWidth) * Fix64.FromInt(side);

            // Along-range: centered chunk of the segment, 40..70% of its length.
            var chunk = Fix64.FromFraction(rng.NextInt(40, 71), 100);
            var t0 = (Fix64.One - chunk) * Fix64.Half;
            var t1 = t0 + chunk;

            var p0 = a + dir * (segLength * t0);
            var p1 = a + dir * (segLength * t1);
            return new ZonePolygon(new[]
            {
                p0 + normal * nearLat,
                p1 + normal * nearLat,
                p1 + normal * farLat,
                p0 + normal * farLat,
            });
        }
    }
}
