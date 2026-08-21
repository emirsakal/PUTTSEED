using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// The near miss, driven by the real simulation rather than by numbers
    /// invented to suit it: a shot that passes the cup must produce exactly one
    /// event, and a shot that goes in must produce none.
    /// </summary>
    public class NearMissTests
    {
        private static CourseData CourseWithHoleOffLine(int tenths) => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: new Vec2Fix(Fix64.FromInt(5), Fix64.FromFraction(tenths, 100)),
            par: 2,
            walls: System.Array.Empty<WallSegment>());

        /// <summary>Plays one shot to rest, counting near-miss events.</summary>
        private static int EventsFor(CourseData course, ShotInput shot, out bool holed)
        {
            var sim = new GolfSim(course, SimConfig.Default);
            var watch = new NearMissWatch();
            float cupRadius = FixView.ToFloat(SimConfig.Default.HoleRadius);
            int events = 0;

            sim.Shoot(shot);
            for (int i = 0; i < 4000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
                var cup = FixView.ToVector2(course.HolePosition);
                var ball = FixView.ToVector2(sim.Ball.Position);
                if (watch.Observe(UnityEngine.Vector2.Distance(ball, cup), cupRadius,
                    sim.IsHoled, !sim.IsAtRest))
                {
                    events++;
                }
            }

            holed = sim.IsHoled;
            return events;
        }

        [Test]
        public void APassByTheCup_IsOneEvent()
        {
            // A quarter of a unit off line: inside two cup radii, outside the
            // cup itself, so the ball cannot be captured on the way past.
            int events = EventsFor(CourseWithHoleOffLine(25), new ShotInput(0, 255), out bool holed);

            Assert.That(holed, Is.False, "the fixture must MISS, or it tests nothing");
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void AShotThatDrops_IsNoEvent()
        {
            var course = CourseWithHoleOffLine(0); // dead on line

            // Find a power that actually holes out, then assert the pass that
            // ended in the cup said nothing.
            bool found = false;
            for (int power = 0; power < 256 && !found; power++)
            {
                int events = EventsFor(course, new ShotInput(0, power), out bool holed);
                if (!holed)
                {
                    continue;
                }

                found = true;
                Assert.That(events, Is.EqualTo(0), $"power {power} holed out and still cried near miss");
            }

            Assert.That(found, Is.True, "no power holed out, so nothing was proven");
        }

        [Test]
        public void StoppingAtTheLip_IsAnEventToo()
        {
            // The cruellest miss of all: the ball parks beside the cup. There
            // is no leaving the ring to notice, so the pass ends when the ball
            // does.
            var watch = new NearMissWatch();
            Assert.That(watch.Observe(0.2f, 0.15f, holed: false, moving: true), Is.False);
            Assert.That(watch.Observe(0.2f, 0.15f, holed: false, moving: false), Is.True);

            // And it stays quiet while the ball sits there.
            for (int i = 0; i < 10; i++)
            {
                Assert.That(watch.Observe(0.2f, 0.15f, holed: false, moving: false), Is.False);
            }
        }

        [Test]
        public void TwoPasses_AreTwoEvents()
        {
            var watch = new NearMissWatch();
            int events = 0;
            for (int pass = 0; pass < 2; pass++)
            {
                events += watch.Observe(0.2f, 0.15f, false, true) ? 1 : 0;  // in
                events += watch.Observe(2.0f, 0.15f, false, true) ? 1 : 0;  // out
            }

            Assert.That(events, Is.EqualTo(2));
        }

        [Test]
        public void FarFromTheCup_IsNeverAnEvent()
        {
            var watch = new NearMissWatch();
            for (int i = 0; i < 50; i++)
            {
                Assert.That(watch.Observe(1.5f, 0.15f, false, true), Is.False);
                Assert.That(watch.Observe(1.5f, 0.15f, false, false), Is.False);
            }
        }
    }
}
