using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class GolfSimIntegrationTests
    {
        private static CourseData EmptyCourse() => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: new Vec2Fix(Fix64.FromInt(100), Fix64.FromInt(100)),
            par: 2,
            walls: System.Array.Empty<WallSegment>());

        private static GolfSim NewSim() => new GolfSim(EmptyCourse(), SimConfig.Default);

        [Test]
        public void NewSim_BallStartsAtCourseStart_AtRest()
        {
            var sim = NewSim();
            Assert.That(sim.Ball.Position, Is.EqualTo(Vec2Fix.Zero));
            Assert.That(sim.Ball.Velocity, Is.EqualTo(Vec2Fix.Zero));
            Assert.That(sim.Strokes, Is.EqualTo(0));
            Assert.That(sim.TickCount, Is.EqualTo(0));
        }

        [Test]
        public void Shoot_AtAngleZero_FullPower_SetsVelocityAlongPositiveX()
        {
            var sim = NewSim();
            sim.Shoot(new ShotInput(0, 255));
            Assert.That(sim.Ball.Velocity.X, Is.EqualTo(SimConfig.Default.MaxShotSpeed));
            Assert.That(sim.Ball.Velocity.Y, Is.EqualTo(Fix64.Zero));
            Assert.That(sim.Strokes, Is.EqualTo(1));
        }

        [Test]
        public void Shoot_QuarterTurn_FullPower_SetsVelocityAlongPositiveY()
        {
            var sim = NewSim();
            sim.Shoot(new ShotInput(256, 255));
            Assert.That(sim.Ball.Velocity.X, Is.EqualTo(Fix64.Zero));
            Assert.That(sim.Ball.Velocity.Y, Is.EqualTo(SimConfig.Default.MaxShotSpeed));
        }

        [Test]
        public void Shoot_PowerScalesLinearly()
        {
            var sim = NewSim();
            sim.Shoot(new ShotInput(0, 127));
            // speed = maxSpeed * (powerIndex + 1) / 256
            var expected = SimConfig.Default.MaxShotSpeed * Fix64.FromFraction(128, 256);
            Assert.That(sim.Ball.Velocity.X, Is.EqualTo(expected));
        }

        [Test]
        public void Tick_AdvancesPosition_ByVelocityTimesDt()
        {
            var sim = NewSim();
            // Slow shot: no sub-stepping. Damping applies first (semi-implicit),
            // then position advances by exactly dampedV * dt.
            sim.Shoot(new ShotInput(0, 31)); // speed = max * 32/256 = 1 u/s with default max 8
            var v = sim.Ball.Velocity;
            sim.Tick();
            var damped = v.X * SimConfig.Default.RollDamping;
            Assert.That(sim.Ball.Position.X, Is.EqualTo(damped * SimConfig.Default.Dt));
            Assert.That(sim.Ball.Position.Y, Is.EqualTo(Fix64.Zero));
            Assert.That(sim.TickCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_AppliesExponentialFrictionDamping()
        {
            var sim = NewSim();
            sim.Shoot(new ShotInput(0, 255));
            var v0 = sim.Ball.Velocity.X;
            sim.Tick();
            Assert.That(sim.Ball.Velocity.X, Is.EqualTo(v0 * SimConfig.Default.RollDamping));
        }

        [Test]
        public void Velocity_DecaysMonotonically_UntilNegligible()
        {
            var sim = NewSim();
            sim.Shoot(new ShotInput(0, 255));
            var lastSpeedSq = sim.Ball.Velocity.LengthSq();
            for (int i = 0; i < 2400; i++) // 20 seconds
            {
                sim.Tick();
                var speedSq = sim.Ball.Velocity.LengthSq();
                Assert.That(speedSq <= lastSpeedSq, Is.True, $"speed increased at tick {i}");
                lastSpeedSq = speedSq;
            }

            // After 20 s of rolling friction the ball is essentially stopped.
            Assert.That(lastSpeedSq < Fix64.FromFraction(1, 1000), Is.True);
        }

        [Test]
        public void TwoSims_SameInputs_ProduceIdenticalTrajectories()
        {
            var a = NewSim();
            var b = NewSim();
            a.Shoot(new ShotInput(700, 200));
            b.Shoot(new ShotInput(700, 200));
            for (int i = 0; i < 600; i++)
            {
                a.Tick();
                b.Tick();
                Assert.That(a.Ball.Position, Is.EqualTo(b.Ball.Position));
                Assert.That(a.Ball.Velocity, Is.EqualTo(b.Ball.Velocity));
            }
        }
    }
}
