using System.Collections.Concurrent;
using System.Threading.Tasks;
using NUnit.Framework;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.Replay;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.CourseGen
{
    /// <summary>
    /// The ROADMAP Week-2 property suite: 1000 seeds through the full pipeline.
    /// Each seed must generate (bounded), be solvable (author solution replays
    /// to capture within par) and round-trip through the replay codec. Seeds
    /// run in parallel — each iteration owns its sims, so parallelism cannot
    /// affect per-seed determinism.
    /// </summary>
    [TestFixture]
    public class PropertySuiteTests
    {
        private const int SeedCount = 1000;

        private static bool Replays(CourseData course, ShotInput[] shots, out int strokes)
        {
            var sim = new GolfSim(course, SimConfig.Default);
            int shotIdx = 0;
            for (int tick = 0; tick < 200_000 && !sim.IsHoled; tick++)
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
        public void ThousandSeeds_GenerateSolvableRoundTrippingCourses()
        {
            var failures = new ConcurrentBag<string>();
            int maxAttempts = 4 * GeneratorConfig.Default.AttemptsPerLevel;

            Parallel.For(1, SeedCount + 1, seedInt =>
            {
                ulong seed = (ulong)seedInt;
                try
                {
                    var result = CourseGenerator.Generate(
                        seed, GeneratorConfig.Default, SimConfig.Default, SolverConfig.Default);

                    if (result.Attempts > maxAttempts)
                    {
                        failures.Add($"seed {seed}: attempts {result.Attempts} over bound");
                    }

                    if (result.Course.Par < 2 || result.Course.Par > SolverConfig.Default.MaxPar)
                    {
                        failures.Add($"seed {seed}: par {result.Course.Par} out of range");
                    }

                    if (!Replays(result.Course, result.AuthorSolution, out var strokes))
                    {
                        failures.Add($"seed {seed}: author solution does not capture");
                    }
                    else
                    {
                        if (strokes != result.AuthorStrokes)
                        {
                            failures.Add($"seed {seed}: replay strokes {strokes} != author {result.AuthorStrokes}");
                        }

                        if (strokes > result.Course.Par)
                        {
                            failures.Add($"seed {seed}: author solution over par");
                        }
                    }

                    var code = ReplayCodec.Encode(seed, result.AuthorSolution);
                    if (!ReplayCodec.TryDecode(code, out var seed2, out var shots2)
                        || seed2 != seed
                        || shots2.Length != result.AuthorSolution.Length)
                    {
                        failures.Add($"seed {seed}: codec round-trip failed");
                    }
                    else
                    {
                        for (int i = 0; i < shots2.Length; i++)
                        {
                            if (shots2[i].AngleIndex != result.AuthorSolution[i].AngleIndex
                                || shots2[i].PowerIndex != result.AuthorSolution[i].PowerIndex)
                            {
                                failures.Add($"seed {seed}: codec shot {i} mismatch");
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    failures.Add($"seed {seed}: {e.GetType().Name}: {e.Message}");
                }
            });

            Assert.That(failures, Is.Empty,
                $"{failures.Count} failures:\n{string.Join("\n", failures)}");
        }
    }
}
