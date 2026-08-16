using NUnit.Framework;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.Daily;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.CourseGen
{
    [TestFixture]
    public class CourseGeneratorTests
    {
        private static GenerationResult Gen(ulong seed)
            => CourseGenerator.Generate(seed, GeneratorConfig.Default, SimConfig.Default, SolverConfig.Default);

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
                    break;
                }

                sim.Tick();
            }

            strokes = sim.Strokes;
            return sim.IsHoled;
        }

        [Test]
        public void EverySeed_ProducesASolvableCourse()
        {
            for (ulong seed = 1; seed <= 8; seed++)
            {
                var result = Gen(seed);
                Assert.That(result.Course, Is.Not.Null, $"seed {seed}");
                Assert.That(result.AuthorSolution.Length, Is.GreaterThanOrEqualTo(1), $"seed {seed}");
                Assert.That(Replays(result.Course, result.AuthorSolution, out var strokes), Is.True,
                    $"seed {seed}: author solution must replay to capture");
                Assert.That(strokes, Is.EqualTo(result.AuthorStrokes), $"seed {seed}");
                Assert.That(strokes, Is.LessThanOrEqualTo(result.Course.Par), $"seed {seed}: over par");
            }
        }

        [Test]
        public void Par_IsWithinGddRange()
        {
            for (ulong seed = 1; seed <= 8; seed++)
            {
                var result = Gen(seed);
                Assert.That(result.Course.Par, Is.InRange(2, 5), $"seed {seed}");
            }
        }

        [Test]
        public void SameSeed_IdenticalCourseAndSolution()
        {
            var a = Gen(42);
            var b = Gen(42);

            Assert.That(a.Course.StartPosition, Is.EqualTo(b.Course.StartPosition));
            Assert.That(a.Course.HolePosition, Is.EqualTo(b.Course.HolePosition));
            Assert.That(a.Course.Par, Is.EqualTo(b.Course.Par));
            Assert.That(a.Course.Walls.Length, Is.EqualTo(b.Course.Walls.Length));
            for (int i = 0; i < a.Course.Walls.Length; i++)
            {
                Assert.That(a.Course.Walls[i].A, Is.EqualTo(b.Course.Walls[i].A));
                Assert.That(a.Course.Walls[i].B, Is.EqualTo(b.Course.Walls[i].B));
            }

            Assert.That(a.Course.Bumpers.Length, Is.EqualTo(b.Course.Bumpers.Length));
            Assert.That(a.Course.SandZones.Length, Is.EqualTo(b.Course.SandZones.Length));
            Assert.That(a.Course.WaterZones.Length, Is.EqualTo(b.Course.WaterZones.Length));
            Assert.That(a.AuthorSolution.Length, Is.EqualTo(b.AuthorSolution.Length));
            for (int i = 0; i < a.AuthorSolution.Length; i++)
            {
                Assert.That(a.AuthorSolution[i].AngleIndex, Is.EqualTo(b.AuthorSolution[i].AngleIndex));
                Assert.That(a.AuthorSolution[i].PowerIndex, Is.EqualTo(b.AuthorSolution[i].PowerIndex));
            }

            Assert.That(a.Difficulty, Is.EqualTo(b.Difficulty));
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentCourses()
        {
            var a = Gen(1);
            var b = Gen(2);
            bool anyDifference =
                a.Course.HolePosition != b.Course.HolePosition
                || a.Course.Walls.Length != b.Course.Walls.Length
                || a.Course.StartPosition != b.Course.StartPosition;
            Assert.That(anyDifference, Is.True);
        }

        [Test]
        public void Attempts_AreBounded()
        {
            for (ulong seed = 1; seed <= 8; seed++)
            {
                var result = Gen(seed);
                Assert.That(result.Attempts, Is.LessThanOrEqualTo(4 * GeneratorConfig.Default.AttemptsPerLevel),
                    $"seed {seed}");
            }
        }

        [Test]
        public void DailySeed_GeneratesTodaysCourse()
        {
            var seed = DailySeed.FromUtcDate(2026, 8, 16);
            var result = Gen(seed);
            Assert.That(result.AuthorSolution.Length, Is.GreaterThanOrEqualTo(1));
            Assert.That(Replays(result.Course, result.AuthorSolution, out _), Is.True);
        }
    }
}
