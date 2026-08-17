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
        public void Phase_DependsOnlyOnTicksSinceShot()
        {
            // The solver's BFS treats a rest state as the whole state. That is
            // only sound if a shot's outcome never depends on how many ticks
            // the sim ran before it — i.e. blades re-arm to phase0 per shot.
            var direct = new GolfSim(CourseWithMill(omegaSteps: 4), SimConfig.Default);
            var detoured = new GolfSim(CourseWithMill(omegaSteps: 4), SimConfig.Default);

            // The detoured sim burns a wildly different tick history first.
            detoured.Shoot(new ShotInput(768, 40)); // a little hop in -y
            for (int i = 0; i < 3000 && !detoured.IsAtRest; i++)
            {
                detoured.Tick();
            }

            for (int i = 0; i < 500; i++)
            {
                detoured.Tick(); // extra idle ticks at rest
            }

            var restart = new Vec2Fix(Fix64.Zero, Fix64.Zero);
            direct.RestoreRest(restart, 0);
            detoured.RestoreRest(restart, 0);

            direct.Shoot(new ShotInput(0, 255));
            detoured.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 2400; i++)
            {
                direct.Tick();
                detoured.Tick();
                Assert.That(detoured.Ball.Position.X.Raw, Is.EqualTo(direct.Ball.Position.X.Raw),
                    $"trajectory must not depend on prior tick history (tick {i})");
                Assert.That(detoured.Ball.Position.Y.Raw, Is.EqualTo(direct.Ball.Position.Y.Raw),
                    $"trajectory must not depend on prior tick history (tick {i})");
            }
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
        public void TicksSinceShot_TracksTheShotClock()
        {
            var sim = new GolfSim(BareCourse(), SimConfig.Default);
            Assert.That(sim.TicksSinceShot, Is.Zero);

            sim.Shoot(new ShotInput(0, 100));
            for (int i = 0; i < 7; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.TicksSinceShot, Is.EqualTo(7), "the clock counts moving ticks");

            sim.RestoreRest(Vec2Fix.Zero, 0);
            Assert.That(sim.TicksSinceShot, Is.Zero, "RestoreRest re-arms the clock");

            sim.Shoot(new ShotInput(0, 100));
            Assert.That(sim.TicksSinceShot, Is.Zero, "Shoot re-arms the clock");
        }
    }
}
