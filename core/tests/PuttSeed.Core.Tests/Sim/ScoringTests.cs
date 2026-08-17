using NUnit.Framework;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public sealed class ScoringTests
    {
        [TestCase(1, 3, 3, Description = "well under par")]
        [TestCase(2, 3, 3, Description = "one under par")]
        [TestCase(3, 3, 3, Description = "exactly par — the standard of good play")]
        [TestCase(4, 3, 2, Description = "one over par")]
        [TestCase(5, 3, 1, Description = "two over, within limit")]
        [TestCase(6, 3, 1, Description = "at the stroke limit (par + 3)")]
        public void Stars_FollowGddBuckets(int strokes, int par, int expected)
        {
            Assert.That(Scoring.Stars(strokes, par), Is.EqualTo(expected));
        }

        /// <summary>
        /// The case that drove the 2026-08-18 recalibration: generation only
        /// ever certifies par 2, so under-par means an ace. Par itself has to
        /// carry the top tier or the third star is unreachable by design.
        /// </summary>
        [TestCase(1, 2, 3, Description = "ace on a par 2")]
        [TestCase(2, 2, 3, Description = "par on a par 2 — three stars")]
        [TestCase(3, 2, 2, Description = "one over")]
        [TestCase(4, 2, 1, Description = "two over")]
        [TestCase(5, 2, 1, Description = "at the limit")]
        public void Stars_OnTheParTwoCoursesGenerationActuallyMakes(int strokes, int par, int expected)
        {
            Assert.That(Scoring.Stars(strokes, par), Is.EqualTo(expected));
        }

        [Test]
        public void Stars_NeverIncreaseWithMoreStrokes()
        {
            for (int par = 2; par <= 5; par++)
            {
                for (int strokes = 1; strokes < par + 3; strokes++)
                {
                    Assert.That(Scoring.Stars(strokes + 1, par),
                        Is.LessThanOrEqualTo(Scoring.Stars(strokes, par)),
                        $"par {par}, strokes {strokes} -> {strokes + 1}");
                }
            }
        }
    }
}
