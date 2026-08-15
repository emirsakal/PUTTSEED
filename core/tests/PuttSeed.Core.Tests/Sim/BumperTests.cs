using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class BumperTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        private static CourseData CourseWithBumperAt(Vec2Fix center) => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: V(50, 50),
            par: 2,
            walls: System.Array.Empty<WallSegment>(),
            bumpers: new[] { new Bumper(center, Fix64.FromFraction(3, 10)) });

        [Test]
        public void HeadOnHit_BouncesBack_WithSpeedBoost()
        {
            var sim = new GolfSim(CourseWithBumperAt(V(3, 0)), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 127)); // 4 u/s toward the bumper

            Fix64 speedIn = Fix64.Zero, speedOut = Fix64.Zero;
            for (int i = 0; i < 600; i++)
            {
                var vxPrev = sim.Ball.Velocity.X;
                sim.Tick();
                var vx = sim.Ball.Velocity.X;
                if (vxPrev > Fix64.Zero && vx < Fix64.Zero)
                {
                    speedIn = vxPrev;
                    speedOut = -vx;
                    break;
                }
            }

            Assert.That(speedOut > Fix64.Zero, Is.True, "ball never bounced off the bumper");
            Assert.That(speedOut > speedIn, Is.True,
                $"bumper must boost speed (in {speedIn}, out {speedOut})");
        }

        [Test]
        public void ExitSpeed_IsCapped()
        {
            var sim = new GolfSim(CourseWithBumperAt(V(3, 0)), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255)); // full power at the bumper
            var cap = SimConfig.Default.BumperMaxExitSpeed;
            for (int i = 0; i < 600; i++)
            {
                sim.Tick();
                var speedSq = sim.Ball.Velocity.LengthSq();
                Assert.That(speedSq <= cap * cap, Is.True, $"exit speed exceeded cap at tick {i}");
            }
        }

        [Test]
        public void BallNeverEndsTickInsideBumper()
        {
            var course = CourseWithBumperAt(V(3, 0));
            var sim = new GolfSim(course, SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            var minDist = SimConfig.Default.BallRadius + Fix64.FromFraction(3, 10);
            // Allow one raw ulp of slack for the push-out rounding.
            var minDistSq = minDist * minDist - Fix64.FromRaw(16);
            for (int i = 0; i < 600; i++)
            {
                sim.Tick();
                var deltaSq = (sim.Ball.Position - V(3, 0)).LengthSq();
                Assert.That(deltaSq >= minDistSq, Is.True, $"ball inside bumper at tick {i}");
            }
        }

        [Test]
        public void OffCenterHit_DeflectsBall()
        {
            // Bumper slightly above the shot line: ball should deflect downward.
            var course = new CourseData(
                startPosition: Vec2Fix.Zero,
                holePosition: V(50, 50),
                par: 2,
                walls: System.Array.Empty<WallSegment>(),
                bumpers: new[]
                {
                    new Bumper(new Vec2Fix(Fix64.FromInt(3), Fix64.FromFraction(2, 10)), Fix64.FromFraction(3, 10)),
                });
            var sim = new GolfSim(course, SimConfig.Default);
            sim.Shoot(new ShotInput(0, 200));
            bool deflected = false;
            for (int i = 0; i < 600; i++)
            {
                sim.Tick();
                if (sim.Ball.Velocity.Y < Fix64.Zero)
                {
                    deflected = true;
                    break;
                }
            }

            Assert.That(deflected, Is.True, "off-center bumper hit must deflect the ball");
        }

        [Test]
        public void BumperCollisions_AreDeterministic()
        {
            var a = new GolfSim(CourseWithBumperAt(V(3, 1)), SimConfig.Default);
            var b = new GolfSim(CourseWithBumperAt(V(3, 1)), SimConfig.Default);
            a.Shoot(new ShotInput(50, 255));
            b.Shoot(new ShotInput(50, 255));
            for (int i = 0; i < 1200; i++)
            {
                a.Tick();
                b.Tick();
                Assert.That(a.StateHash(), Is.EqualTo(b.StateHash()), $"hash diverged at tick {i}");
            }
        }
    }
}
