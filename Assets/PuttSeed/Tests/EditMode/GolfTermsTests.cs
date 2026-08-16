using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    public class GolfTermsTests
    {
        [TestCase(1, 2, "Ace!", Description = "hole in one trumps everything")]
        [TestCase(1, 5, "Ace!")]
        [TestCase(2, 4, "Eagle!")]
        [TestCase(2, 5, "Eagle!", Description = "three under is still an eagle line")]
        [TestCase(2, 3, "Birdie!")]
        [TestCase(3, 3, "Par — well played!")]
        [TestCase(4, 3, "Bogey — holed!")]
        [TestCase(5, 3, "Holed!", Description = "double bogey and beyond")]
        public void SuccessLine_UsesGolfVocabulary(int strokes, int par, string expected)
        {
            Assert.That(GolfTerms.SuccessLine(strokes, par), Is.EqualTo(expected));
        }
    }
}
