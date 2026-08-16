using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class IceZoneTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        /// <summary>Ice strip covering x in [2,6], y in [-2,2] on the shot line.</summary>
        private static CourseData CourseWithIceStrip(bool withIce) => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: V(50, 50),
            par: 2,
            walls: System.Array.Empty<WallSegment>(),
            iceZones: withIce
                ? new[] { new ZonePolygon(new[] { V(2, -2), V(6, -2), V(6, 2), V(2, 2) }) }
                : System.Array.Empty<ZonePolygon>());

        [Test]
        public void IceCarriesBallFarther_ComparedToBareGround()
        {
            var bare = new GolfSim(CourseWithIceStrip(false), SimConfig.Default);
            var icy = new GolfSim(CourseWithIceStrip(true), SimConfig.Default);
            bare.Shoot(new ShotInput(0, 127));
            icy.Shoot(new ShotInput(0, 127));
            for (int i = 0; i < 2400; i++)
            {
                bare.Tick();
                icy.Tick();
            }

            Assert.That(icy.Ball.Position.X > bare.Ball.Position.X, Is.True,
                $"ice must lengthen the roll (bare {bare.Ball.Position.X}, icy {icy.Ball.Position.X})");
        }

        [Test]
        public void GentleShot_SlidesAcrossTheWholeIceStrip()
        {
            // A shot that would die inside [2,6] on grass slides past it on ice.
            var bare = new GolfSim(CourseWithIceStrip(false), SimConfig.Default);
            bare.Shoot(new ShotInput(0, 159)); // 5 u/s: rolls ~3.4 on grass
            for (int i = 0; i < 20000 && !bare.IsAtRest; i++)
            {
                bare.Tick();
            }

            Assert.That(bare.Ball.Position.X > Fix64.FromInt(2), Is.True);
            Assert.That(bare.Ball.Position.X < Fix64.FromInt(6), Is.True,
                "precondition: the grass shot must stop inside the strip area");

            var icy = new GolfSim(CourseWithIceStrip(true), SimConfig.Default);
            icy.Shoot(new ShotInput(0, 159));
            for (int i = 0; i < 20000 && !icy.IsAtRest; i++)
            {
                icy.Tick();
            }

            Assert.That(icy.Ball.Position.X > Fix64.FromInt(6), Is.True,
                $"same shot must slide beyond the ice strip (stopped at {icy.Ball.Position.X})");
        }

        [Test]
        public void Sand_WinsOverIce_WhenZonesOverlap()
        {
            // Same strip as both sand AND ice: sand's damping must apply.
            var strip = new[] { new ZonePolygon(new[] { V(2, -2), V(6, -2), V(6, 2), V(2, 2) }) };
            var overlapped = new GolfSim(new CourseData(
                Vec2Fix.Zero, V(50, 50), 2, System.Array.Empty<WallSegment>(),
                sandZones: strip, iceZones: strip), SimConfig.Default);
            var sandOnly = new GolfSim(new CourseData(
                Vec2Fix.Zero, V(50, 50), 2, System.Array.Empty<WallSegment>(),
                sandZones: strip), SimConfig.Default);

            overlapped.Shoot(new ShotInput(0, 200));
            sandOnly.Shoot(new ShotInput(0, 200));
            for (int i = 0; i < 2400; i++)
            {
                overlapped.Tick();
                sandOnly.Tick();
                Assert.That(overlapped.StateHash(), Is.EqualTo(sandOnly.StateHash()),
                    $"sand+ice must behave exactly like sand at tick {i}");
            }
        }

        [Test]
        public void IceOffThePath_HasNoEffect()
        {
            var bare = new GolfSim(CourseWithIceStrip(false), SimConfig.Default);
            var offPath = new GolfSim(new CourseData(
                Vec2Fix.Zero, V(50, 50), 2, System.Array.Empty<WallSegment>(),
                iceZones: new[] { new ZonePolygon(new[] { V(2, 5), V(6, 5), V(6, 9), V(2, 9) }) }),
                SimConfig.Default);
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
        public void IceHandling_IsDeterministic()
        {
            var a = new GolfSim(CourseWithIceStrip(true), SimConfig.Default);
            var b = new GolfSim(CourseWithIceStrip(true), SimConfig.Default);
            a.Shoot(new ShotInput(30, 255));
            b.Shoot(new ShotInput(30, 255));
            for (int i = 0; i < 2400; i++)
            {
                a.Tick();
                b.Tick();
                Assert.That(a.StateHash(), Is.EqualTo(b.StateHash()), $"hash diverged at tick {i}");
            }
        }
    }
}
