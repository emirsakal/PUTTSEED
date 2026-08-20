using NUnit.Framework;
using PuttSeed.Unity;
using UnityEngine;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// The wind readout. Both units come off one reading scale, so they can
    /// never disagree about the same wind — a number in the top bar that says
    /// one thing in English and another in Turkish is worse than no number.
    /// </summary>
    public class WindVaneTests
    {
        [TearDown]
        public void TearDown() => Loc.Apply("en");

        [Test]
        public void SpeedLabel_ReadsInTheLanguagesOwnUnit()
        {
            var wind = new Vector2(0.65f, 0f); // the one strength the game blows

            Loc.Apply("en");
            Assert.That(WindVane.SpeedLabel(wind), Is.EqualTo("13 mph"));

            Loc.Apply("tr");
            Assert.That(WindVane.SpeedLabel(wind), Is.EqualTo("21 km/s"));
        }

        [Test]
        public void SpeedLabel_ReadsTheVectorNotTheAxis()
        {
            // A diagonal wind is the same strength as a straight one; the
            // label must come off the magnitude, not off x.
            Loc.Apply("en");
            var straight = new Vector2(0.65f, 0f);
            var diagonal = new Vector2(0.65f * 0.7071f, 0.65f * 0.7071f);
            Assert.That(WindVane.SpeedLabel(diagonal), Is.EqualTo(WindVane.SpeedLabel(straight)));
        }
    }
}
