using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class RestoreRestTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        private static CourseData EmptyCourse() => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: V(100, 100),
            par: 2,
            walls: System.Array.Empty<WallSegment>());

        [Test]
        public void RestoreRest_PlacesBallAtRestWithStrokes()
        {
            var sim = new GolfSim(EmptyCourse(), SimConfig.Default);
            sim.RestoreRest(V(5, 3), strokes: 2);
            Assert.That(sim.Ball.Position, Is.EqualTo(V(5, 3)));
            Assert.That(sim.Ball.Velocity, Is.EqualTo(Vec2Fix.Zero));
            Assert.That(sim.Strokes, Is.EqualTo(2));
            Assert.That(sim.IsAtRest, Is.True);
            Assert.That(sim.IsHoled, Is.False);
        }

        [Test]
        public void RestoreRest_AcceptsNextShotImmediately()
        {
            var sim = new GolfSim(EmptyCourse(), SimConfig.Default);
            sim.RestoreRest(V(5, 3), strokes: 1);
            sim.Shoot(new ShotInput(0, 100));
            Assert.That(sim.Strokes, Is.EqualTo(2));
            Assert.That(sim.IsAtRest, Is.False);
        }

        [Test]
        public void RestoreRest_SetsWaterResetAnchor()
        {
            // Water pocket ahead: after RestoreRest the last-rest anchor must be
            // the restored position, so a water shot returns there.
            var course = new CourseData(
                startPosition: Vec2Fix.Zero,
                holePosition: V(100, 100),
                par: 2,
                walls: System.Array.Empty<WallSegment>(),
                waterZones: new[] { new ZonePolygon(new[] { V(7, 2), V(9, 2), V(9, 4), V(7, 4) }) });
            var sim = new GolfSim(course, SimConfig.Default);
            sim.RestoreRest(V(5, 3), strokes: 1);
            sim.Shoot(new ShotInput(0, 255)); // straight into water
            for (int i = 0; i < 1200 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.Ball.Position, Is.EqualTo(V(5, 3)));
            Assert.That(sim.Strokes, Is.EqualTo(3), "shot + penalty on top of restored strokes");
        }

        [Test]
        public void RestoredSim_MatchesNaturallyReachedState_Trajectory()
        {
            // A sim restored to a rest state must continue exactly like a sim
            // that reached the same rest state by playing.
            var played = new GolfSim(EmptyCourse(), SimConfig.Default);
            played.Shoot(new ShotInput(70, 90));
            for (int i = 0; i < 12000 && !played.IsAtRest; i++)
            {
                played.Tick();
            }

            var restored = new GolfSim(EmptyCourse(), SimConfig.Default);
            restored.RestoreRest(played.Ball.Position, played.Strokes);

            played.Shoot(new ShotInput(400, 200));
            restored.Shoot(new ShotInput(400, 200));
            for (int i = 0; i < 1200; i++)
            {
                played.Tick();
                restored.Tick();
                Assert.That(restored.Ball.Position, Is.EqualTo(played.Ball.Position), $"tick {i}");
                Assert.That(restored.Ball.Velocity, Is.EqualTo(played.Ball.Velocity), $"tick {i}");
            }
        }
    }
}
