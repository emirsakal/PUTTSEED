using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class HoleCaptureTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        /// <summary>Open course with the hole 3 units down the +x shot line.</summary>
        private static CourseData CourseWithHoleAt3() => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: V(3, 0),
            par: 1,
            walls: System.Array.Empty<WallSegment>());

        [Test]
        public void SlowBall_IsCaptured()
        {
            var sim = new GolfSim(CourseWithHoleAt3(), SimConfig.Default);
            // 5 u/s: total roll ~3.4 units, arrives at the hole well under the
            // capture speed threshold.
            sim.Shoot(new ShotInput(0, 159));
            for (int i = 0; i < 2400 && !sim.IsHoled; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsHoled, Is.True, "slow ball must drop in");
            Assert.That(sim.Ball.Position, Is.EqualTo(V(3, 0)), "captured ball sits at the hole center");
            Assert.That(sim.Ball.Velocity, Is.EqualTo(Vec2Fix.Zero));
            Assert.That(sim.IsAtRest, Is.True);
        }

        [Test]
        public void FastBall_RimsOut_NotCaptured()
        {
            var sim = new GolfSim(CourseWithHoleAt3(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255)); // crosses the hole at ~3.6 u/s
            bool everHoled = false;
            for (int i = 0; i < 2400; i++)
            {
                sim.Tick();
                everHoled |= sim.IsHoled;
            }

            Assert.That(everHoled, Is.False, "fast ball must rim out, not sink");
        }

        [Test]
        public void RimOut_DeflectsBallAwayFromHole()
        {
            var sim = new GolfSim(CourseWithHoleAt3(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            bool bouncedBack = false;
            for (int i = 0; i < 2400; i++)
            {
                sim.Tick();
                if (sim.Ball.Velocity.X < Fix64.Zero)
                {
                    bouncedBack = true;
                    break;
                }
            }

            Assert.That(bouncedBack, Is.True, "rim-out must push the ball back out");
        }

        [Test]
        public void HoledSim_IgnoresFurtherShotsAndTicks()
        {
            var sim = new GolfSim(CourseWithHoleAt3(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 159));
            for (int i = 0; i < 2400 && !sim.IsHoled; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsHoled, Is.True);
            var strokes = sim.Strokes;
            sim.Shoot(new ShotInput(512, 255));
            Assert.That(sim.Strokes, Is.EqualTo(strokes), "no more strokes after holing out");
            sim.Tick();
            Assert.That(sim.Ball.Position, Is.EqualTo(V(3, 0)));
            Assert.That(sim.IsHoled, Is.True);
        }

        [Test]
        public void StateHash_ReflectsHoledFlag()
        {
            var holed = new GolfSim(CourseWithHoleAt3(), SimConfig.Default);
            var missed = new GolfSim(
                new CourseData(Vec2Fix.Zero, V(30, 0), par: 1, walls: System.Array.Empty<WallSegment>()),
                SimConfig.Default);

            holed.Shoot(new ShotInput(0, 159));
            missed.Shoot(new ShotInput(0, 159));
            for (int i = 0; i < 2400; i++)
            {
                holed.Tick();
                missed.Tick();
            }

            Assert.That(holed.IsHoled, Is.True);
            Assert.That(missed.IsHoled, Is.False);
            Assert.That(holed.StateHash(), Is.Not.EqualTo(missed.StateHash()));
        }

        [Test]
        public void HoleCapture_IsDeterministic()
        {
            var a = new GolfSim(CourseWithHoleAt3(), SimConfig.Default);
            var b = new GolfSim(CourseWithHoleAt3(), SimConfig.Default);
            a.Shoot(new ShotInput(0, 159));
            b.Shoot(new ShotInput(0, 159));
            for (int i = 0; i < 2400; i++)
            {
                a.Tick();
                b.Tick();
                Assert.That(a.StateHash(), Is.EqualTo(b.StateHash()), $"hash diverged at tick {i}");
            }
        }
    }
}
