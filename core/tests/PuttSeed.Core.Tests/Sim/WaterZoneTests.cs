using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class WaterZoneTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        /// <summary>Water covering x in [3,5], y in [-2,2] on the shot line.</summary>
        private static CourseData CourseWithWater() => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: V(50, 50),
            par: 2,
            walls: System.Array.Empty<WallSegment>(),
            waterZones: new[] { new ZonePolygon(new[] { V(3, -2), V(5, -2), V(5, 2), V(3, 2) }) });

        [Test]
        public void BallEnteringWater_ReturnsToLastRestPosition_WithPenalty()
        {
            var sim = new GolfSim(CourseWithWater(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255)); // full power straight into the water
            for (int i = 0; i < 1200 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsAtRest, Is.True);
            Assert.That(sim.Ball.Position, Is.EqualTo(Vec2Fix.Zero), "ball must return to the pre-shot rest position");
            Assert.That(sim.Strokes, Is.EqualTo(2), "shot (1) + water penalty (1)");
            Assert.That(sim.Ball.Velocity, Is.EqualTo(Vec2Fix.Zero));
        }

        [Test]
        public void LastRestPosition_TracksLatestRest_NotStart()
        {
            var sim = new GolfSim(CourseWithWater(), SimConfig.Default);

            // First: a gentle shot that stops before the water.
            sim.Shoot(new ShotInput(0, 100));
            for (int i = 0; i < 12000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            var restPos = sim.Ball.Position;
            Assert.That(restPos.X > Fix64.Zero, Is.True, "first shot should have moved the ball");
            Assert.That(restPos.X < Fix64.FromInt(3), Is.True, "first shot should stop before the water");

            // Second: full power into the water; ball must return to restPos, not start.
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 1200 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.Ball.Position, Is.EqualTo(restPos));
            Assert.That(sim.Strokes, Is.EqualTo(3), "two shots + one penalty");
        }

        [Test]
        public void FastBall_CannotSkipOverWater()
        {
            // Even at max speed the center-in-polygon check must trigger while
            // crossing (sub-stepping keeps per-step travel far below zone size).
            var sim = new GolfSim(CourseWithWater(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 1200; i++)
            {
                sim.Tick();
                Assert.That(sim.Ball.Position.X < Fix64.FromInt(5), Is.True,
                    $"ball got past the water at tick {i}");
            }
        }

        [Test]
        public void ShotAvoidingWater_NoPenalty()
        {
            var sim = new GolfSim(CourseWithWater(), SimConfig.Default);
            sim.Shoot(new ShotInput(256, 255)); // straight up, away from water
            for (int i = 0; i < 12000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.Strokes, Is.EqualTo(1));
            Assert.That(sim.Ball.Position.Y > Fix64.Zero, Is.True);
        }

        [Test]
        public void WaterHandling_IsDeterministic()
        {
            var a = new GolfSim(CourseWithWater(), SimConfig.Default);
            var b = new GolfSim(CourseWithWater(), SimConfig.Default);
            a.Shoot(new ShotInput(10, 255));
            b.Shoot(new ShotInput(10, 255));
            for (int i = 0; i < 1200; i++)
            {
                a.Tick();
                b.Tick();
                Assert.That(a.StateHash(), Is.EqualTo(b.StateHash()), $"hash diverged at tick {i}");
            }
        }
    }
}
