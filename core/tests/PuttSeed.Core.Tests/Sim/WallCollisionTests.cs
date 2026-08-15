using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class WallCollisionTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        /// <summary>Vertical wall at x=2 spanning y in [-2, 2].</summary>
        private static CourseData CourseWithVerticalWall() => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: V(50, 50),
            par: 2,
            walls: new[] { new WallSegment(V(2, -2), V(2, 2)) });

        [Test]
        public void HeadOnShot_BouncesBack()
        {
            var sim = new GolfSim(CourseWithVerticalWall(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255)); // +x, full power
            for (int i = 0; i < 120; i++)
            {
                sim.Tick();
            }

            // After 1 s the ball has hit the wall and is traveling back (-x).
            Assert.That(sim.Ball.Velocity.X < Fix64.Zero, Is.True, "ball did not bounce back");
            Assert.That(sim.Ball.Position.X < Fix64.FromInt(2), Is.True, "ball ended up beyond the wall");
        }

        [Test]
        public void HeadOnShot_NeverTunnels_AtFullPower()
        {
            var sim = new GolfSim(CourseWithVerticalWall(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            var wallX = Fix64.FromInt(2);
            for (int i = 0; i < 600; i++)
            {
                sim.Tick();
                Assert.That(sim.Ball.Position.X < wallX, Is.True, $"tunneled through wall at tick {i}");
            }
        }

        [Test]
        public void Bounce_LosesEnergy_ByRestitution()
        {
            var sim = new GolfSim(CourseWithVerticalWall(), SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));

            // Sample speed right before impact and right after the sign flip.
            Fix64 speedBefore = sim.Ball.Velocity.X;
            Fix64 speedAfter = Fix64.Zero;
            for (int i = 0; i < 240; i++)
            {
                var vxPrev = sim.Ball.Velocity.X;
                sim.Tick();
                var vx = sim.Ball.Velocity.X;
                if (vxPrev > Fix64.Zero && vx < Fix64.Zero)
                {
                    speedBefore = vxPrev;
                    speedAfter = -vx;
                    break;
                }
            }

            Assert.That(speedAfter > Fix64.Zero, Is.True, "no bounce happened");
            // Restitution 0.8: outgoing normal speed is roughly 80% of incoming
            // (damping in the same tick makes it slightly less; allow a band).
            var ratio = speedAfter / speedBefore;
            Assert.That(ratio > Fix64.FromFraction(7, 10), Is.True, $"bounce too dead: {ratio}");
            Assert.That(ratio < Fix64.FromFraction(85, 100), Is.True, $"bounce too lively: {ratio}");
        }

        [Test]
        public void ObliqueShot_ReflectsNormal_KeepsTangential()
        {
            var sim = new GolfSim(CourseWithVerticalWall(), SimConfig.Default);
            sim.Shoot(new ShotInput(128, 255)); // 45 degrees up-right
            bool bounced = false;
            for (int i = 0; i < 240; i++)
            {
                sim.Tick();
                if (sim.Ball.Velocity.X < Fix64.Zero)
                {
                    bounced = true;
                    // Tangential (y) component keeps its sign after the bounce.
                    Assert.That(sim.Ball.Velocity.Y > Fix64.Zero, Is.True, "tangential velocity flipped");
                    break;
                }
            }

            Assert.That(bounced, Is.True, "ball never bounced off the wall");
        }

        [Test]
        public void ShotPastSegmentEnd_DoesNotCollide()
        {
            // Wall spans y in [-2,2]; shoot from (0,5): straight +x, above the wall.
            var course = new CourseData(
                startPosition: V(0, 5),
                holePosition: V(50, 50),
                par: 2,
                walls: new[] { new WallSegment(V(2, -2), V(2, 2)) });
            var sim = new GolfSim(course, SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 240; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.Ball.Position.X > Fix64.FromInt(2), Is.True,
                "ball should have passed beyond the wall's x, missing it entirely");
        }

        [Test]
        public void BallInCorner_IsPushedOut_NotStuck()
        {
            // Two walls forming a corner at (2,2).
            var course = new CourseData(
                startPosition: Vec2Fix.Zero,
                holePosition: V(50, 50),
                par: 2,
                walls: new[]
                {
                    new WallSegment(V(2, -2), V(2, 2)),
                    new WallSegment(V(-2, 2), V(2, 2)),
                });
            var sim = new GolfSim(course, SimConfig.Default);
            sim.Shoot(new ShotInput(128, 255)); // 45 degrees toward the corner
            for (int i = 0; i < 600; i++)
            {
                sim.Tick();
                Assert.That(sim.Ball.Position.X < Fix64.FromInt(2), Is.True, $"escaped through x wall at tick {i}");
                Assert.That(sim.Ball.Position.Y < Fix64.FromInt(2), Is.True, $"escaped through y wall at tick {i}");
            }
        }

        [Test]
        public void WallCollisions_AreDeterministic()
        {
            var a = new GolfSim(CourseWithVerticalWall(), SimConfig.Default);
            var b = new GolfSim(CourseWithVerticalWall(), SimConfig.Default);
            a.Shoot(new ShotInput(96, 255));
            b.Shoot(new ShotInput(96, 255));
            for (int i = 0; i < 1200; i++)
            {
                a.Tick();
                b.Tick();
                Assert.That(a.Ball.Position, Is.EqualTo(b.Ball.Position), $"positions diverged at tick {i}");
                Assert.That(a.Ball.Velocity, Is.EqualTo(b.Ball.Velocity), $"velocities diverged at tick {i}");
            }
        }
    }
}
