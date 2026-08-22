using NUnit.Framework;
using PuttSeed.Unity;
using UnityEngine;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// A gauntlet week used to live only in memory: Menu or the back button
    /// mid-week threw away every banked hole without a word. The progress now
    /// rides the save, and these hold the two things that make that safe —
    /// it survives a round trip, and finishing clears it so a new week never
    /// inherits an old one.
    /// </summary>
    public class GauntletProgressTests
    {
        [Test]
        public void AFreshSave_HasNoWeekInProgress()
        {
            var data = new SaveData();
            Assert.That(data.gauntletProgressWeek, Is.EqualTo(-1));
            Assert.That(data.gauntletProgressHole, Is.EqualTo(0));
            Assert.That(data.gauntletProgressCode, Is.Empty);
        }

        [Test]
        public void ProgressSurvivesTheSaveFile()
        {
            var data = new SaveData
            {
                gauntletProgressWeek = 341,
                gauntletProgressHole = 4,
                gauntletProgressStrokes = 11,
                gauntletProgressCode = "GAUNT-EXAMPLE",
            };
            var loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));

            Assert.That(loaded.gauntletProgressWeek, Is.EqualTo(341));
            Assert.That(loaded.gauntletProgressHole, Is.EqualTo(4));
            Assert.That(loaded.gauntletProgressStrokes, Is.EqualTo(11));
            Assert.That(loaded.gauntletProgressCode, Is.EqualTo("GAUNT-EXAMPLE"));
        }

        [Test]
        public void ClearingForgetsEverything()
        {
            string path = System.IO.Path.Combine(Application.temporaryCachePath, "gauntlet-progress-test.json");
            System.IO.File.Delete(path);
            var store = new StatsStore(path);
            store.SaveGauntletProgress(341, 4, 11, "GAUNT-EXAMPLE");
            Assert.That(new StatsStore(path).Data.gauntletProgressHole, Is.EqualTo(4), "saved to disk");

            store.ClearGauntletProgress();
            var reloaded = new StatsStore(path).Data;
            Assert.That(reloaded.gauntletProgressWeek, Is.EqualTo(-1));
            Assert.That(reloaded.gauntletProgressHole, Is.EqualTo(0));
            Assert.That(reloaded.gauntletProgressCode, Is.Empty);
            System.IO.File.Delete(path);
        }
    }
}
