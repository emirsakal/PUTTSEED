using NUnit.Framework;
using PuttSeed.Core.CourseGen;

namespace PuttSeed.Core.Tests.CourseGen
{
    [TestFixture]
    public class DifficultyRaterTests
    {
        /// <summary>
        /// Pins the 2026-08-19 threshold recalibration (corridors grew long
        /// enough to hold a par 3, which lifted every score by about two):
        /// score = tightness + turns + 2*hazards, Easy ≤ 18 &lt; Normal ≤ 22
        /// &lt; Hard. A silent cut change re-shuffles every practice bucket —
        /// and the cup's own capture rule follows the rating — so the exact
        /// boundaries are asserted.
        /// </summary>
        [TestCase(6, 6, Difficulty.Easy, Description = "score 18 — top of Easy")]
        [TestCase(7, 6, Difficulty.Normal, Description = "score 19 — bottom of Normal")]
        [TestCase(10, 6, Difficulty.Normal, Description = "score 22 — top of Normal")]
        [TestCase(11, 6, Difficulty.Hard, Description = "score 23 — bottom of Hard")]
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
            // 1/32 capture ratio, 7 turns, 6 hazards: 2 + 7 + 12 = 21 <= 22.
            Assert.That(DifficultyRater.Rate(16, 512, 7, 6), Is.EqualTo(Difficulty.Normal));
        }

        [Test]
        public void TightWindingHazardousCourse_IsHard()
        {
            // 1/128 capture ratio, 9 turns, 7 hazards: 4 + 9 + 14 = 27 > 22.
            Assert.That(DifficultyRater.Rate(4, 512, 9, 7), Is.EqualTo(Difficulty.Hard));
        }

        /// <summary>
        /// A longer hole is not a harder hole. The same course rated as a par 3
        /// must come out no harder than it would as a par 2, or "Easy" silently
        /// means "short" and choosing Easy in practice takes the par variety
        /// back out of the mode built to show it off.
        /// </summary>
        [Test]
        public void TheSameCourse_IsNeverHarderForBeingLonger()
        {
            for (int turns = 4; turns < 10; turns++)
            {
                for (int hazards = 0; hazards < 7; hazards++)
                {
                    var asPar2 = DifficultyRater.Rate(16, 512, turns, hazards, par: 2);
                    var asPar3 = DifficultyRater.Rate(16, 512, turns, hazards, par: 3);
                    Assert.That((int)asPar3, Is.LessThanOrEqualTo((int)asPar2),
                        $"turns {turns}, hazards {hazards}");
                }
            }
        }

        /// <summary>The allowance is real: some course must actually cross a bucket.</summary>
        [Test]
        public void TheParAllowance_MovesAtLeastOneBucket()
        {
            // Score 19: Normal as a par 2, Easy as a par 3 (19 - 2 = 17).
            Assert.That(DifficultyRater.Rate(16, 16, 7, 6, par: 2), Is.EqualTo(Difficulty.Normal));
            Assert.That(DifficultyRater.Rate(16, 16, 7, 6, par: 3), Is.EqualTo(Difficulty.Easy));
        }

        [Test]
        public void Score_IsTheRatingsRawInput()
        {
            // tightness 2 (1/32 capture) + 6 turns + 2*4 hazards.
            Assert.That(DifficultyRater.Score(16, 512, 6, 4), Is.EqualTo(16));
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
