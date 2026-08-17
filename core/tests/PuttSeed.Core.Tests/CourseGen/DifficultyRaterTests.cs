using NUnit.Framework;
using PuttSeed.Core.CourseGen;

namespace PuttSeed.Core.Tests.CourseGen
{
    [TestFixture]
    public class DifficultyRaterTests
    {
        /// <summary>
        /// Pins the 2026-08-18 threshold recalibration (the v2 element wave
        /// joined the hazard pool): score = tightness + turns + 2*hazards,
        /// Easy ≤ 16 &lt; Normal ≤ 20 &lt; Hard. A silent cut change
        /// re-shuffles every practice bucket, so the exact boundaries are
        /// asserted.
        /// </summary>
        [TestCase(4, 6, Difficulty.Easy, Description = "score 16 — top of Easy")]
        [TestCase(5, 6, Difficulty.Normal, Description = "score 17 — bottom of Normal")]
        [TestCase(8, 6, Difficulty.Normal, Description = "score 20 — top of Normal")]
        [TestCase(9, 6, Difficulty.Hard, Description = "score 21 — bottom of Hard")]
        public void BucketBoundaries_ArePinned(int turns, int hazards, Difficulty expected)
        {
            // captureShots == sampledShots → tightness 0; score is turns + 2*hazards.
            Assert.That(DifficultyRater.Rate(16, 16, turns, hazards), Is.EqualTo(expected));
        }

        [Test]
        public void ForgivingCourse_IsEasy()
        {
            // Loose capture (1/8), 4 turns, 2 hazards: 0 + 4 + 4 = 8 <= 16.
            Assert.That(DifficultyRater.Rate(64, 512, 4, 2), Is.EqualTo(Difficulty.Easy));
        }

        [Test]
        public void MidCourse_IsNormal()
        {
            // 1/32 capture ratio, 6 turns, 5 hazards: 2 + 6 + 10 = 18 <= 20.
            Assert.That(DifficultyRater.Rate(16, 512, 6, 5), Is.EqualTo(Difficulty.Normal));
        }

        [Test]
        public void TightWindingHazardousCourse_IsHard()
        {
            // 1/128 capture ratio, 7 turns, 6 hazards: 4 + 7 + 12 = 23 > 20.
            Assert.That(DifficultyRater.Rate(4, 512, 7, 6), Is.EqualTo(Difficulty.Hard));
        }

        [Test]
        public void MoreHazards_NeverEasier()
        {
            for (int hazards = 0; hazards < 6; hazards++)
            {
                var lighter = DifficultyRater.Rate(32, 512, 4, hazards);
                var heavier = DifficultyRater.Rate(32, 512, 4, hazards + 1);
                Assert.That((int)heavier, Is.GreaterThanOrEqualTo((int)lighter));
            }
        }

        [Test]
        public void MoreTurns_NeverEasier()
        {
            for (int turns = 3; turns < 8; turns++)
            {
                var lighter = DifficultyRater.Rate(32, 512, turns, 1);
                var heavier = DifficultyRater.Rate(32, 512, turns + 1, 1);
                Assert.That((int)heavier, Is.GreaterThanOrEqualTo((int)lighter));
            }
        }

        [Test]
        public void TighterSolution_NeverEasier()
        {
            var loose = DifficultyRater.Rate(64, 512, 5, 1);
            var mid = DifficultyRater.Rate(16, 512, 5, 1);
            var tight = DifficultyRater.Rate(2, 512, 5, 1);
            Assert.That((int)mid, Is.GreaterThanOrEqualTo((int)loose));
            Assert.That((int)tight, Is.GreaterThanOrEqualTo((int)mid));
        }
    }
}
