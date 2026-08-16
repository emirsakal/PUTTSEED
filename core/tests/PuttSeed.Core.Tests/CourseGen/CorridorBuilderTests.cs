using NUnit.Framework;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.CourseGen
{
    [TestFixture]
    public class CorridorBuilderTests
    {
        private static Corridor BuildOrFail(ulong seed)
        {
            var ok = CorridorBuilder.TryBuild(new FixRng(seed), GeneratorConfig.Default, out var corridor);
            Assert.That(ok, Is.True, $"corridor build failed for seed {seed}");
            return corridor;
        }

        [Test]
        public void SegmentCount_IsWithinConfiguredRange()
        {
            var cfg = GeneratorConfig.Default;
            for (ulong seed = 1; seed <= 30; seed++)
            {
                if (!CorridorBuilder.TryBuild(new FixRng(seed), cfg, out var corridor))
                {
                    continue;
                }

                int segments = corridor.Centerline.Length - 1;
                Assert.That(segments, Is.InRange(cfg.MinSegments, cfg.MaxSegments), $"seed {seed}");
            }
        }

        [Test]
        public void AllVertices_StayInsideBounds()
        {
            var cfg = GeneratorConfig.Default;
            for (ulong seed = 1; seed <= 30; seed++)
            {
                if (!CorridorBuilder.TryBuild(new FixRng(seed), cfg, out var corridor))
                {
                    continue;
                }

                foreach (var v in corridor.Centerline)
                {
                    Assert.That(v.X >= cfg.BoundsMin.X && v.X <= cfg.BoundsMax.X, Is.True, $"seed {seed}: {v}");
                    Assert.That(v.Y >= cfg.BoundsMin.Y && v.Y <= cfg.BoundsMax.Y, Is.True, $"seed {seed}: {v}");
                }
            }
        }

        [Test]
        public void SegmentLengths_AreWithinConfiguredRange()
        {
            var cfg = GeneratorConfig.Default;
            var corridor = BuildOrFail(3);
            for (int i = 1; i < corridor.Centerline.Length; i++)
            {
                var len = (corridor.Centerline[i] - corridor.Centerline[i - 1]).Length();
                // Allow one raw ulp of sqrt tolerance on each side.
                Assert.That(len >= cfg.MinSegmentLength - Fix64.Epsilon, Is.True, $"segment {i} too short: {len}");
                Assert.That(len <= cfg.MaxSegmentLength + Fix64.Epsilon, Is.True, $"segment {i} too long: {len}");
            }
        }

        [Test]
        public void SameSeed_SameCorridor()
        {
            var a = BuildOrFail(42);
            var b = BuildOrFail(42);
            Assert.That(a.Centerline.Length, Is.EqualTo(b.Centerline.Length));
            for (int i = 0; i < a.Centerline.Length; i++)
            {
                Assert.That(a.Centerline[i], Is.EqualTo(b.Centerline[i]));
            }
        }

        [Test]
        public void MostSeeds_ProduceACorridor()
        {
            int ok = 0;
            for (ulong seed = 1; seed <= 100; seed++)
            {
                if (CorridorBuilder.TryBuild(new FixRng(seed), GeneratorConfig.Default, out _))
                {
                    ok++;
                }
            }

            Assert.That(ok, Is.GreaterThanOrEqualTo(80), $"only {ok}/100 seeds built a corridor");
        }

        [Test]
        public void Centerline_NeverSelfIntersects()
        {
            for (ulong seed = 1; seed <= 50; seed++)
            {
                if (!CorridorBuilder.TryBuild(new FixRng(seed), GeneratorConfig.Default, out var corridor))
                {
                    continue;
                }

                var c = corridor.Centerline;
                for (int i = 0; i < c.Length - 1; i++)
                {
                    for (int j = i + 2; j < c.Length - 1; j++)
                    {
                        Assert.That(
                            GeomFix.SegmentsProperlyIntersect(c[i], c[i + 1], c[j], c[j + 1]),
                            Is.False,
                            $"seed {seed}: centerline segments {i} and {j} intersect");
                    }
                }
            }
        }

        [Test]
        public void Walls_FormClosedBoundary_ExpectedCount()
        {
            var corridor = BuildOrFail(5);
            int n = corridor.Centerline.Length - 1;
            var walls = CorridorBuilder.BuildWalls(corridor);
            // Two mitered offset chains of n walls each plus two end caps:
            // joints meet in a single miter point, so no connector stubs.
            Assert.That(walls.Length, Is.EqualTo(2 * n + 2));
        }

        [Test]
        public void WallChains_ShareJointEndpoints_NoKnots()
        {
            // Each side chain must be a continuous polyline: segment i's end is
            // exactly segment i+1's start. The old connector-stub scheme let
            // offsets cross at tight turns and drew knots.
            for (ulong seed = 1; seed <= 30; seed++)
            {
                if (!CorridorBuilder.TryBuild(new FixRng(seed), GeneratorConfig.Default, out var corridor))
                {
                    continue;
                }

                int n = corridor.Centerline.Length - 1;
                var walls = CorridorBuilder.BuildWalls(corridor);
                for (int side = 0; side < 2; side++)
                {
                    int start = side * n;
                    for (int i = 0; i < n - 1; i++)
                    {
                        Assert.That(walls[start + i].B, Is.EqualTo(walls[start + i + 1].A),
                            $"seed {seed}: side {side} chain breaks at joint {i}");
                    }
                }
            }
        }

        [Test]
        public void BallCannotEscapeCorridor()
        {
            // Functional enclosure check: hammer full-power shots in many
            // directions; the ball must stay inside the corridor's bounding box
            // (inflated by the half width).
            var corridor = BuildOrFail(11);
            var walls = CorridorBuilder.BuildWalls(corridor);
            var start = CorridorBuilder.StartPosition(corridor);
            var hole = CorridorBuilder.HolePosition(corridor);

            var min = corridor.Centerline[0];
            var max = corridor.Centerline[0];
            foreach (var v in corridor.Centerline)
            {
                min = new Vec2Fix(Fix64.Min(min.X, v.X), Fix64.Min(min.Y, v.Y));
                max = new Vec2Fix(Fix64.Max(max.X, v.X), Fix64.Max(max.Y, v.Y));
            }

            var margin = corridor.HalfWidth + Fix64.One;
            var course = new CourseData(start, hole, par: 3, walls: walls);
            for (int angle = 0; angle < 1024; angle += 128)
            {
                var sim = new GolfSim(course, SimConfig.Default);
                sim.Shoot(new ShotInput(angle, 255));
                for (int i = 0; i < 900; i++)
                {
                    sim.Tick();
                    var p = sim.Ball.Position;
                    Assert.That(p.X >= min.X - margin && p.X <= max.X + margin, Is.True,
                        $"angle {angle}: escaped in x at tick {i}: {p}");
                    Assert.That(p.Y >= min.Y - margin && p.Y <= max.Y + margin, Is.True,
                        $"angle {angle}: escaped in y at tick {i}: {p}");
                }
            }
        }

        [Test]
        public void StartAndHole_AreInsetFromCorridorEnds()
        {
            var corridor = BuildOrFail(9);
            var start = CorridorBuilder.StartPosition(corridor);
            var hole = CorridorBuilder.HolePosition(corridor);
            var first = corridor.Centerline[0];
            var last = corridor.Centerline[corridor.Centerline.Length - 1];
            Assert.That(start, Is.Not.EqualTo(first), "start must be inset from the cap");
            Assert.That(hole, Is.Not.EqualTo(last), "hole must be inset from the cap");
            Assert.That((start - first).LengthSq() < Fix64.FromInt(2), Is.True);
            Assert.That((hole - last).LengthSq() < Fix64.FromInt(2), Is.True);
        }
    }
}
