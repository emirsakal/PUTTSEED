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

        [Test]
        public void EveryOfferedButtonHasSomewhereToGo()
        {
            // The bug this forecloses: the label and the action were written
            // out separately — the rules here, the calls in a click handler —
            // so a mode could be offered a button that did nothing when
            // pressed, or be sent somewhere with no button to send it. The
            // two now come from one decision, and this walks every state the
            // decision can be in to prove they cannot drift apart.
            foreach (GameMode mode in System.Enum.GetValues(typeof(GameMode)))
            {
                for (int bits = 0; bits < 32; bits++)
                {
                    bool holed = (bits & 1) != 0;
                    bool failed = (bits & 2) != 0;
                    bool tutorial = (bits & 4) != 0;
                    bool journey = (bits & 8) != 0;
                    bool gauntlet = (bits & 16) != 0;

                    var state = AdvanceButton.For(mode, holed, failed, tutorial, journey, gauntlet);
                    var to = AdvanceButton.DestinationFor(
                        mode, holed, failed, tutorial, journey, gauntlet);

                    Assert.That(state.Visible,
                        Is.EqualTo(to != AdvanceButton.Destination.None),
                        $"{mode} holed:{holed} failed:{failed} tutorial:{tutorial} " +
                        $"journey:{journey} gauntlet:{gauntlet} — button says " +
                        $"visible:{state.Visible} but the destination is {to}");

                    Assert.That(state.Label.Length > 0,
                        Is.EqualTo(to != AdvanceButton.Destination.None),
                        $"{mode}: a destination needs a label and a label needs a destination");
                }
            }
        }

        [Test]
        public void FinishingTheTutorialIsTheOnlyDestinationThatLeavesTheScene()
        {
            // GameUI routes FinishTutorial through the scene change and every
            // other destination through the in-scene sweep. If a second
            // destination ever means "leave the scene", that branch has to
            // learn about it — this is the tripwire.
            Assert.That(
                AdvanceButton.DestinationFor(GameMode.Tutorial, false, false, false, true, true),
                Is.EqualTo(AdvanceButton.Destination.FinishTutorial));

            foreach (GameMode mode in System.Enum.GetValues(typeof(GameMode)))
            {
                if (mode == GameMode.Tutorial)
                {
                    continue;
                }

                Assert.That(
                    AdvanceButton.DestinationFor(mode, true, true, true, true, true),
                    Is.Not.EqualTo(AdvanceButton.Destination.FinishTutorial),
                    $"{mode} must not end the tutorial");
            }
        }
    }
}
