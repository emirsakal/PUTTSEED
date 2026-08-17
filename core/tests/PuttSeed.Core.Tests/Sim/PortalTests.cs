using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class PortalTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        private static readonly Fix64 PortalRadius = Fix64.FromFraction(1, 2);

        /// <summary>A twin pair: A at (3,0) exits to B at (10,6), and back.</summary>
        private static CourseData CourseWithTwinPortals() => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: V(50, 50),
            par: 2,
            walls: System.Array.Empty<WallSegment>(),
            portals: new[]
            {
                new Portal(V(3, 0), V(10, 6), PortalRadius),
                new Portal(V(10, 6), V(3, 0), PortalRadius),
            });

        private static CourseData BareCourse() => new CourseData(
            Vec2Fix.Zero, V(50, 50), 2, System.Array.Empty<WallSegment>());

        [Test]
        public void Portal_TeleportsWithoutTouchingVelocity()
        {
            var bare = new GolfSim(BareCourse(), SimConfig.Default);
            var ported = new GolfSim(CourseWithTwinPortals(), SimConfig.Default);
            bare.Shoot(new ShotInput(0, 255));
            ported.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 1200; i++)
            {
                bare.Tick();
                ported.Tick();
                Assert.That(ported.Ball.Velocity.X.Raw, Is.EqualTo(bare.Ball.Velocity.X.Raw),
                    $"teleport must never touch velocity (tick {i})");
                Assert.That(ported.Ball.Velocity.Y.Raw, Is.EqualTo(bare.Ball.Velocity.Y.Raw),
                    $"teleport must never touch velocity (tick {i})");
            }

            Assert.That(ported.PortalTransitCount, Is.EqualTo(1), "the +x shot passes portal A once");
        }

        [Test]
        public void Portal_CarriesTheBallToTheExitNeighborhood()
        {
            var sim = new GolfSim(CourseWithTwinPortals(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 12000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsAtRest, Is.True);
            Assert.That(sim.PortalTransitCount, Is.EqualTo(1), "exactly one transit — no ping-pong");
            Assert.That(sim.Ball.Position.X > Fix64.FromInt(10), Is.True,
                $"ball must continue past the exit (rested at {sim.Ball.Position.X}, {sim.Ball.Position.Y})");
            Assert.That(sim.Ball.Position.Y > Fix64.FromInt(5), Is.True,
                "ball must be on the exit's side of the course");
        }

        [Test]
        public void TwinPortal_AllowsTheReverseTrip()
        {
            var sim = new GolfSim(CourseWithTwinPortals(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 12000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.PortalTransitCount, Is.EqualTo(1), "setup: went through A");

            sim.Shoot(new ShotInput(512, 255)); // straight back in -x toward B
            for (int i = 0; i < 12000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.PortalTransitCount, Is.EqualTo(2),
                "the twin must carry the return trip");
            Assert.That(sim.Ball.Position.X < Fix64.FromInt(3), Is.True,
                $"ball must be back on A's side (rested at {sim.Ball.Position.X})");
        }

        [Test]
        public void PortalOffThePath_HasNoEffect()
        {
            var bare = new GolfSim(BareCourse(), SimConfig.Default);
            var ported = new GolfSim(new CourseData(
                Vec2Fix.Zero, V(50, 50), 2, System.Array.Empty<WallSegment>(),
                portals: new[] { new Portal(V(3, 8), V(10, 8), PortalRadius) }), SimConfig.Default);
            bare.Shoot(new ShotInput(0, 255));
            ported.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 1200; i++)
            {
                bare.Tick();
                ported.Tick();
                Assert.That(ported.StateHash(), Is.EqualTo(bare.StateHash()), $"diverged at tick {i}");
            }
        }

        [Test]
        public void CourseWithoutPortals_KeepsLegacyBehavior()
        {
            Assert.That(BareCourse().Portals, Is.Empty);
        }
    }
}
