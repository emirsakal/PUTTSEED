using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    public class AchievementsTests
    {
        private static SaveData Fresh() => new SaveData();

        [Test]
        public void FirstHole_AlwaysEarnedOnce()
        {
            var data = Fresh();
            var earned = Achievements.EvaluateRun(data, GameMode.Practice, false, 4, 3, 2);
            Assert.That(earned, Does.Contain("first_hole"));

            data.achievements.Add("first_hole");
            earned = Achievements.EvaluateRun(data, GameMode.Practice, false, 4, 3, 2);
            Assert.That(earned, Does.Not.Contain("first_hole"));
        }

        [Test]
        public void Ace_RequiresSingleStroke()
        {
            Assert.That(Achievements.EvaluateRun(Fresh(), GameMode.Daily, false, 1, 2, 1),
                Does.Contain("ace"));
            Assert.That(Achievements.EvaluateRun(Fresh(), GameMode.Daily, false, 2, 3, 1),
                Does.Not.Contain("ace"));
        }

        [Test]
        public void CleanStrike_RequiresZeroWallHits()
        {
            Assert.That(Achievements.EvaluateRun(Fresh(), GameMode.Daily, false, 2, 2, 0),
                Does.Contain("no_walls"));
            Assert.That(Achievements.EvaluateRun(Fresh(), GameMode.Daily, false, 2, 2, 1),
                Does.Not.Contain("no_walls"));
        }

        [Test]
        public void UnderPar_OnlyOnDailies()
        {
            Assert.That(Achievements.EvaluateRun(Fresh(), GameMode.Daily, false, 2, 3, 1),
                Does.Contain("three_star"));
            Assert.That(Achievements.EvaluateRun(Fresh(), GameMode.Practice, false, 2, 3, 1),
                Does.Not.Contain("three_star"));
        }

        [Test]
        public void TimeTraveler_OnlyOnArchiveDailies()
        {
            Assert.That(Achievements.EvaluateRun(Fresh(), GameMode.Daily, true, 3, 3, 1),
                Does.Contain("archive1"));
            Assert.That(Achievements.EvaluateRun(Fresh(), GameMode.Daily, false, 3, 3, 1),
                Does.Not.Contain("archive1"));
        }

        [Test]
        public void SevenDays_ReadsTheRecordedStreak()
        {
            var data = Fresh();
            data.streak = 7;
            Assert.That(Achievements.EvaluateRun(data, GameMode.Daily, false, 3, 3, 1),
                Does.Contain("streak7"));
        }

        [Test]
        public void Regular_CountsCompletedDaysOnly()
        {
            var data = Fresh();
            for (int day = 1; day <= 10; day++)
            {
                data.days.Add(new DayRecord { day = day, completed = true });
            }

            data.days.Add(new DayRecord { day = 11, completed = false });
            Assert.That(Achievements.CompletedDailyCount(data), Is.EqualTo(10));
            Assert.That(Achievements.EvaluateRun(data, GameMode.Daily, false, 3, 3, 1),
                Does.Contain("dailies10"));
        }

        [Test]
        public void EveryCatalogId_ResolvesViaFind()
        {
            foreach (var def in Achievements.All)
            {
                Assert.That(Achievements.Find(def.Id), Is.SameAs(def));
            }

            Assert.That(Achievements.Find("nope"), Is.Null);
        }
    }
}
