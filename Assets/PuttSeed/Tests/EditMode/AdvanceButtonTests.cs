using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// One button, five modes, and rules that used to live inside a per-frame
    /// refresh where nothing could be checked.
    /// </summary>
    public class AdvanceButtonTests
    {
        private static AdvanceButton.State For(GameMode mode, bool holed = false, bool failed = false,
            bool tutorial = true, bool journey = true, bool gauntlet = true)
            => AdvanceButton.For(mode, holed, failed, tutorial, journey, gauntlet);

        [Test]
        public void Practice_OffersANewCourseOnceTheRunIsOver()
        {
            // The change this file was written for: finishing a practice
            // course used to mean Menu, then Practice, to reach a course the
            // game had already grown in the background.
            Assert.That(For(GameMode.Practice, holed: true).Visible, Is.True);
            Assert.That(For(GameMode.Practice, holed: true).Label, Is.EqualTo("New course"));

            // Out of strokes is finished too.
            Assert.That(For(GameMode.Practice, failed: true).Visible, Is.True);
        }

        [Test]
        public void Practice_StaysQuietWhileTheBallIsStillRolling()
        {
            Assert.That(For(GameMode.Practice).Visible, Is.False);
        }

        [Test]
        public void Tutorial_AlwaysOffersTheWayOut()
        {
            // A lesson is not a challenge to pass: the next one is available
            // whether or not this ball ever drops.
            Assert.That(For(GameMode.Tutorial).Label, Is.EqualTo("Next lesson"));
            Assert.That(For(GameMode.Tutorial, tutorial: false).Label, Is.EqualTo("Finish tutorial"));
        }

        [Test]
        public void Journey_WaitsForTheBallToDrop()
        {
            Assert.That(For(GameMode.Journey).Visible, Is.False);
            Assert.That(For(GameMode.Journey, failed: true).Visible, Is.False, "a failed level is not passed");
            Assert.That(For(GameMode.Journey, holed: true).Label, Is.EqualTo("Next level"));
            Assert.That(For(GameMode.Journey, holed: true, journey: false).Visible, Is.False);
        }

        [Test]
        public void Gauntlet_AdvancesOnFailureToo()
        {
            Assert.That(For(GameMode.Gauntlet).Visible, Is.False);
            Assert.That(For(GameMode.Gauntlet, failed: true).Label, Is.EqualTo("Next hole"));
            Assert.That(For(GameMode.Gauntlet, holed: true).Label, Is.EqualTo("Next hole"));
            Assert.That(For(GameMode.Gauntlet, holed: true, gauntlet: false).Visible, Is.False);
        }

        [Test]
        public void Daily_HasNoAdvanceButtonAtAll()
        {
            // The day is done when it is done; the closing card carries it.
            Assert.That(For(GameMode.Daily, holed: true).Visible, Is.False);
        }
    }
}
