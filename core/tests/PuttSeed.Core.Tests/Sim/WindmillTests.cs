using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class WindmillTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        /// <summary>
        /// A two-blade mill at (3,0): phase0 256 puts the blade pair vertical
        /// (a full diameter wall across the shot line at x=3).
        /// </summary>
        private static CourseData CourseWithMill(int omegaSteps, int phase0 = 256) => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: V(50, 50),
            par: 2,
            walls: System.Array.Empty<WallSegment>(),
            windmills: new[]
            {
                new Windmill(V(3, 0), Fix64.FromInt(2), bladeCount: 2,
                    omegaSteps: omegaSteps, phase0: phase0),
            });

        private static CourseData BareCourse() => new CourseData(
            Vec2Fix.Zero, V(50, 50), 2, System.Array.Empty<WallSegment>());

        [Test]
        public void StaticMill_BlocksLikeAWall()
        {
            var sim = new GolfSim(CourseWithMill(omegaSteps: 0), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 12000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsAtRest, Is.True);
            Assert.That(sim.Ball.Position.X < Fix64.FromInt(3), Is.True,
                $"a vertical static blade pair must block the shot (rested at {sim.Ball.Position.X})");
            Assert.That(sim.WindmillHitCount, Is.GreaterThan(0));
        }

        [Test]
        public void Rotation_ChangesTheOutcome()
        {
            var still = new GolfSim(CourseWithMill(omegaSteps: 0), SimConfig.Default);
            var spinning = new GolfSim(CourseWithMill(omegaSteps: 4), SimConfig.Default);
            still.Shoot(new ShotInput(0, 255));
            spinning.Shoot(new ShotInput(0, 255));

            bool diverged = false;
            for (int i = 0; i < 2400; i++)
            {
                still.Tick();
                spinning.Tick();
                if (still.StateHash() != spinning.StateHash())
                {
                    diverged = true;
                    break;
                }
            }

            Assert.That(diverged, Is.True, "a spinning mill must produce different dynamics");
        }

        [Test]
        public void WaitingBeforeTheShot_ChangesTheOutcome()
        {
            // The point of a free-running mill: blades keep turning while you
            // line up, so the angle you launch into is the one you waited for.
            var immediate = new GolfSim(CourseWithMill(omegaSteps: 4), SimConfig.Default);
            var patient = new GolfSim(CourseWithMill(omegaSteps: 4), SimConfig.Default);

            for (int i = 0; i < 37; i++)
            {
                patient.Tick(); // at rest: only the blades move
            }

            immediate.Shoot(new ShotInput(0, 255));
            patient.Shoot(new ShotInput(0, 255));

            bool diverged = false;
            for (int i = 0; i < 2400; i++)
            {
                immediate.Tick();
                patient.Tick();
                if (immediate.StateHash() != patient.StateHash())
                {
                    diverged = true;
                    break;
                }
            }

            Assert.That(diverged, Is.True, "the wait must change what the shot meets");
        }

        [Test]
        public void SameClockValue_SameTrajectory()
        {
            // What lets a replay store 10 bits per shot: the trajectory
            // depends on the clock VALUE, not on how the clock got there.
            var a = new GolfSim(CourseWithMill(omegaSteps: 3), SimConfig.Default);
            var b = new GolfSim(CourseWithMill(omegaSteps: 3), SimConfig.Default);

            for (int i = 0; i < 50; i++)
            {
                a.Tick();
            }

            for (int i = 0; i < 50 + GolfSim.MillClockPeriod; i++)
            {
                b.Tick(); // a full extra wrap of waiting
            }

            Assert.That(b.MillClock, Is.EqualTo(a.MillClock), "setup: same phase");

            a.Shoot(new ShotInput(0, 255));
            b.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 2400; i++)
            {
                a.Tick();
                b.Tick();

                // Positions, not StateHash: the hash folds in TickCount, and
                // b deliberately waited a full extra turn to get here.
                Assert.That(b.Ball.Position.X.Raw, Is.EqualTo(a.Ball.Position.X.Raw), $"tick {i}");
                Assert.That(b.Ball.Position.Y.Raw, Is.EqualTo(a.Ball.Position.Y.Raw), $"tick {i}");
                Assert.That(b.Ball.Velocity.X.Raw, Is.EqualTo(a.Ball.Velocity.X.Raw), $"tick {i}");
                Assert.That(b.Ball.Velocity.Y.Raw, Is.EqualTo(a.Ball.Velocity.Y.Raw), $"tick {i}");
            }
        }

        [Test]
        public void RestoreRest_ReArmsTheClock_KeepingSolverNodesReproducible()
        {
            // The solver expands rest states with RestoreRest; if the clock
            // survived, the same node would explore different blade angles
            // depending on how the search reached it.
            var sim = new GolfSim(CourseWithMill(omegaSteps: 4), SimConfig.Default);
            for (int i = 0; i < 77; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.MillClock, Is.Not.Zero);
            sim.RestoreRest(Vec2Fix.Zero, 0);
            Assert.That(sim.MillClock, Is.Zero);
        }

        [Test]
        public void MillOffThePath_HasNoEffect()
        {
            var bare = new GolfSim(BareCourse(), SimConfig.Default);
            var milled = new GolfSim(new CourseData(
                Vec2Fix.Zero, V(50, 50), 2, System.Array.Empty<WallSegment>(),
                windmills: new[]
                {
                    new Windmill(V(3, 10), Fix64.FromInt(2), 2, 4, 0),
                }), SimConfig.Default);
            bare.Shoot(new ShotInput(0, 255));
            milled.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 1200; i++)
            {
                bare.Tick();
                milled.Tick();
                Assert.That(milled.StateHash(), Is.EqualTo(bare.StateHash()), $"diverged at tick {i}");
            }
        }

        [Test]
        public void CourseWithoutMills_KeepsLegacyBehavior()
        {
            Assert.That(BareCourse().Windmills, Is.Empty);
        }

        [Test]
        public void MillClock_AdvancesAtRest_AndWrapsAtThePeriod()
        {
            var sim = new GolfSim(BareCourse(), SimConfig.Default);
            Assert.That(sim.MillClock, Is.Zero);

            for (int i = 0; i < 7; i++)
            {
                sim.Tick(); // never shot: the ball is at rest the whole time
            }

            Assert.That(sim.MillClock, Is.EqualTo(7), "blades turn while you think");

            for (int i = 0; i < GolfSim.MillClockPeriod; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.MillClock, Is.EqualTo(7), "a full period returns the same phase");
            Assert.That(sim.MillClock, Is.InRange(0, GolfSim.MillClockPeriod - 1));
        }
    }
}
