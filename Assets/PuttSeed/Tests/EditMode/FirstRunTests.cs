using System.Collections.Generic;
using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// Who gets asked the opening questions. The interesting case is not the
    /// new player — it is the one who has been playing for weeks and installs
    /// an update: their save has never set the flag, because the flag did not
    /// exist, and asking them to pick a language they chose long ago would be
    /// the software equivalent of asking a regular their name.
    /// </summary>
    public class FirstRunTests
    {
        [Test]
        public void ABrandNewSaveIsAsked()
        {
            Assert.That(FirstRun.NeedsSetup(new SaveData()), Is.True);
        }

        [Test]
        public void OnceAnsweredItNeverAsksAgain()
        {
            Assert.That(FirstRun.NeedsSetup(new SaveData { setupSeen = true }), Is.False);
        }

        [Test]
        public void AnExistingPlayerIsNotInterrogatedAfterAnUpdate()
        {
            // Every one of these says "this person has played", and any one of
            // them is enough.
            Assert.That(FirstRun.NeedsSetup(new SaveData { tutorialSeen = true }), Is.False);
            Assert.That(FirstRun.NeedsSetup(new SaveData { practicePlayed = 3 }), Is.False);
            Assert.That(FirstRun.NeedsSetup(new SaveData
            {
                days = new List<DayRecord> { new DayRecord { day = 2400 } },
            }), Is.False);
            Assert.That(FirstRun.NeedsSetup(new SaveData
            {
                journeyStars = new List<int> { 3 },
            }), Is.False);
        }

        [Test]
        public void TheFlagSurvivesASaveRoundTrip()
        {
            var data = new SaveData { setupSeen = true };
            var loaded = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(data));
            Assert.That(loaded.setupSeen, Is.True);
            Assert.That(new SaveData().setupSeen, Is.False, "a fresh save must still be asked");
        }
    }
}
