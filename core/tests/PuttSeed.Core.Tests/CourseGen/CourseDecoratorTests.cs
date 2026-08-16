using NUnit.Framework;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.CourseGen
{
    [TestFixture]
    public class CourseDecoratorTests
    {
        /// <summary>False when the corridor build legitimately re-rolls (skip seed).</summary>
        private static bool BuildDecorated(
            ulong seed,
            out Corridor corridor,
            out Bumper[] bumpers,
            out ZonePolygon[] sand,
            out ZonePolygon[] water)
        {
            var cfg = GeneratorConfig.Default;
            var rng = new FixRng(seed);
            if (!CorridorBuilder.TryBuild(rng, cfg, out corridor))
            {
                bumpers = System.Array.Empty<Bumper>();
                sand = System.Array.Empty<ZonePolygon>();
                water = System.Array.Empty<ZonePolygon>();
                return false;
            }

            CourseDecorator.Decorate(rng, corridor, cfg, cfg.MaxBumpers, cfg.MaxSand, cfg.MaxWater,
                out bumpers, out sand, out water);
            return true;
        }

        /// <summary>Distance from a point to the nearest centerline segment.</summary>
        private static Fix64 DistanceToCenterline(Corridor corridor, Vec2Fix p)
        {
            var best = Fix64.MaxValue;
            var c = corridor.Centerline;
            for (int i = 0; i < c.Length - 1; i++)
            {
                var a = c[i];
                var ab = c[i + 1] - a;
                var t = Fix64.Clamp(Vec2Fix.Dot(p - a, ab) / ab.LengthSq(), Fix64.Zero, Fix64.One);
                var d = (p - (a + ab * t)).Length();
                best = Fix64.Min(best, d);
            }

            return best;
        }

        [Test]
        public void Counts_AreWithinGddLimits()
        {
            for (ulong seed = 1; seed <= 40; seed++)
            {
                if (!BuildDecorated(seed, out _, out var bumpers, out var sand, out var water))
                {
                    continue;
                }

                Assert.That(bumpers.Length, Is.InRange(0, 3), $"seed {seed}");
                Assert.That(sand.Length, Is.InRange(0, 2), $"seed {seed}");
                Assert.That(water.Length, Is.InRange(0, 1), $"seed {seed}");
            }
        }

        [Test]
        public void Bumpers_StayNearCenterline_LeavePassage()
        {
            // Lateral placement is capped so that halfWidth - |lat| - bumperR
            // leaves at least a ball-diameter passage on one side.
            for (ulong seed = 1; seed <= 40; seed++)
            {
                if (!BuildDecorated(seed, out var corridor, out var bumpers, out _, out _))
                {
                    continue;
                }

                foreach (var b in bumpers)
                {
                    var lat = DistanceToCenterline(corridor, b.Center);
                    Assert.That(lat <= Fix64.FromFraction(36, 100), Is.True,
                        $"seed {seed}: bumper lateral {lat} too far off center");
                }
            }
        }

        [Test]
        public void Bumpers_DoNotOverlapEachOther()
        {
            for (ulong seed = 1; seed <= 40; seed++)
            {
                if (!BuildDecorated(seed, out _, out var bumpers, out _, out _))
                {
                    continue;
                }

                for (int i = 0; i < bumpers.Length; i++)
                {
                    for (int j = i + 1; j < bumpers.Length; j++)
                    {
                        var dist = (bumpers[i].Center - bumpers[j].Center).Length();
                        var minDist = bumpers[i].Radius + bumpers[j].Radius;
                        Assert.That(dist >= minDist, Is.True,
                            $"seed {seed}: bumpers {i} and {j} overlap");
                    }
                }
            }
        }

        [Test]
        public void ZoneVertices_StayInsideCorridorBand()
        {
            for (ulong seed = 1; seed <= 40; seed++)
            {
                if (!BuildDecorated(seed, out var corridor, out _, out var sand, out var water))
                {
                    continue;
                }

                var limit = corridor.HalfWidth + Fix64.FromFraction(1, 10);
                foreach (var zone in sand)
                {
                    foreach (var v in zone.Vertices)
                    {
                        Assert.That(DistanceToCenterline(corridor, v) <= limit, Is.True,
                            $"seed {seed}: sand vertex {v} outside corridor");
                    }
                }

                foreach (var zone in water)
                {
                    foreach (var v in zone.Vertices)
                    {
                        Assert.That(DistanceToCenterline(corridor, v) <= limit, Is.True,
                            $"seed {seed}: water vertex {v} outside corridor");
                    }
                }
            }
        }

        [Test]
        public void Water_AlwaysLeavesAPassage()
        {
            // At the water zone's own along-position there must be a contiguous
            // lateral band of at least ~0.5 units free of water.
            for (ulong seed = 1; seed <= 60; seed++)
            {
                if (!BuildDecorated(seed, out var corridor, out _, out _, out var water))
                {
                    continue;
                }

                foreach (var zone in water)
                {
                    // Zone centroid -> nearest segment gives the local frame.
                    var centroid = Vec2Fix.Zero;
                    foreach (var v in zone.Vertices)
                    {
                        centroid += v;
                    }

                    centroid = centroid / Fix64.FromInt(zone.Vertices.Length);

                    int bestSeg = 0;
                    var bestD = Fix64.MaxValue;
                    var c = corridor.Centerline;
                    for (int i = 0; i < c.Length - 1; i++)
                    {
                        var a = c[i];
                        var ab = c[i + 1] - a;
                        var t = Fix64.Clamp(Vec2Fix.Dot(centroid - a, ab) / ab.LengthSq(), Fix64.Zero, Fix64.One);
                        var d = (centroid - (a + ab * t)).Length();
                        if (d < bestD)
                        {
                            bestD = d;
                            bestSeg = i;
                        }
                    }

                    var seg = c[bestSeg + 1] - c[bestSeg];
                    var segLenSq = seg.LengthSq();
                    var tC = Fix64.Clamp(Vec2Fix.Dot(centroid - c[bestSeg], seg) / segLenSq, Fix64.Zero, Fix64.One);
                    var onLine = c[bestSeg] + seg * tC;
                    var normal = seg.Perp() / seg.Length();

                    int freeRun = 0, maxFreeRun = 0;
                    for (int step = -9; step <= 9; step++)
                    {
                        var probe = onLine + normal * Fix64.FromFraction(step, 10);
                        if (zone.Contains(probe))
                        {
                            freeRun = 0;
                        }
                        else
                        {
                            freeRun++;
                            if (freeRun > maxFreeRun)
                            {
                                maxFreeRun = freeRun;
                            }
                        }
                    }

                    Assert.That(maxFreeRun, Is.GreaterThanOrEqualTo(5),
                        $"seed {seed}: water blocks the corridor (free run {maxFreeRun})");
                }
            }
        }

        [Test]
        public void Decorations_KeepClearOfStartAndHole()
        {
            for (ulong seed = 1; seed <= 40; seed++)
            {
                if (!BuildDecorated(seed, out var corridor, out var bumpers, out var sand, out var water))
                {
                    continue;
                }

                var start = CorridorBuilder.StartPosition(corridor);
                var hole = CorridorBuilder.HolePosition(corridor);
                var clear = Fix64.One;
                foreach (var b in bumpers)
                {
                    Assert.That((b.Center - start).Length() >= clear, Is.True, $"seed {seed}: bumper near start");
                    Assert.That((b.Center - hole).Length() >= clear, Is.True, $"seed {seed}: bumper near hole");
                }

                foreach (var zone in water)
                {
                    Assert.That(zone.Contains(start), Is.False, $"seed {seed}: water on start");
                    Assert.That(zone.Contains(hole), Is.False, $"seed {seed}: water on hole");
                }
            }
        }

        [Test]
        public void SameSeed_SameDecorations()
        {
            Assert.That(BuildDecorated(42, out _, out var b1, out var s1, out var w1), Is.True);
            Assert.That(BuildDecorated(42, out _, out var b2, out var s2, out var w2), Is.True);
            Assert.That(b1.Length, Is.EqualTo(b2.Length));
            for (int i = 0; i < b1.Length; i++)
            {
                Assert.That(b1[i].Center, Is.EqualTo(b2[i].Center));
                Assert.That(b1[i].Radius, Is.EqualTo(b2[i].Radius));
            }

            Assert.That(s1.Length, Is.EqualTo(s2.Length));
            Assert.That(w1.Length, Is.EqualTo(w2.Length));
        }
    }
}
