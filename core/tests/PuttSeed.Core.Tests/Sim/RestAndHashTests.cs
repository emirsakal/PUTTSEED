using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class RestAndHashTests
    {
        private static CourseData EmptyCourse() => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: new Vec2Fix(Fix64.FromInt(100), Fix64.FromInt(100)),
            par: 2,
            walls: System.Array.Empty<WallSegment>());

        [Test]
        public void NewSim_StartsAtRest()
        {
            var sim = new GolfSim(EmptyCourse(), SimConfig.Default);
            Assert.That(sim.IsAtRest, Is.True);
        }

        [Test]
        public void AfterShot_BallIsNotAtRest()
        {
            var sim = new GolfSim(EmptyCourse(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            Assert.That(sim.IsAtRest, Is.False);
            sim.Tick();
            Assert.That(sim.IsAtRest, Is.False);
        }

        [Test]
        public void BallComesToRest_VelocityZeroedExactly()
        {
            var sim = new GolfSim(EmptyCourse(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 10)); // gentle shot
            for (int i = 0; i < 12000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsAtRest, Is.True, "ball never came to rest");
            Assert.That(sim.Ball.Velocity, Is.EqualTo(Vec2Fix.Zero), "rest velocity must be exactly zero");
        }

        [Test]
        public void AtRest_PositionStopsChanging()
        {
            var sim = new GolfSim(EmptyCourse(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 10));
            for (int i = 0; i < 12000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            var restPos = sim.Ball.Position;
            for (int i = 0; i < 120; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.Ball.Position, Is.EqualTo(restPos));
            Assert.That(sim.IsAtRest, Is.True);
        }

        [Test]
        public void Shoot_WhileMoving_IsIgnored()
        {
            var sim = new GolfSim(EmptyCourse(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 10; i++)
            {
                sim.Tick();
            }

            var vBefore = sim.Ball.Velocity;
            sim.Shoot(new ShotInput(512, 255)); // must be a no-op mid-flight
            Assert.That(sim.Ball.Velocity, Is.EqualTo(vBefore));
            Assert.That(sim.Strokes, Is.EqualTo(1));
        }

        [Test]
        public void Shoot_AfterRest_IsAccepted()
        {
            var sim = new GolfSim(EmptyCourse(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 10));
            for (int i = 0; i < 12000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            sim.Shoot(new ShotInput(512, 20));
            Assert.That(sim.Strokes, Is.EqualTo(2));
            Assert.That(sim.IsAtRest, Is.False);
            Assert.That(sim.Ball.Velocity.X < Fix64.Zero, Is.True);
        }

        [Test]
        public void StateHash_IsEqual_ForIdenticalRuns()
        {
            var a = new GolfSim(EmptyCourse(), SimConfig.Default);
            var b = new GolfSim(EmptyCourse(), SimConfig.Default);
            Assert.That(a.StateHash(), Is.EqualTo(b.StateHash()));

            a.Shoot(new ShotInput(300, 180));
            b.Shoot(new ShotInput(300, 180));
            for (int i = 0; i < 600; i++)
            {
                a.Tick();
                b.Tick();
                Assert.That(a.StateHash(), Is.EqualTo(b.StateHash()), $"hash diverged at tick {i}");
            }
        }

        [Test]
        public void StateHash_Changes_WhenStateChanges()
        {
            var sim = new GolfSim(EmptyCourse(), SimConfig.Default);
            var h0 = sim.StateHash();
            sim.Shoot(new ShotInput(0, 255));
            var h1 = sim.StateHash();
            Assert.That(h1, Is.Not.EqualTo(h0), "hash must reflect the shot");
            sim.Tick();
            Assert.That(sim.StateHash(), Is.Not.EqualTo(h1), "hash must reflect a tick");
        }

        [Test]
        public void StateHash_DiffersBetweenDifferentShots()
        {
            var a = new GolfSim(EmptyCourse(), SimConfig.Default);
            var b = new GolfSim(EmptyCourse(), SimConfig.Default);
            a.Shoot(new ShotInput(100, 200));
            b.Shoot(new ShotInput(101, 200));
            for (int i = 0; i < 60; i++)
            {
                a.Tick();
                b.Tick();
            }

            Assert.That(a.StateHash(), Is.Not.EqualTo(b.StateHash()));
        }
    }
}
