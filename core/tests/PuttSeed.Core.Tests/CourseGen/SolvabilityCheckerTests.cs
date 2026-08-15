using NUnit.Framework;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.CourseGen
{
    [TestFixture]
    public class SolvabilityCheckerTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        private static Vec2Fix VH(int x2, int y2) // half-units
            => new Vec2Fix(Fix64.FromFraction(x2, 2), Fix64.FromFraction(y2, 2));

        /// <summary>Straight walled box corridor, hole dead ahead.</summary>
        private static CourseData StraightCourse() => new CourseData(
            startPosition: VH(1, 0),
            holePosition: V(5, 0),
            par: 2,
            walls: new[]
            {
                new WallSegment(V(0, -1), V(6, -1)),
                new WallSegment(V(6, -1), V(6, 1)),
                new WallSegment(V(6, 1), V(0, 1)),
                new WallSegment(V(0, 1), V(0, -1)),
            });

        /// <summary>Same box, hole unreachable outside the walls.</summary>
        private static CourseData SealedCourse() => new CourseData(
            startPosition: VH(1, 0),
            holePosition: V(10, 10),
            par: 2,
            walls: new[]
            {
                new WallSegment(V(0, -1), V(6, -1)),
                new WallSegment(V(6, -1), V(6, 1)),
                new WallSegment(V(6, 1), V(0, 1)),
                new WallSegment(V(0, 1), V(0, -1)),
            });

        /// <summary>L-shaped corridor: along +x then up +y; hole around the corner.</summary>
        private static CourseData LCourse() => new CourseData(
            startPosition: VH(1, 0),
            holePosition: V(6, 5),
            par: 3,
            walls: new[]
            {
                new WallSegment(V(0, -1), V(7, -1)),  // bottom
                new WallSegment(V(7, -1), V(7, 6)),   // right
                new WallSegment(V(7, 6), V(5, 6)),    // top cap
                new WallSegment(V(5, 6), V(5, 1)),    // inner corner wall
                new WallSegment(V(5, 1), V(0, 1)),    // upper-left
                new WallSegment(V(0, 1), V(0, -1)),   // start cap
            });

        /// <summary>Replays a solution on a fresh sim; true if it holes out.</summary>
        private static bool Replays(CourseData course, ShotInput[] shots, out int strokes)
        {
            var sim = new GolfSim(course, SimConfig.Default);
            int shotIdx = 0;
            for (int tick = 0; tick < 100_000 && !sim.IsHoled; tick++)
            {
                if (sim.IsAtRest && shotIdx < shots.Length)
                {
                    sim.Shoot(shots[shotIdx]);
                    shotIdx++;
                }
                else if (sim.IsAtRest)
                {
                    break; // out of shots, still not holed
                }

                sim.Tick();
            }

            strokes = sim.Strokes;
            return sim.IsHoled;
        }

        [Test]
        public void StraightCourse_IsSolvable()
        {
            var result = SolvabilityChecker.Solve(StraightCourse(), SimConfig.Default, SolverConfig.Default);
            Assert.That(result.Solved, Is.True);
            Assert.That(result.AuthorSolution.Length, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.AuthorStrokes, Is.LessThanOrEqualTo(SolverConfig.Default.MaxPar));
        }

        [Test]
        public void AuthorSolution_ReplaysToCapture()
        {
            var course = StraightCourse();
            var result = SolvabilityChecker.Solve(course, SimConfig.Default, SolverConfig.Default);
            Assert.That(result.Solved, Is.True);
            Assert.That(Replays(course, result.AuthorSolution, out var strokes), Is.True,
                "author solution must reproduce the capture on a fresh sim");
            Assert.That(strokes, Is.EqualTo(result.AuthorStrokes),
                "replayed stroke count must match the solver's");
        }

        [Test]
        public void SealedCourse_IsNotSolvable_AndTerminates()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = SolvabilityChecker.Solve(SealedCourse(), SimConfig.Default, SolverConfig.Default);
            sw.Stop();
            Assert.That(result.Solved, Is.False);
            Assert.That(sw.Elapsed.TotalSeconds, Is.LessThan(30), "solver must be bounded");
        }

        [Test]
        public void LCourse_IsSolvable_AndReplays()
        {
            var course = LCourse();
            var result = SolvabilityChecker.Solve(course, SimConfig.Default, SolverConfig.Default);
            Assert.That(result.Solved, Is.True, "L corridor must be solvable within depth cap");
            Assert.That(Replays(course, result.AuthorSolution, out _), Is.True);
        }

        [Test]
        public void Solve_IsDeterministic()
        {
            var a = SolvabilityChecker.Solve(LCourse(), SimConfig.Default, SolverConfig.Default);
            var b = SolvabilityChecker.Solve(LCourse(), SimConfig.Default, SolverConfig.Default);
            Assert.That(a.Solved, Is.EqualTo(b.Solved));
            Assert.That(a.AuthorSolution.Length, Is.EqualTo(b.AuthorSolution.Length));
            for (int i = 0; i < a.AuthorSolution.Length; i++)
            {
                Assert.That(a.AuthorSolution[i].AngleIndex, Is.EqualTo(b.AuthorSolution[i].AngleIndex));
                Assert.That(a.AuthorSolution[i].PowerIndex, Is.EqualTo(b.AuthorSolution[i].PowerIndex));
            }
        }

        [Test]
        public void TightnessStats_ArePopulatedWhenSolved()
        {
            var result = SolvabilityChecker.Solve(StraightCourse(), SimConfig.Default, SolverConfig.Default);
            Assert.That(result.Solved, Is.True);
            Assert.That(result.SampledShotCount, Is.GreaterThan(0));
            Assert.That(result.CaptureShotCount, Is.InRange(1, result.SampledShotCount));
        }
    }
}
