using System.Collections.Generic;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.CourseGen
{
    /// <summary>
    /// Grows the corridor polyline (ARCHITECTURE.md "corridor growth"): from a
    /// start pad, 4–8 segments with turn and length constraints inside the
    /// bounding box, self-avoiding, then widened into wall pairs with joint
    /// connectors and end caps so the playfield is fully enclosed.
    /// </summary>
    public static class CorridorBuilder
    {
        private const int CandidateTriesPerSegment = 8;

        /// <summary>Distance of the start/hole from their corridor end vertices.</summary>
        private static readonly Fix64 EndInset = Fix64.FromFraction(7, 10);

        /// <summary>
        /// Attempts to grow a corridor from the RNG stream. Returns false when a
        /// self-avoiding, in-bounds polyline could not be grown within the
        /// bounded number of candidate tries (caller re-rolls with a new seed).
        /// </summary>
        public static bool TryBuild(FixRng rng, GeneratorConfig cfg, out Corridor corridor)
        {
            corridor = default;
            int segments = rng.NextInt(cfg.MinSegments, cfg.MaxSegments + 1);

            // Safe box for centerline vertices: bounds shrunk by half width + margin.
            var shrink = cfg.HalfWidth + Fix64.Half;
            var safeMin = new Vec2Fix(cfg.BoundsMin.X + shrink, cfg.BoundsMin.Y + shrink);
            var safeMax = new Vec2Fix(cfg.BoundsMax.X - shrink, cfg.BoundsMax.Y - shrink);

            var vertices = new List<Vec2Fix>(segments + 1) { Vec2Fix.Zero };
            var angles = new List<int>(segments);
            int direction = rng.NextInt(0, FixTrig.AngleSteps);

            for (int s = 0; s < segments; s++)
            {
                bool placed = false;
                for (int t = 0; t < CandidateTriesPerSegment && !placed; t++)
                {
                    int candidateDir = direction;
                    if (s > 0)
                    {
                        int turn = rng.NextInt(cfg.MinTurnSteps, cfg.MaxTurnSteps + 1);
                        int sign = (rng.NextUInt() & 1) == 0 ? 1 : -1;
                        candidateDir = (direction + sign * turn) & (FixTrig.AngleSteps - 1);
                    }

                    // Length in quarter-unit steps for a coarse deterministic grid.
                    int minQ = (cfg.MinSegmentLength * Fix64.FromInt(4)).ToInt();
                    int maxQ = (cfg.MaxSegmentLength * Fix64.FromInt(4)).ToInt();
                    var length = Fix64.FromFraction(rng.NextInt(minQ, maxQ + 1), 4);

                    var from = vertices[vertices.Count - 1];
                    var to = from + FixTrig.UnitVector(candidateDir) * length;

                    if (to.X < safeMin.X || to.X > safeMax.X || to.Y < safeMin.Y || to.Y > safeMax.Y)
                    {
                        continue;
                    }

                    if (!IsSelfAvoiding(vertices, from, to, cfg.HalfWidth))
                    {
                        continue;
                    }

                    vertices.Add(to);
                    angles.Add(candidateDir);
                    direction = candidateDir;
                    placed = true;
                }

                if (!placed)
                {
                    return false;
                }
            }

            corridor = new Corridor(vertices.ToArray(), angles.ToArray(), cfg.HalfWidth);
            return true;
        }

        private static bool IsSelfAvoiding(List<Vec2Fix> vertices, Vec2Fix from, Vec2Fix to, Fix64 halfWidth)
        {
            // New endpoint must keep clear distance from all earlier vertices
            // (except the immediate predecessor) so widened walls cannot overlap.
            var clearance = halfWidth * Fix64.FromFraction(5, 2);
            var clearanceSq = clearance * clearance;
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                if ((to - vertices[i]).LengthSq() < clearanceSq)
                {
                    return false;
                }
            }

            // And the new centerline segment must not cross any earlier one
            // (the shared-vertex neighbor is skipped).
            for (int i = 0; i < vertices.Count - 2; i++)
            {
                if (GeomFix.SegmentsProperlyIntersect(vertices[i], vertices[i + 1], from, to))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Widens the centerline into an enclosed wall set: two MITERED offset
        /// chains plus two end caps. At every interior vertex the two offset
        /// lines meet in their exact intersection point (the miter), so each
        /// side is one continuous polyline — no connector stubs, no crossing
        /// offsets, none of the knot artifacts naive offsetting produces at
        /// turns. Turn angles are capped at 67.5°, which bounds the miter
        /// length at ~1.2× the half width.
        /// </summary>
        public static WallSegment[] BuildWalls(Corridor corridor)
        {
            var c = corridor.Centerline;
            var a = corridor.SegmentAngles;
            int n = c.Length - 1;
            var w = corridor.HalfWidth;

            var walls = new WallSegment[2 * n + 2];
            int at = 0;

            for (int side = 0; side < 2; side++)
            {
                // Left chain uses the +90° normal, right chain the -90° normal.
                int quarter = side == 0 ? FixTrig.AngleSteps / 4 : -FixTrig.AngleSteps / 4;

                var prev = c[0] + FixTrig.UnitVector(a[0] + quarter) * w;
                for (int i = 1; i <= n; i++)
                {
                    Vec2Fix point;
                    if (i == n)
                    {
                        point = c[n] + FixTrig.UnitVector(a[n - 1] + quarter) * w;
                    }
                    else
                    {
                        // Miter: intersection of the two neighboring offset
                        // lines = V + w * (n1 + n2) / (1 + n1·n2).
                        var n1 = FixTrig.UnitVector(a[i - 1] + quarter);
                        var n2 = FixTrig.UnitVector(a[i] + quarter);
                        var denom = Fix64.One + Vec2Fix.Dot(n1, n2);
                        point = c[i] + (n1 + n2) * (w / denom);
                    }

                    walls[at++] = new WallSegment(prev, point);
                    prev = point;
                }
            }

            // End caps across the corridor at the first and last vertices.
            var n0 = FixTrig.UnitVector(a[0] + FixTrig.AngleSteps / 4);
            var nn = FixTrig.UnitVector(a[n - 1] + FixTrig.AngleSteps / 4);
            walls[at++] = new WallSegment(c[0] + n0 * w, c[0] - n0 * w);
            walls[at] = new WallSegment(c[n] + nn * w, c[n] - nn * w);

            return walls;
        }

        /// <summary>Ball start: inset along the first segment from the start cap.</summary>
        public static Vec2Fix StartPosition(Corridor corridor)
            => corridor.Centerline[0] + FixTrig.UnitVector(corridor.SegmentAngles[0]) * EndInset;

        /// <summary>Hole center: inset back along the last segment from the end cap.</summary>
        public static Vec2Fix HolePosition(Corridor corridor)
        {
            int last = corridor.SegmentAngles.Length - 1;
            return corridor.Centerline[corridor.Centerline.Length - 1]
                - FixTrig.UnitVector(corridor.SegmentAngles[last]) * EndInset;
        }
    }
}
