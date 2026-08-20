using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    /// <summary>
    /// Wind on a rolling ball, and — the part that took a player to notice —
    /// wind on a ball that has stopped rolling.
    /// </summary>
    [TestFixture]
    public class WindTests
    {
        private static CourseData EmptyCourse() => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: new Vec2Fix(Fix64.FromInt(100), Fix64.FromInt(100)),
            par: 2,
            walls: System.Array.Empty<WallSegment>());

        /// <summary>The strength every windy day blows at.</summary>
        private static SimConfig Windy() => SimConfig.Default.WithWind(
            new Vec2Fix(Fix64.FromFraction(65, 100), Fix64.Zero));

        [Test]
        public void WindyDay_BallComesToRest_AndStaysThere()
        {
            var sim = new GolfSim(EmptyCourse(), Windy());
            sim.Shoot(new ShotInput(0, 255));

            // Ten seconds is far longer than any putt takes to die.
            for (int i = 0; i < 1200 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsAtRest, Is.True,
                "a steady wind used to hold the ball above the rest threshold forever");

            // And it stays put: friction is viscous, so it produces no force at
            // zero speed — a wind that still pushed here would walk a resting
            // ball across the green and hammer it into the nearest wall.
            var settled = sim.Ball.Position;
            for (int i = 0; i < 600; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.Ball.Position.X.Raw, Is.EqualTo(settled.X.Raw));
            Assert.That(sim.Ball.Position.Y.Raw, Is.EqualTo(settled.Y.Raw));
            Assert.That(sim.IsAtRest, Is.True);
        }

        [Test]
        public void WindStillBendsARollingBall()
        {
            var still = new GolfSim(EmptyCourse(), SimConfig.Default);
            var blown = new GolfSim(EmptyCourse(),
                SimConfig.Default.WithWind(new Vec2Fix(Fix64.Zero, Fix64.FromFraction(65, 100))));

            var shot = new ShotInput(0, 255); // straight along +x
            still.Shoot(shot);
            blown.Shoot(shot);
            for (int i = 0; i < 1200; i++)
            {
                still.Tick();
                blown.Tick();
            }

            // The crosswind must have moved it off line by something a player
            // would see — a tenth of a unit is the ball's own radius.
            var drift = blown.Ball.Position.Y - still.Ball.Position.Y;
            Assert.That(drift.Raw, Is.GreaterThan(Fix64.FromFraction(1, 10).Raw),
                "wind that cannot bend a roll is not a mechanic");
        }

        [Test]
        public void WindNeverDrivesTheBallOnItsOwn()
        {
            // Nudged to a crawl: below the speed the wind alone could sustain,
            // so the wind must not pick it back up.
            var sim = new GolfSim(EmptyCourse(), Windy());
            sim.Shoot(new ShotInput(0, 0)); // the weakest shot there is
            for (int i = 0; i < 1200 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsAtRest, Is.True);
        }
    }
}
