using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class OneWayGateTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        /// <summary>
        /// A vertical gate segment at x=3 spanning y in [-2,2], passable only
        /// in +x (PassNormal = +x). Ball starts at origin, hole far away.
        /// </summary>
        private static CourseData CourseWithGate(int passSignX) => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: V(50, 50),
            par: 2,
            walls: System.Array.Empty<WallSegment>(),
            gates: new[]
            {
                new OneWayGate(V(3, -2), V(3, 2),
                    new Vec2Fix(Fix64.FromInt(passSignX), Fix64.Zero)),
            });

        [Test]
        public void BallMovingWithPassNormal_PassesThrough()
        {
            var gated = new GolfSim(CourseWithGate(1), SimConfig.Default);
            var open = new GolfSim(new CourseData(
                Vec2Fix.Zero, V(50, 50), 2, System.Array.Empty<WallSegment>()), SimConfig.Default);

            gated.Shoot(new ShotInput(0, 255)); // +x, full power
            open.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 1200; i++)
            {
                gated.Tick();
                open.Tick();
                Assert.That(gated.StateHash(), Is.EqualTo(open.StateHash()),
                    $"a passable gate must be inert; diverged at tick {i}");
            }

            Assert.That(gated.Ball.Position.X > Fix64.FromInt(3), Is.True,
                "ball must end up beyond the gate line");
        }

        [Test]
        public void BallMovingAgainstPassNormal_BouncesLikeAWall()
        {
            var sim = new GolfSim(CourseWithGate(-1), SimConfig.Default); // pass = -x, ball moves +x
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 2400 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsAtRest, Is.True, "ball must come to rest");
            Assert.That(sim.Ball.Position.X < Fix64.FromInt(3), Is.True,
                "ball must stay on the blocked side of the gate");
            Assert.That(sim.GateHitCount, Is.GreaterThan(0), "the block must count as a gate hit");
        }

        [Test]
        public void GateIsSolidFromTheBlockedSide_EvenAfterAPassage()
        {
            // Pass through in +x, then shoot back in -x: the gate must refuse
            // the return trip — the valve semantics that make it a gate.
            var sim = new GolfSim(CourseWithGate(1), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 2400 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.Ball.Position.X > Fix64.FromInt(3), Is.True, "setup: ball crossed");
            Assert.That(sim.IsHoled, Is.False, "setup: still playing");

            sim.Shoot(new ShotInput(512, 255)); // 180 degrees: straight back in -x
            for (int i = 0; i < 2400 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.Ball.Position.X > Fix64.FromInt(3), Is.True,
                "gate must block the return trip");
            Assert.That(sim.GateHitCount, Is.GreaterThan(0));
        }

        [Test]
        public void GateOffThePath_HasNoEffect()
        {
            var open = new GolfSim(new CourseData(
                Vec2Fix.Zero, V(50, 50), 2, System.Array.Empty<WallSegment>()), SimConfig.Default);
            var gated = new GolfSim(new CourseData(
                Vec2Fix.Zero, V(50, 50), 2, System.Array.Empty<WallSegment>(),
                gates: new[] { new OneWayGate(V(3, 5), V(3, 9), new Vec2Fix(Fix64.One, Fix64.Zero)) }),
                SimConfig.Default);

            open.Shoot(new ShotInput(0, 255));
            gated.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 1200; i++)
            {
                open.Tick();
                gated.Tick();
                Assert.That(gated.StateHash(), Is.EqualTo(open.StateHash()), $"diverged at tick {i}");
            }
        }

        [Test]
        public void CourseWithoutGates_KeepsLegacyBehavior()
        {
            // The gate array defaults to empty and must add zero dynamics —
            // the existing golden hashes depend on it.
            var course = new CourseData(
                Vec2Fix.Zero, V(50, 50), 2, System.Array.Empty<WallSegment>());
            Assert.That(course.Gates, Is.Empty);
        }
    }
}
