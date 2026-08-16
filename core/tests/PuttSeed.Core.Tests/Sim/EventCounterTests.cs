using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    /// <summary>
    /// The presentation-facing event counters: deterministic observations for
    /// audio/haptics that deliberately stay OUT of the state hash (they add no
    /// dynamics information — position/velocity already capture the outcome).
    /// </summary>
    [TestFixture]
    public class EventCounterTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        [Test]
        public void WallBounce_IncrementsWallHitCount()
        {
            var course = new CourseData(Vec2Fix.Zero, V(50, 50), par: 2,
                walls: new[] { new WallSegment(V(2, -2), V(2, 2)) });
            var sim = new GolfSim(course, SimConfig.Default);
            Assert.That(sim.WallHitCount, Is.EqualTo(0));
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 240; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.WallHitCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(sim.BumperHitCount, Is.EqualTo(0));
        }

        [Test]
        public void BumperBounce_IncrementsBumperHitCount()
        {
            var course = new CourseData(Vec2Fix.Zero, V(50, 50), par: 2,
                walls: System.Array.Empty<WallSegment>(),
                bumpers: new[] { new Bumper(V(3, 0), Fix64.FromFraction(3, 10)) });
            var sim = new GolfSim(course, SimConfig.Default);
            sim.Shoot(new ShotInput(0, 200));
            for (int i = 0; i < 240; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.BumperHitCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(sim.WallHitCount, Is.EqualTo(0));
        }

        [Test]
        public void WaterEntry_IncrementsWaterEntryCount()
        {
            var course = new CourseData(Vec2Fix.Zero, V(50, 50), par: 2,
                walls: System.Array.Empty<WallSegment>(),
                waterZones: new[] { new ZonePolygon(new[] { V(3, -2), V(5, -2), V(5, 2), V(3, 2) }) });
            var sim = new GolfSim(course, SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 1200 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.WaterEntryCount, Is.EqualTo(1));
        }

        [Test]
        public void QuietRoll_IncrementsNothing()
        {
            var course = new CourseData(Vec2Fix.Zero, V(50, 50), par: 2,
                walls: System.Array.Empty<WallSegment>());
            var sim = new GolfSim(course, SimConfig.Default);
            sim.Shoot(new ShotInput(0, 100));
            for (int i = 0; i < 2400; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.WallHitCount, Is.EqualTo(0));
            Assert.That(sim.BumperHitCount, Is.EqualTo(0));
            Assert.That(sim.WaterEntryCount, Is.EqualTo(0));
        }

        [Test]
        public void Counters_DoNotAffectStateHash()
        {
            // Two sims whose dynamics agree but whose counter histories differ
            // is impossible by determinism; instead we pin the contract the
            // cheap way: the 10k golden hash test (unchanged goldens) plus this
            // direct check that a bounce-heavy and a bounce-free sim with the
            // same final kinematics-relevant fields hash equal via RestoreRest.
            var course = new CourseData(Vec2Fix.Zero, V(50, 50), par: 2,
                walls: new[] { new WallSegment(V(2, -2), V(2, 2)) });
            var bouncy = new GolfSim(course, SimConfig.Default);
            bouncy.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 2400 && !bouncy.IsAtRest; i++)
            {
                bouncy.Tick();
            }

            Assert.That(bouncy.WallHitCount, Is.GreaterThan(0));

            // RestoreRest both sims to the same state: every hashed field now
            // agrees, but only `bouncy` carries a bounce history in counters.
            bouncy.RestoreRest(bouncy.Ball.Position, bouncy.Strokes);
            var clean = new GolfSim(course, SimConfig.Default);
            clean.RestoreRest(bouncy.Ball.Position, bouncy.Strokes);
            while (clean.TickCount < bouncy.TickCount)
            {
                clean.Tick();
            }

            Assert.That(clean.WallHitCount, Is.EqualTo(0), "restored sim has no bounce history");
            Assert.That(clean.StateHash(), Is.EqualTo(bouncy.StateHash()),
                "counters must not leak into the state hash");
        }

        [Test]
        public void Counters_AreDeterministic()
        {
            var course = new CourseData(Vec2Fix.Zero, V(50, 50), par: 3,
                walls: new[]
                {
                    new WallSegment(V(2, -2), V(2, 2)),
                    new WallSegment(V(-2, -2), V(-2, 2)),
                },
                bumpers: new[] { new Bumper(V(0, 1), Fix64.FromFraction(3, 10)) });
            var a = new GolfSim(course, SimConfig.Default);
            var b = new GolfSim(course, SimConfig.Default);
            a.Shoot(new ShotInput(100, 255));
            b.Shoot(new ShotInput(100, 255));
            for (int i = 0; i < 2400; i++)
            {
                a.Tick();
                b.Tick();
            }

            Assert.That(a.WallHitCount, Is.EqualTo(b.WallHitCount));
            Assert.That(a.BumperHitCount, Is.EqualTo(b.BumperHitCount));
        }
    }
}
