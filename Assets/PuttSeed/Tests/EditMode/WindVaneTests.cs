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

        /// <summary>The three strengths DailyMutators draws a day's wind from.</summary>
        private static readonly float[] Strengths = { 0.35f, 0.65f, 1f };

        [Test]
        public void SpeedLabel_ReadsInTheLanguagesOwnUnit()
        {
            var wind = new Vector2(0.65f, 0f); // the middle of the three

            Loc.Apply("en");
            Assert.That(WindVane.SpeedLabel(wind), Is.EqualTo("13 mph"));

            Loc.Apply("tr");
            Assert.That(WindVane.SpeedLabel(wind), Is.EqualTo("21 km/s"));
        }

        [Test]
        public void EveryStrength_ReadsAsItsOwnWind()
        {
            // Three strengths that shared a barb count or a speed would be one
            // strength as far as the player is concerned.
            Loc.Apply("en");
            var barbs = new System.Collections.Generic.List<int>();
            var labels = new System.Collections.Generic.List<string>();
            foreach (float strength in Strengths)
            {
                var wind = new Vector2(strength, 0f);
                barbs.Add(WindVane.Barbs(wind));
                labels.Add(WindVane.SpeedLabel(wind));
            }

            Assert.That(barbs, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(labels, Is.Unique);
        }

        [Test]
        public void Barbs_NeverExceedTheVanesThree()
        {
            Assert.That(WindVane.Barbs(new Vector2(9f, 0f)), Is.EqualTo(3));
            Assert.That(WindVane.Barbs(new Vector2(0.01f, 0f)), Is.EqualTo(1));
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
