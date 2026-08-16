using NUnit.Framework;
using PuttSeed.Core.CourseGen;

namespace PuttSeed.Core.Tests.CourseGen
{
    [TestFixture]
    public class DifficultyRaterTests
    {
        [Test]
        public void ForgivingShortCourse_IsEasy()
        {
            // 64 of 512 sampled shots capture, 3 turns, no hazards.
            Assert.That(DifficultyRater.Rate(64, 512, 3, 0), Is.EqualTo(Difficulty.Easy));
        }

        [Test]
        public void MidCourse_IsNormal()
        {
            // 1/32 capture ratio, 5 turns, 1 hazard.
            Assert.That(DifficultyRater.Rate(16, 512, 5, 1), Is.EqualTo(Difficulty.Normal));
        }

        [Test]
        public void TightWindingHazardousCourse_IsHard()
        {
            // 1/128 capture ratio, 7 turns, 3 hazards.
            Assert.That(DifficultyRater.Rate(4, 512, 7, 3), Is.EqualTo(Difficulty.Hard));
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
