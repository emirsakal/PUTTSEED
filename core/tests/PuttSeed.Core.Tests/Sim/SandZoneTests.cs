using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class SandZoneTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        /// <summary>Sand strip covering x in [2,4], y in [-2,2] on the shot line.</summary>
        private static CourseData CourseWithSandStrip(bool withSand) => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: V(50, 50),
            par: 2,
            walls: System.Array.Empty<WallSegment>(),
            sandZones: withSand
                ? new[] { new ZonePolygon(new[] { V(2, -2), V(4, -2), V(4, 2), V(2, 2) }) }
                : System.Array.Empty<ZonePolygon>());

        [Test]
        public void SandSlowsBallDown_ComparedToBareGround()
        {
            var bare = new GolfSim(CourseWithSandStrip(false), SimConfig.Default);
            var sandy = new GolfSim(CourseWithSandStrip(true), SimConfig.Default);
            bare.Shoot(new ShotInput(0, 255));
            sandy.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 1200; i++)
            {
                bare.Tick();
                sandy.Tick();
            }

            Assert.That(sandy.Ball.Position.X < bare.Ball.Position.X, Is.True,
                $"sand must shorten the roll (bare {bare.Ball.Position.X}, sandy {sandy.Ball.Position.X})");
        }

        [Test]
        public void BallShotGentlyIntoSand_StopsInside()
        {
            var sim = new GolfSim(CourseWithSandStrip(true), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 191)); // 6 u/s: reaches sand, dies inside
            for (int i = 0; i < 12000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsAtRest, Is.True);
            Assert.That(sim.Ball.Position.X > Fix64.FromInt(2), Is.True, "ball stopped before the sand");
            Assert.That(sim.Ball.Position.X < Fix64.FromInt(4), Is.True, "ball rolled through the sand");
        }

        [Test]
        public void SandOffThePath_HasNoEffect()
        {
            var bare = new GolfSim(CourseWithSandStrip(false), SimConfig.Default);
            var offPath = new CourseData(
                startPosition: Vec2Fix.Zero,
                holePosition: V(50, 50),
                par: 2,
                walls: System.Array.Empty<WallSegment>(),
                sandZones: new[] { new ZonePolygon(new[] { V(2, 5), V(4, 5), V(4, 9), V(2, 9) }) });
            var sandy = new GolfSim(offPath, SimConfig.Default);
            bare.Shoot(new ShotInput(0, 255));
            sandy.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 1200; i++)
            {
                bare.Tick();
                sandy.Tick();
                Assert.That(sandy.StateHash(), Is.EqualTo(bare.StateHash()), $"diverged at tick {i}");
            }
        }
    }
}
