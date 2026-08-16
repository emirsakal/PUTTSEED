using NUnit.Framework;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public sealed class ScoringTests
    {
        [TestCase(1, 3, 3, Description = "well under par")]
        [TestCase(2, 3, 3, Description = "one under par")]
        [TestCase(3, 3, 2, Description = "exactly par")]
        [TestCase(4, 3, 1, Description = "over par, within limit")]
        [TestCase(6, 3, 1, Description = "at the stroke limit (par + 3)")]
        public void Stars_FollowGddBuckets(int strokes, int par, int expected)
        {
            Assert.That(Scoring.Stars(strokes, par), Is.EqualTo(expected));
        }

        [Test]
        public void Stars_MinimumParTwoHoleInOne_IsThreeStars()
        {
            Assert.That(Scoring.Stars(1, 2), Is.EqualTo(3));
        }
    }
}
