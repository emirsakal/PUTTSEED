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
        private static readonly Fix64 EndClearance = Fix64.FromFraction(12, 10);
        private static readonly Fix64 SandBandWidth = Fix64.FromFraction(12, 10);
        private static readonly Fix64 WaterBandWidth = Fix64.One;
        private static readonly Fix64 IceBandWidth = Fix64.FromFraction(14, 10);
        private static readonly Fix64 PortalRadius = Fix64.FromFraction(35, 100);
        private const int PlacementTries = 8;

        /// <summary>
        /// Draws decoration from the RNG stream. Max counts are parameters (not
        /// read from config) so the generator can relax decoration on retries.
        /// The v2-wave draws (gates, ramps, portals, windmills) come last and
        /// each sits behind its budget guard: with a zero budget it consumes
        /// ZERO rng draws, so v1 output is bit-identical by construction.
        /// </summary>
        public static void Decorate(
            FixRng rng,
            Corridor corridor,
            GeneratorConfig cfg,
            int maxBumpers,
            int maxSand,
            int maxWater,
            int maxIce,
            int maxGates,
            int maxRamps,
            int maxPortals,
            int maxWindmills,
            out Bumper[] bumpers,
            out ZonePolygon[] sand,
            out ZonePolygon[] water,
            out ZonePolygon[] ice,
            out OneWayGate[] gates,
            out RampZone[] ramps,
            out Portal[] portals,
            out Windmill[] windmills)
        {
            int segments = corridor.SegmentAngles.Length;
            var start = CorridorBuilder.StartPosition(corridor);
            var hole = CorridorBuilder.HolePosition(corridor);

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

                    // Keep clear of the start pad and — critically — the hole:
                    // a bumper overlapping the cup makes the course unsolvable.
                    if ((center - start).LengthSq() < EndClearance * EndClearance
                        || (center - hole).LengthSq() < EndClearance * EndClearance)
                    {
                        continue;
                    }

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

            // Ice: slippery bands hugging one wall; unlike sand they may sit on
            // the last segment (a slide toward the hole is a fun finish), but
            // never on the start pad segment.
            int iceCount = rng.NextInt(0, maxIce + 1);
            var placedIce = new List<ZonePolygon>(iceCount);
            for (int i = 0; i < iceCount && segments >= 2; i++)
            {
                int seg = rng.NextInt(1, segments);
                placedIce.Add(SideBand(corridor, rng, seg, IceBandWidth));
            }

            // ---- The 2026-08 element wave. Order is frozen forever: gates,
            // ramps, portals, windmills — appending only, never reordering.

            // Gates: a full-width valve across a mid segment, passable toward
            // the hole — cross it and the way back is closed.
            var placedGates = new List<OneWayGate>();
            if (maxGates > 0)
            {
                int gateCount = rng.NextInt(0, maxGates + 1);
                for (int i = 0; i < gateCount && segments >= 2; i++)
                {
                    for (int t = 0; t < PlacementTries; t++)
                    {
                        int seg = rng.NextInt(1, segments);
                        var along = Fix64.FromFraction(rng.NextInt(30, 71), 100);
                        var center = PointInSegmentFrame(corridor, seg, along, Fix64.Zero);
                        if ((center - start).LengthSq() < EndClearance * EndClearance
                            || (center - hole).LengthSq() < EndClearance * EndClearance)
                        {
                            continue;
                        }

                        var dir = FixTrig.UnitVector(corridor.SegmentAngles[seg]);
                        var normal = FixTrig.UnitVector(corridor.SegmentAngles[seg] + FixTrig.AngleSteps / 4);
                        placedGates.Add(new OneWayGate(
                            center + normal * corridor.HalfWidth,
                            center - normal * corridor.HalfWidth,
                            dir));
                        break;
                    }
                }
            }

            // Ramps: a full-width sloped band along the corridor axis, downhill
            // or uphill — full width never blocks, it only bends the speed.
            var placedRamps = new List<RampZone>();
            if (maxRamps > 0)
            {
                int rampCount = rng.NextInt(0, maxRamps + 1);
                for (int i = 0; i < rampCount && segments >= 2; i++)
                {
                    int seg = rng.NextInt(1, segments);
                    var dir = FixTrig.UnitVector(corridor.SegmentAngles[seg]);
                    int sign = (rng.NextUInt() & 1) == 0 ? 1 : -1;
                    int magnitude = rng.NextInt(4, 7);
                    placedRamps.Add(new RampZone(
                        FullBand(corridor, rng, seg),
                        dir * Fix64.FromInt(sign * magnitude)));
                }
            }

            // Portals: a twin pair on the centerline of two distinct segments,
            // both directions — a shortcut the solver prices into par.
            var placedPortals = new List<Portal>();
            if (maxPortals > 0 && segments >= 3)
            {
                int pairCount = rng.NextInt(0, maxPortals + 1);
                for (int i = 0; i < pairCount; i++)
                {
                    for (int t = 0; t < PlacementTries; t++)
                    {
                        int segA = rng.NextInt(1, segments - 1);
                        int segB = rng.NextInt(segA + 1, segments);
                        var pa = PointInSegmentFrame(corridor, segA, Fix64.Half, Fix64.Zero);
                        var pb = PointInSegmentFrame(corridor, segB, Fix64.Half, Fix64.Zero);
                        if ((pa - start).LengthSq() < EndClearance * EndClearance
                            || (pa - hole).LengthSq() < EndClearance * EndClearance
                            || (pb - start).LengthSq() < EndClearance * EndClearance
                            || (pb - hole).LengthSq() < EndClearance * EndClearance
                            || (pa - pb).LengthSq() < EndClearance * EndClearance)
                        {
                            continue;
                        }

                        placedPortals.Add(new Portal(pa, pb, PortalRadius));
                        placedPortals.Add(new Portal(pb, pa, PortalRadius));
                        break;
                    }
                }
            }

            // Windmills: a two-blade bar spinning on the centerline; blades
            // reach 3/4 of the half width, so the corridor edges stay open.
            var placedMills = new List<Windmill>();
            if (maxWindmills > 0)
            {
                int millCount = rng.NextInt(0, maxWindmills + 1);
                var bladeLength = corridor.HalfWidth * Fix64.FromFraction(3, 4);
                var reach = EndClearance + bladeLength;
                for (int i = 0; i < millCount && segments >= 2; i++)
                {
                    for (int t = 0; t < PlacementTries; t++)
                    {
                        int seg = rng.NextInt(1, segments);
                        var along = Fix64.FromFraction(rng.NextInt(35, 66), 100);
                        var pivot = PointInSegmentFrame(corridor, seg, along, Fix64.Zero);
                        if ((pivot - start).LengthSq() < reach * reach
                            || (pivot - hole).LengthSq() < reach * reach)
                        {
                            continue;
                        }

                        int omegaSign = (rng.NextUInt() & 1) == 0 ? 1 : -1;
                        int omegaMagnitude = rng.NextInt(2, 5);
                        int phase0 = rng.NextInt(0, FixTrig.AngleSteps);
                        placedMills.Add(new Windmill(
                            pivot, bladeLength, bladeCount: 2,
                            omegaSteps: omegaSign * omegaMagnitude, phase0: phase0));
                        break;
                    }
                }
            }

            bumpers = placedBumpers.ToArray();
            sand = placedSand.ToArray();
            water = placedWater.ToArray();
            ice = placedIce.ToArray();
            gates = placedGates.ToArray();
            ramps = placedRamps.ToArray();
            portals = placedPortals.ToArray();
            windmills = placedMills.ToArray();
        }

        /// <summary>
        /// A full-width rectangular band across the given segment (30-50% of
        /// its length, centered) — used by ramps, which modify rather than
        /// block, so spanning the whole corridor is safe.
        /// </summary>
        private static ZonePolygon FullBand(Corridor corridor, FixRng rng, int seg)
        {
            var a = corridor.Centerline[seg];
            var b = corridor.Centerline[seg + 1];
            var dir = FixTrig.UnitVector(corridor.SegmentAngles[seg]);
            var normal = FixTrig.UnitVector(corridor.SegmentAngles[seg] + FixTrig.AngleSteps / 4);
            var segLength = (b - a).Length();

            var chunk = Fix64.FromFraction(rng.NextInt(30, 51), 100);
            var t0 = (Fix64.One - chunk) * Fix64.Half;
            var t1 = t0 + chunk;
            var p0 = a + dir * (segLength * t0);
            var p1 = a + dir * (segLength * t1);
            var w = corridor.HalfWidth;
            return new ZonePolygon(new[]
            {
                p0 - normal * w,
                p1 - normal * w,
                p1 + normal * w,
                p0 + normal * w,
            });
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
