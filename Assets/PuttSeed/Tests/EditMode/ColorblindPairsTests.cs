using NUnit.Framework;
using PuttSeed.Unity;
using UnityEngine;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// The colorblind palette was shifting sand, water and bumpers apart while
    /// the aim's power ramp — green to red, the worst possible gauge for the
    /// player the mode exists for — ran untouched. These hold the two
    /// red-green pairs to the rule: in colorblind mode neither end may lean on
    /// the red-green axis alone.
    /// </summary>
    public class ColorblindPairsTests
    {
        [TearDown]
        public void TearDown() => PaletteMaterials.ColorblindMode = false;

        [Test]
        public void StandardRamp_IsGreenToRed()
        {
            PaletteMaterials.ColorblindMode = false;
            Assert.That(PaletteMaterials.PowerLow.g, Is.GreaterThan(PaletteMaterials.PowerLow.r));
            Assert.That(PaletteMaterials.PowerHigh.r, Is.GreaterThan(PaletteMaterials.PowerHigh.g));
        }

        [Test]
        public void ColorblindRamp_SeparatesOnBlue_NotOnRedGreen()
        {
            PaletteMaterials.ColorblindMode = true;
            Assert.That(PaletteMaterials.PowerLow.b, Is.GreaterThan(PaletteMaterials.PowerLow.r),
                "the cool end must lean blue, not green");
            Assert.That(PaletteMaterials.PowerHigh.r, Is.GreaterThan(PaletteMaterials.PowerHigh.b),
                "the hot end must lean orange, not red-against-green");
            // The two ends differ in BLUE by a margin deutan vision keeps.
            Assert.That(PaletteMaterials.PowerLow.b - PaletteMaterials.PowerHigh.b, Is.GreaterThan(0.5f));
        }

        [Test]
        public void DifficultyLabels_FollowTheSameRule()
        {
            PaletteMaterials.ColorblindMode = true;
            Assert.That(PaletteMaterials.DifficultyEasy.b, Is.GreaterThan(PaletteMaterials.DifficultyEasy.r));
            Assert.That(PaletteMaterials.DifficultyHard.r, Is.GreaterThan(PaletteMaterials.DifficultyHard.b));
            Assert.That(PaletteMaterials.DifficultyEasy.b - PaletteMaterials.DifficultyHard.b,
                Is.GreaterThan(0.5f));
        }
    }
}
