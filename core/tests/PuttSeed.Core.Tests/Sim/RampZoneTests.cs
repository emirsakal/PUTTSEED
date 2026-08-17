using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class RampZoneTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        /// <summary>Ramp covering x in [2,4], y in [-2,2] with the given accel along +x.</summary>
        private static CourseData CourseWithRamp(int accelX) => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: V(50, 50),
            par: 2,
            walls: System.Array.Empty<WallSegment>(),
            ramps: new[]
            {
                new RampZone(
                    new ZonePolygon(new[] { V(2, -2), V(4, -2), V(4, 2), V(2, 2) }),
                    new Vec2Fix(Fix64.FromInt(accelX), Fix64.Zero)),
            });

        private static CourseData BareCourse() => new CourseData(
            Vec2Fix.Zero, V(50, 50), 2, System.Array.Empty<WallSegment>());

        [Test]
        public void DownhillRamp_CarriesTheBallFarther()
        {
            var bare = new GolfSim(BareCourse(), SimConfig.Default);
            var ramped = new GolfSim(CourseWithRamp(6), SimConfig.Default);
            bare.Shoot(new ShotInput(0, 255));
            ramped.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 1200; i++)
            {
                bare.Tick();
                ramped.Tick();
            }

            Assert.That(ramped.Ball.Position.X > bare.Ball.Position.X, Is.True,
                $"downhill must lengthen the roll (bare {bare.Ball.Position.X}, ramped {ramped.Ball.Position.X})");
        }

        [Test]
        public void UphillRamp_RollsAGentleBallBack()
        {
            var sim = new GolfSim(CourseWithRamp(-6), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 191)); // enough to enter, not to climb
            for (int i = 0; i < 12000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsAtRest, Is.True, "ball must come to rest");
            Assert.That(sim.Ball.Position.X < Fix64.FromInt(2), Is.True,
                $"ball must roll back off the uphill ramp (rested at {sim.Ball.Position.X})");
        }

        [Test]
        public void BallNeverRestsInsideARamp()
        {
            // A downhill ramp keeps feeding speed: the ball must exit the far
            // edge and rest beyond it, never inside the zone.
            var sim = new GolfSim(CourseWithRamp(6), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 150)); // gentle: dies inside without the ramp
            for (int i = 0; i < 12000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsAtRest, Is.True, "ball must come to rest");
            Assert.That(sim.Ball.Position.X > Fix64.FromInt(4), Is.True,
                $"ball must be carried past the ramp (rested at {sim.Ball.Position.X})");
        }

        [Test]
        public void RampOffThePath_HasNoEffect()
        {
            var bare = new GolfSim(BareCourse(), SimConfig.Default);
            var offPath = new GolfSim(new CourseData(
                Vec2Fix.Zero, V(50, 50), 2, System.Array.Empty<WallSegment>(),
                ramps: new[]
                {
                    new RampZone(
                        new ZonePolygon(new[] { V(2, 5), V(4, 5), V(4, 9), V(2, 9) }),
                        new Vec2Fix(Fix64.FromInt(6), Fix64.Zero)),
                }), SimConfig.Default);
            bare.Shoot(new ShotInput(0, 255));
            offPath.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 1200; i++)
            {
                bare.Tick();
                offPath.Tick();
                Assert.That(offPath.StateHash(), Is.EqualTo(bare.StateHash()), $"diverged at tick {i}");
            }
        }

        [Test]
        public void CourseWithoutRamps_KeepsLegacyBehavior()
        {
            Assert.That(BareCourse().Ramps, Is.Empty);
        }
    }
}
