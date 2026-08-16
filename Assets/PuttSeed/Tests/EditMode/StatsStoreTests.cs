using System.IO;
using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    public class StatsStoreTests
    {
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Path.GetTempPath(), $"puttseed-stats-{System.Guid.NewGuid():N}.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }

        [Test]
        public void FreshStore_StartsEmpty()
        {
            var store = new StatsStore(_path);
            Assert.That(store.Data.streak, Is.EqualTo(0));
            Assert.That(store.Data.days, Is.Empty);
            Assert.That(store.Data.practicePlayed, Is.EqualTo(0));
        }

        [Test]
        public void Attempts_PersistAcrossReload()
        {
            var store = new StatsStore(_path);
            store.RecordDailyAttempt(100);
            store.RecordDailyAttempt(100);

            var reloaded = new StatsStore(_path);
            Assert.That(reloaded.GetOrCreateDay(100).attempts, Is.EqualTo(2));
        }

        [Test]
        public void Completion_KeepsBestStrokesStarsAndReplay()
        {
            var store = new StatsStore(_path);
            store.RecordDailyCompletion(100, 4, 1, "PUTT-worse");
            store.RecordDailyCompletion(100, 2, 3, "PUTT-better");
            store.RecordDailyCompletion(100, 3, 2, "PUTT-mediocre");

            var record = store.GetOrCreateDay(100);
            Assert.That(record.bestStrokes, Is.EqualTo(2));
            Assert.That(record.bestStars, Is.EqualTo(3));
            Assert.That(record.bestReplay, Is.EqualTo("PUTT-better"));
            Assert.That(record.completed, Is.True);
        }

        [Test]
        public void Streak_IncrementsOnConsecutiveDays()
        {
            var store = new StatsStore(_path);
            store.RecordDailyCompletion(100, 3, 2, "a");
            store.RecordDailyCompletion(101, 3, 2, "b");
            store.RecordDailyCompletion(102, 3, 2, "c");
            Assert.That(store.Data.streak, Is.EqualTo(3));
        }

        [Test]
        public void Streak_ResetsAfterGap()
        {
            var store = new StatsStore(_path);
            store.RecordDailyCompletion(100, 3, 2, "a");
            store.RecordDailyCompletion(101, 3, 2, "b");
            store.RecordDailyCompletion(105, 3, 2, "c"); // missed 102-104
            Assert.That(store.Data.streak, Is.EqualTo(1));
        }

        [Test]
        public void RepeatCompletionSameDay_DoesNotDoubleStreak()
        {
            var store = new StatsStore(_path);
            store.RecordDailyCompletion(100, 3, 2, "a");
            store.RecordDailyCompletion(100, 2, 3, "a2");
            Assert.That(store.Data.streak, Is.EqualTo(1));
        }

        [Test]
        public void FirstEverCompletion_StartsStreakAtOne()
        {
            var store = new StatsStore(_path);
            store.RecordDailyCompletion(1, 3, 2, "a"); // day 1 with lastCompletedDay 0
            Assert.That(store.Data.streak, Is.EqualTo(1));
        }

        [Test]
        public void CorruptFile_StartsFresh()
        {
            File.WriteAllText(_path, "{not json!!");
            var store = new StatsStore(_path);
            Assert.That(store.Data.days, Is.Empty);
            store.RecordDailyAttempt(50); // and can still save
            Assert.That(new StatsStore(_path).GetOrCreateDay(50).attempts, Is.EqualTo(1));
        }

        [Test]
        public void TutorialSeen_Persists()
        {
            var store = new StatsStore(_path);
            Assert.That(store.Data.tutorialSeen, Is.False);
            store.MarkTutorialSeen();
            Assert.That(new StatsStore(_path).Data.tutorialSeen, Is.True);
        }

        [Test]
        public void SoundAndHaptics_DefaultOn_TogglesPersist()
        {
            var store = new StatsStore(_path);
            Assert.That(store.Data.soundEnabled, Is.True);
            Assert.That(store.Data.hapticsEnabled, Is.True);

            store.SetSoundEnabled(false);
            store.SetHapticsEnabled(false);

            var reloaded = new StatsStore(_path);
            Assert.That(reloaded.Data.soundEnabled, Is.False);
            Assert.That(reloaded.Data.hapticsEnabled, Is.False);
        }

        [Test]
        public void LegacySave_WithoutSettingsFields_DefaultsToOn()
        {
            File.WriteAllText(_path, "{\"streak\":2,\"lastCompletedDay\":10}");
            var store = new StatsStore(_path);
            Assert.That(store.Data.soundEnabled, Is.True);
            Assert.That(store.Data.hapticsEnabled, Is.True);
            Assert.That(store.Data.streak, Is.EqualTo(2));
        }

        [Test]
        public void PracticeCounter_Persists()
        {
            var store = new StatsStore(_path);
            store.RecordPracticePlayed();
            store.RecordPracticePlayed();
            Assert.That(new StatsStore(_path).Data.practicePlayed, Is.EqualTo(2));
        }
    }
}
