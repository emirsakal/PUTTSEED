using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    /// <summary>
    /// Presentation-facing run observations: deterministic, but deliberately
    /// outside StateHash. They exist so achievements can be detected exactly
    /// instead of guessed at.
    /// </summary>
    [TestFixture]
    public class RunObservationTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        /// <summary>A narrow corridor along x: the ball rattles between two walls.</summary>
        private static CourseData Corridor() => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: V(50, 50),
            par: 2,
            walls: new[]
            {
                new WallSegment(V(-1, 1), V(20, 1)),
                new WallSegment(V(-1, -1), V(20, -1)),
                new WallSegment(V(20, -1), V(20, 1)),
            });

        [Test]
        public void WallHitsThisShot_CountsWithinAShot_AndResetsOnTheNext()
        {
            var sim = new GolfSim(Corridor(), SimConfig.Default);
            sim.Shoot(new ShotInput(40, 255)); // angled: bounces down the corridor
            for (int i = 0; i < 2400 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            int firstShotHits = sim.WallHitsThisShot;
            Assert.That(firstShotHits, Is.GreaterThan(0), "the ball must have hit a wall");
            Assert.That(sim.WallHitCount, Is.EqualTo(firstShotHits), "one shot so far");

            sim.Shoot(new ShotInput(0, 20)); // a gentle nudge that touches nothing
            Assert.That(sim.WallHitsThisShot, Is.Zero, "a new shot re-arms the per-shot count");
            for (int i = 0; i < 2400 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.WallHitCount, Is.GreaterThanOrEqualTo(firstShotHits),
                "the run total never decreases");
        }

        [Test]
        public void TouchedHazard_StaysFalse_OnABareCourse()
        {
            var sim = new GolfSim(Corridor(), SimConfig.Default);
            Assert.That(sim.TouchedHazard, Is.False);
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 2400 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.TouchedHazard, Is.False, "walls are not hazards");
        }

        [Test]
        public void TouchedHazard_TripsOnSand()
        {
            var course = new CourseData(Vec2Fix.Zero, V(50, 50), 2,
                System.Array.Empty<WallSegment>(),
                sandZones: new[] { new ZonePolygon(new[] { V(2, -2), V(4, -2), V(4, 2), V(2, 2) }) });
            var sim = new GolfSim(course, SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 2400 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.TouchedHazard, Is.True);
        }

        [Test]
        public void TouchedHazard_TripsOnABumper()
        {
            var course = new CourseData(Vec2Fix.Zero, V(50, 50), 2,
                System.Array.Empty<WallSegment>(),
                bumpers: new[] { new Bumper(V(3, 0), Fix64.FromFraction(3, 10)) });
            var sim = new GolfSim(course, SimConfig.Default);
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 2400 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.TouchedHazard, Is.True);
        }

        [Test]
        public void Observations_SurviveARestoreRest_AsAFreshShot()
        {
            // RestoreRest is the solver's expansion hook: it starts a new shot,
            // so the per-shot count must re-arm exactly as Shoot does.
            var sim = new GolfSim(Corridor(), SimConfig.Default);
            sim.Shoot(new ShotInput(40, 255));
            for (int i = 0; i < 2400 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.WallHitsThisShot, Is.GreaterThan(0));
            sim.RestoreRest(Vec2Fix.Zero, 0);
            Assert.That(sim.WallHitsThisShot, Is.Zero);
        }
    }
}
