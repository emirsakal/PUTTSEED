using NUnit.Framework;
using PuttSeed.Core.Daily;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Replay;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Replay
{
    [TestFixture]
    public class GauntletCodecTests
    {
        private static ShotInput[][] Shots(params int[] perHole)
        {
            var holes = new ShotInput[GauntletWeek.Length][];
            for (int h = 0; h < holes.Length; h++)
            {
                holes[h] = new ShotInput[perHole[h]];
                for (int i = 0; i < holes[h].Length; i++)
                {
                    holes[h][i] = new ShotInput((h * 97 + i * 13) % 1024, (h * 31 + i * 7) % 256);
                }
            }

            return holes;
        }

        private static int[][] Clocks(ShotInput[][] shots)
        {
            var clocks = new int[shots.Length][];
            for (int h = 0; h < shots.Length; h++)
            {
                clocks[h] = new int[shots[h].Length];
                for (int i = 0; i < clocks[h].Length; i++)
                {
                    clocks[h][i] = (h * 137 + i * 41) % 1024;
                }
            }

            return clocks;
        }

        [Test]
        public void RoundTrip_PreservesWeekShotsAndClocks()
        {
            var shots = Shots(2, 3, 1, 2, 4, 2, 3);
            var clocks = Clocks(shots);
            var code = GauntletCodec.Encode(312, shots, clocks);

            Assert.That(code, Does.StartWith("PUTTWK-"));
            Assert.That(GauntletCodec.TryDecode(code, out int week, out var outShots, out var outClocks),
                Is.True);
            Assert.That(week, Is.EqualTo(312));
            for (int h = 0; h < GauntletWeek.Length; h++)
            {
                Assert.That(outShots[h].Length, Is.EqualTo(shots[h].Length), $"hole {h} count");
                for (int i = 0; i < shots[h].Length; i++)
                {
                    Assert.That(outShots[h][i].AngleIndex, Is.EqualTo(shots[h][i].AngleIndex));
                    Assert.That(outShots[h][i].PowerIndex, Is.EqualTo(shots[h][i].PowerIndex));
                    Assert.That(outClocks[h][i], Is.EqualTo(clocks[h][i]));
                }
            }
        }

        [Test]
        public void RoundTrip_RandomRuns_500Cases()
        {
            var rng = new FixRng(90210UL);
            for (int caseNo = 0; caseNo < 500; caseNo++)
            {
                var shots = new ShotInput[GauntletWeek.Length][];
                var clocks = new int[GauntletWeek.Length][];
                for (int h = 0; h < GauntletWeek.Length; h++)
                {
                    shots[h] = new ShotInput[rng.NextInt(0, 6)];
                    clocks[h] = new int[shots[h].Length];
                    for (int i = 0; i < shots[h].Length; i++)
                    {
                        shots[h][i] = new ShotInput(rng.NextInt(0, 1024), rng.NextInt(0, 256));
                        clocks[h][i] = rng.NextInt(0, 1024);
                    }
                }

                int week = rng.NextInt(0, 100000);
                var code = GauntletCodec.Encode(week, shots, clocks);
                Assert.That(GauntletCodec.TryDecode(code, out int week2, out var s2, out var c2),
                    Is.True, $"case {caseNo}");
                Assert.That(week2, Is.EqualTo(week), $"case {caseNo}");
                for (int h = 0; h < GauntletWeek.Length; h++)
                {
                    Assert.That(s2[h].Length, Is.EqualTo(shots[h].Length));
                    for (int i = 0; i < shots[h].Length; i++)
                    {
                        Assert.That(s2[h][i].AngleIndex, Is.EqualTo(shots[h][i].AngleIndex));
                        Assert.That(s2[h][i].PowerIndex, Is.EqualTo(shots[h][i].PowerIndex));
                        Assert.That(c2[h][i], Is.EqualTo(clocks[h][i]));
                    }
                }
            }
        }

        [Test]
        public void RoundTrip_AnUnplayedGauntlet()
        {
            var shots = Shots(0, 0, 0, 0, 0, 0, 0);
            var code = GauntletCodec.Encode(7, shots, Clocks(shots));
            Assert.That(GauntletCodec.TryDecode(code, out int week, out var outShots, out _), Is.True);
            Assert.That(week, Is.EqualTo(7));
            foreach (var hole in outShots)
            {
                Assert.That(hole, Is.Empty);
            }
        }

        [Test]
        public void Encode_RejectsAWrongHoleCount()
        {
            var six = new ShotInput[6][];
            var clocks = new int[6][];
            for (int h = 0; h < 6; h++)
            {
                six[h] = System.Array.Empty<ShotInput>();
                clocks[h] = System.Array.Empty<int>();
            }

            Assert.Throws<System.ArgumentException>(() => GauntletCodec.Encode(1, six, clocks));
        }

        [Test]
        public void Encode_RejectsClocksThatDoNotMatchTheShots()
        {
            var shots = Shots(2, 2, 2, 2, 2, 2, 2);
            var clocks = Clocks(shots);
            clocks[3] = new[] { 1 }; // one clock for two shots
            Assert.Throws<System.ArgumentException>(() => GauntletCodec.Encode(1, shots, clocks));
        }

        [Test]
        public void TryDecode_RejectsGarbageAndForeignCodes()
        {
            Assert.That(GauntletCodec.TryDecode("", out _, out _, out _), Is.False);
            Assert.That(GauntletCodec.TryDecode("PUTTWK-", out _, out _, out _), Is.False);
            Assert.That(GauntletCodec.TryDecode("PUTTWK-!!!!", out _, out _, out _), Is.False);

            // A single-hole code must never decode as a gauntlet.
            var single = ReplayCodec.Encode(42UL, new[] { new ShotInput(1, 2) });
            Assert.That(GauntletCodec.TryDecode(single, out _, out _, out _), Is.False);
        }

        [Test]
        public void TryDecode_RejectsTruncatedAndPaddedPayloads()
        {
            var shots = Shots(2, 1, 3, 1, 2, 1, 1);
            var code = GauntletCodec.Encode(9, shots, Clocks(shots));
            for (int keep = 7; keep < code.Length - 1; keep++)
            {
                Assert.That(GauntletCodec.TryDecode(code.Substring(0, keep), out _, out _, out _),
                    Is.False, $"decoded a truncated code of length {keep}");
            }

            Assert.That(GauntletCodec.TryDecode(code + "AAAA", out _, out _, out _), Is.False,
                "trailing bytes are corruption, not a longer gauntlet");
        }

        [Test]
        public void ACode_StaysPasteable()
        {
            // Seven holes at par-ish length: the whole week must still fit in
            // something a person can paste into a chat window.
            var shots = Shots(2, 2, 2, 2, 2, 2, 2);
            var code = GauntletCodec.Encode(312, shots, Clocks(shots));
            Assert.That(code.Length, Is.LessThan(120), $"({code.Length}) {code}");
        }
    }
}
