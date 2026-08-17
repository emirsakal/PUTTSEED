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
        public void ArchiveCompletion_FillsRecordWithoutStreakCredit()
        {
            var store = new StatsStore(_path);
            store.RecordDailyCompletion(100, 3, 2, "today");
            store.RecordDailyCompletion(99, 2, 3, "old", countsForStreak: false);

            Assert.That(store.Data.streak, Is.EqualTo(1));
            Assert.That(store.Data.lastCompletedDay, Is.EqualTo(100));
            var archived = store.FindDay(99);
            Assert.That(archived, Is.Not.Null);
            Assert.That(archived.completed, Is.True);
            Assert.That(archived.bestStrokes, Is.EqualTo(2));
        }

        [Test]
        public void FindDay_NeverCreatesRecords()
        {
            var store = new StatsStore(_path);
            Assert.That(store.FindDay(123), Is.Null);
            Assert.That(store.Data.days, Is.Empty);
        }

        [Test]
        public void Unlock_OncePersistsAndReportsNewness()
        {
            var store = new StatsStore(_path);
            Assert.That(store.Unlock("ace"), Is.True);
            Assert.That(store.Unlock("ace"), Is.False);
            Assert.That(new StatsStore(_path).Data.achievements, Is.EquivalentTo(new[] { "ace" }));
        }

        [Test]
        public void BestStreak_KeepsThePeakThroughAReset()
        {
            var store = new StatsStore(_path);
            store.RecordDailyCompletion(100, 3, 2, "a");
            store.RecordDailyCompletion(101, 3, 2, "b");
            store.RecordDailyCompletion(102, 3, 2, "c");
            store.RecordDailyCompletion(110, 3, 2, "d"); // gap resets the live streak

            Assert.That(store.Data.streak, Is.EqualTo(1));
            Assert.That(store.Data.bestStreak, Is.EqualTo(3));
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
        public void PracticeBest_KeepsTheLowestPerBucket()
        {
            var store = new StatsStore(_path);
            Assert.That(store.RecordPracticeBest(0, 4), Is.True, "first is a best");
            Assert.That(store.RecordPracticeBest(0, 5), Is.False, "worse never records");
            Assert.That(store.RecordPracticeBest(0, 3), Is.True, "better records");
            Assert.That(store.RecordPracticeBest(2, 6), Is.True, "buckets are independent");

            var reloaded = new StatsStore(_path);
            Assert.That(reloaded.Data.bestPracticeEasy, Is.EqualTo(3));
            Assert.That(reloaded.Data.bestPracticeNormal, Is.EqualTo(0));
            Assert.That(reloaded.Data.bestPracticeHard, Is.EqualTo(6));
        }

        [Test]
        public void HardMode_KeepsItsOwnMedalAndBest()
        {
            var store = new StatsStore(_path);
            store.RecordDailyCompletion(10, 4, 1, "PUTT-x");
            Assert.That(store.GetOrCreateDay(10).hardCompleted, Is.False,
                "a normal run never earns the hard medal");

            store.RecordDailyHardCompletion(10, 3);
            var record = store.GetOrCreateDay(10);
            Assert.That(record.hardCompleted, Is.True);
            Assert.That(record.hardBestStrokes, Is.EqualTo(3));
            Assert.That(record.bestStrokes, Is.EqualTo(4), "the normal best is untouched");

            store.RecordDailyHardCompletion(10, 2);
            Assert.That(store.GetOrCreateDay(10).hardBestStrokes, Is.EqualTo(2), "better is kept");
            store.RecordDailyHardCompletion(10, 5);
            Assert.That(store.GetOrCreateDay(10).hardBestStrokes, Is.EqualTo(2), "worse never downgrades");
        }

        [Test]
        public void HardMode_NeverTouchesTheStreak()
        {
            // The streak is the daily loop's spine; a second, harder run at
            // the same day must not extend or restart it.
            var store = new StatsStore(_path);
            store.RecordDailyCompletion(10, 3, 2, "PUTT-x");
            int streakAfterNormal = store.Data.streak;

            store.RecordDailyHardCompletion(11, 2); // a day never played normally
            Assert.That(store.Data.streak, Is.EqualTo(streakAfterNormal));
            Assert.That(store.Data.lastCompletedDay, Is.EqualTo(10));
        }

        [Test]
        public void HardMode_SurvivesAReload()
        {
            var store = new StatsStore(_path);
            store.RecordDailyHardCompletion(7, 3);
            var reloaded = new StatsStore(_path);
            Assert.That(reloaded.GetOrCreateDay(7).hardCompleted, Is.True);
            Assert.That(reloaded.GetOrCreateDay(7).hardBestStrokes, Is.EqualTo(3));
        }

        [Test]
        public void Journey_ProgressUnlocksAndKeepsBestStars()
        {
            var store = new StatsStore(_path);
            Assert.That(store.UnlockedJourneyLevels(50), Is.EqualTo(1), "only level 1 at first");

            Assert.That(store.RecordJourneyResult(0, 2), Is.True);
            Assert.That(store.UnlockedJourneyLevels(50), Is.EqualTo(2), "completing 1 unlocks 2");

            Assert.That(store.RecordJourneyResult(0, 1), Is.False, "worse stars never downgrade");
            Assert.That(store.RecordJourneyResult(0, 3), Is.True, "better stars record");
            Assert.That(store.TotalJourneyStars(), Is.EqualTo(3));

            var reloaded = new StatsStore(_path);
            Assert.That(reloaded.Data.journeyStars[0], Is.EqualTo(3));
        }

        [Test]
        public void Journey_UnlockCapsAtTotalLevels()
        {
            var store = new StatsStore(_path);
            for (int level = 0; level < 50; level++)
            {
                store.RecordJourneyResult(level, 1);
            }

            Assert.That(store.UnlockedJourneyLevels(50), Is.EqualTo(50));
        }

        [Test]
        public void ColorblindAndBattery_Persist()
        {
            var store = new StatsStore(_path);
            Assert.That(store.Data.colorblindMode, Is.False);
            Assert.That(store.Data.batterySaver, Is.False);
            store.SetColorblindMode(true);
            store.SetBatterySaver(true);

            var reloaded = new StatsStore(_path);
            Assert.That(reloaded.Data.colorblindMode, Is.True);
            Assert.That(reloaded.Data.batterySaver, Is.True);
        }

        [Test]
        public void AimAndSkin_Persist()
        {
            var store = new StatsStore(_path);
            Assert.That(store.Data.aimDirect, Is.False);
            Assert.That(store.Data.ballSkin, Is.EqualTo("cream"));
            store.SetAimDirect(true);
            store.SetBallSkin("rose");

            var reloaded = new StatsStore(_path);
            Assert.That(reloaded.Data.aimDirect, Is.True);
            Assert.That(reloaded.Data.ballSkin, Is.EqualTo("rose"));
        }

        [Test]
        public void ReplaceData_OverwritesAndPersists()
        {
            var store = new StatsStore(_path);
            store.RecordDailyCompletion(100, 3, 2, "a");
            store.ReplaceData(new SaveData { streak = 7 });

            var reloaded = new StatsStore(_path);
            Assert.That(reloaded.Data.streak, Is.EqualTo(7));
            Assert.That(reloaded.Data.days, Is.Empty);
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
