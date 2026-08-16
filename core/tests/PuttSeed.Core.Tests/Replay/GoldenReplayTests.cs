using NUnit.Framework;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.Replay;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Replay
{
    /// <summary>
    /// Golden replay fixtures (ARCHITECTURE.md): three fixed seeds are run
    /// end-to-end — generate course, replay the author solution — and the final
    /// state hash plus the encoded replay string are frozen here. A desync
    /// against these constants is a determinism regression by definition.
    /// </summary>
    [TestFixture]
    public class GoldenReplayTests
    {
        private static (ulong finalHash, string code) RunFixture(ulong seed)
        {
            var result = CourseGenerator.Generate(
                seed, GeneratorConfig.Default, SimConfig.Default, SolverConfig.Default);
            var sim = new GolfSim(result.Course, SimConfig.Default);
            int shotIdx = 0;
            for (int tick = 0; tick < 200_000 && !sim.IsHoled; tick++)
            {
                if (sim.IsAtRest && shotIdx < result.AuthorSolution.Length)
                {
                    sim.Shoot(result.AuthorSolution[shotIdx]);
                    shotIdx++;
                }

                sim.Tick();
            }

            Assert.That(sim.IsHoled, Is.True, $"fixture seed {seed} must hole out");
            return (sim.StateHash(), ReplayCodec.Encode(seed, result.AuthorSolution));
        }

        [TestCase(101UL, 12215932400520877527UL, "PUTT-AWUAAAAAAAAAAoD_A0B_AA")]
        [TestCase(2026UL, 3094461438326056594UL, "PUTT-AeoHAAAAAAAAAmB8A0B9Ag")]
        [TestCase(777UL, 6859109655031616931UL, "PUTT-AQkDAAAAAAAAAoD9A8B9Ag")]
        public void GoldenFixture_FinalHashAndCode_AreStable(ulong seed, ulong expectedHash, string expectedCode)
        {
            var (hash, code) = RunFixture(seed);
            Assert.That(hash, Is.EqualTo(expectedHash),
                $"seed {seed}: actual hash {hash}UL, code \"{code}\"");
            Assert.That(code, Is.EqualTo(expectedCode));
        }
    }
}
