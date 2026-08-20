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
            => RunFixture(seed, GeneratorConfig.Default, SolverConfig.Default, 1);

        private static (ulong finalHash, string code) RunFixture(
            ulong seed, GeneratorConfig cfg, SolverConfig solver, int wireVersion)
        {
            var result = CourseGenerator.Generate(seed, cfg, SimConfig.Default, solver);
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
            var clocks = wireVersion >= 3 ? new int[result.AuthorSolution.Length] : null;
            return (sim.StateHash(),
                ReplayCodec.Encode(seed, result.AuthorSolution, clocks, wireVersion));
        }

        // Re-frozen 2026-08-16 (mitered wall joints changed all wall geometry;
        // earlier re-freeze same day: ice zones entering generation).
        [TestCase(101UL, 13061230583260924845UL, "PUTT-AWUAAAAAAAAAAgB_A6B_AA")]
        [TestCase(2026UL, 4755818037070277499UL, "PUTT-AeoHAAAAAAAAAiD8A4D9Aw")]
        [TestCase(777UL, 17538158318594718253UL, "PUTT-AQkDAAAAAAAAAoD9A-D8Ag")]
        public void GoldenFixture_FinalHashAndCode_AreStable(ulong seed, ulong expectedHash, string expectedCode)
        {
            var (hash, code) = RunFixture(seed);
            Assert.That(hash, Is.EqualTo(expectedHash),
                $"seed {seed}: actual hash {hash}UL, code \"{code}\"");
            Assert.That(code, Is.EqualTo(expectedCode));
        }

        /// <summary>
        /// The same three seeds on the generator the game actually ships:
        /// v4, where a hole can be worth three strokes. The v1 fixtures above
        /// still pin the frozen path — a version byte that was ever emitted
        /// must keep decoding — but the pipeline players meet needs its own
        /// proof, and one of these is a par 3.
        /// </summary>
        // Frozen 2026-08-19, the day the game moved to v4.
        [TestCase(101UL, 199119807181220958UL, "PUTT-BGUAAAAAAAAAAiD8AwDg_gEA")]
        [TestCase(2026UL, 14350009124846679921UL, "PUTT-BOoHAAAAAAAAAoD8AwDgfQAA")]
        [TestCase(777UL, 11975586444328763281UL, "PUTT-BAkDAAAAAAAAAsD9AwBgfQMA")]
        public void GoldenFixtureV4_FinalHashAndCode_AreStable(
            ulong seed, ulong expectedHash, string expectedCode)
        {
            var (hash, code) = RunFixture(seed, GeneratorConfig.V4, SolverConfig.V4, 4);
            Assert.That(hash, Is.EqualTo(expectedHash),
                $"seed {seed}: actual hash {hash}UL, code \"{code}\"");
            Assert.That(code, Is.EqualTo(expectedCode));
        }
    }
}
