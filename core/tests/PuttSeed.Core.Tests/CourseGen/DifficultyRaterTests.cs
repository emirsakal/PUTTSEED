using NUnit.Framework;
using PuttSeed.Core.CourseGen;

namespace PuttSeed.Core.Tests.CourseGen
{
    [TestFixture]
    public class DifficultyRaterTests
    {
        [Test]
        public void ForgivingCourse_IsEasy()
        {
            // Loose capture (1/8), 4 turns, 2 hazards: 0 + 4 + 4 = 8 <= 12.
            Assert.That(DifficultyRater.Rate(64, 512, 4, 2), Is.EqualTo(Difficulty.Easy));
        }

        [Test]
        public void MidCourse_IsNormal()
        {
            // 1/32 capture ratio, 6 turns, 3 hazards: 2 + 6 + 6 = 14 <= 16.
            Assert.That(DifficultyRater.Rate(16, 512, 6, 3), Is.EqualTo(Difficulty.Normal));
        }

        [Test]
        public void TightWindingHazardousCourse_IsHard()
        {
            // 1/128 capture ratio, 7 turns, 5 hazards: 4 + 7 + 10 = 21 > 16.
            Assert.That(DifficultyRater.Rate(4, 512, 7, 5), Is.EqualTo(Difficulty.Hard));
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
