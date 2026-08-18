using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    public class AchievementsTests
    {
        private static SaveData Fresh() => new SaveData();

        /// <summary>
        /// A plain finished run: practice, holed in 4 on a par 3, two wall
        /// bounces along the way, nothing special. Named arguments override
        /// only the fact under test, so each case reads as its own rule.
        /// </summary>
        private static Achievements.RunFacts Run(
            GameMode mode = GameMode.Practice,
            bool isArchiveDay = false,
            bool isHardMode = false,
            int strokes = 4,
            int par = 3,
            int strokeLimit = 6,
            int wallHits = 2,
            int wallHitsFinalShot = 1,
            bool touchedHazard = true,
            bool hasWindmill = false,
            int windmillHits = 0)
            => new Achievements.RunFacts(mode, isArchiveDay, isHardMode, strokes, par, strokeLimit,
                wallHits, wallHitsFinalShot, touchedHazard, hasWindmill, windmillHits);

        [Test]
        public void FirstHole_AlwaysEarnedOnce()
        {
            var data = Fresh();
            Assert.That(Achievements.EvaluateRun(data, Run()), Does.Contain("first_hole"));

            data.achievements.Add("first_hole");
            Assert.That(Achievements.EvaluateRun(data, Run()), Does.Not.Contain("first_hole"));
        }

        [Test]
        public void Ace_RequiresSingleStroke()
        {
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(strokes: 1)), Does.Contain("ace"));
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(strokes: 2)), Does.Not.Contain("ace"));
        }

        [Test]
        public void CleanStrike_RequiresZeroWallHits()
        {
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(wallHits: 0, wallHitsFinalShot: 0)),
                Does.Contain("no_walls"));
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(wallHits: 1)),
                Does.Not.Contain("no_walls"));
        }

        [Test]
        public void BankShot_CountsTheHOLINGShot_NotTheWholeRun()
        {
            // Three walls spread over a run is ordinary; three on the shot
            // that drops is the trick shot worth a badge.
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(wallHits: 9, wallHitsFinalShot: 3)),
                Does.Contain("bank_shot"));
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(wallHits: 9, wallHitsFinalShot: 2)),
                Does.Not.Contain("bank_shot"));
        }

        [Test]
        public void Untouched_RequiresNoHazardContact()
        {
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(touchedHazard: false)),
                Does.Contain("untouched"));
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(touchedHazard: true)),
                Does.Not.Contain("untouched"));
        }

        [Test]
        public void Untouched_IsNotTheSameAsCleanStrike()
        {
            // Walls are the course, not a hazard: bouncing off them must not
            // cost the hazard-free badge, and vice versa.
            var wallyButClean = Achievements.EvaluateRun(Fresh(),
                Run(wallHits: 4, touchedHazard: false));
            Assert.That(wallyButClean, Does.Contain("untouched"));
            Assert.That(wallyButClean, Does.Not.Contain("no_walls"));

            var wallFreeButSandy = Achievements.EvaluateRun(Fresh(),
                Run(wallHits: 0, wallHitsFinalShot: 0, touchedHazard: true));
            Assert.That(wallFreeButSandy, Does.Contain("no_walls"));
            Assert.That(wallFreeButSandy, Does.Not.Contain("untouched"));
        }

        [Test]
        public void Millwright_NeedsAMillOnTheCourse_AndNoBladeHit()
        {
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(hasWindmill: true, windmillHits: 0)),
                Does.Contain("millwright"));
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(hasWindmill: true, windmillHits: 1)),
                Does.Not.Contain("millwright"), "a blade hit disqualifies");
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(hasWindmill: false, windmillHits: 0)),
                Does.Not.Contain("millwright"), "a course without a mill cannot earn it");
        }

        [Test]
        public void DownToTheWire_FiresOnlyOnTheFinalAllowedStroke()
        {
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(strokes: 6, strokeLimit: 6)),
                Does.Contain("last_stroke"));
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(strokes: 5, strokeLimit: 6)),
                Does.Not.Contain("last_stroke"));
        }

        [Test]
        public void DownToTheWire_ReadsTheACTIVELimit_SoHardModeCounts()
        {
            // Hard mode allows par + 1: holing on stroke 3 of a par 2 is the
            // wire there, and merely comfortable under normal rules.
            Assert.That(Achievements.EvaluateRun(Fresh(),
                    Run(mode: GameMode.Daily, isHardMode: true, strokes: 3, par: 2, strokeLimit: 3)),
                Does.Contain("last_stroke"));
            Assert.That(Achievements.EvaluateRun(Fresh(),
                    Run(mode: GameMode.Daily, strokes: 3, par: 2, strokeLimit: 5)),
                Does.Not.Contain("last_stroke"));
        }

        [Test]
        public void HardDay_OnlyOnHardDailies()
        {
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(mode: GameMode.Daily, isHardMode: true)),
                Does.Contain("hard_daily"));
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(mode: GameMode.Daily)),
                Does.Not.Contain("hard_daily"));
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(mode: GameMode.Practice, isHardMode: true)),
                Does.Not.Contain("hard_daily"), "hard rules only exist on the daily");
        }

        [Test]
        public void ThreeStars_OnlyOnDailies()
        {
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(mode: GameMode.Daily, strokes: 2, par: 3)),
                Does.Contain("three_star"));
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(mode: GameMode.Practice, strokes: 2, par: 3)),
                Does.Not.Contain("three_star"));
        }

        [Test]
        public void ThreeStars_EarnedAtPar_NotJustUnderIt()
        {
            // The 2026-08-18 scoring recalibration: par carries the top tier,
            // so this no longer duplicates the Ace condition.
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(mode: GameMode.Daily, strokes: 2, par: 2)),
                Does.Contain("three_star"), "par earns three stars");
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(mode: GameMode.Daily, strokes: 3, par: 2)),
                Does.Not.Contain("three_star"), "one over does not");
        }

        [Test]
        public void ThreeStars_AndAce_AreNoLongerTheSameCondition()
        {
            // On the par-2 courses generation actually makes, a two-stroke
            // finish must earn the star achievement WITHOUT earning Ace.
            var earned = Achievements.EvaluateRun(Fresh(), Run(mode: GameMode.Daily, strokes: 2, par: 2));
            Assert.That(earned, Does.Contain("three_star"));
            Assert.That(earned, Does.Not.Contain("ace"));
        }

        [Test]
        public void Perfectionist_CountsThreeStarDaysOnly()
        {
            var data = Fresh();
            for (int day = 1; day <= 9; day++)
            {
                data.days.Add(new DayRecord { day = day, completed = true, bestStars = 3 });
            }

            data.days.Add(new DayRecord { day = 10, completed = true, bestStars = 2 });
            Assert.That(Achievements.ThreeStarDayCount(data), Is.EqualTo(9));
            Assert.That(Achievements.EvaluateRun(data, Run()), Does.Not.Contain("three_star_10"));

            data.days.Add(new DayRecord { day = 11, completed = true, bestStars = 3 });
            Assert.That(Achievements.EvaluateRun(data, Run()), Does.Contain("three_star_10"));
        }

        [Test]
        public void TimeTraveler_OnlyOnArchiveDailies()
        {
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(mode: GameMode.Daily, isArchiveDay: true)),
                Does.Contain("archive1"));
            Assert.That(Achievements.EvaluateRun(Fresh(), Run(mode: GameMode.Daily)),
                Does.Not.Contain("archive1"));
        }

        [Test]
        public void SevenDays_ReadsTheRecordedStreak()
        {
            var data = Fresh();
            data.streak = 7;
            Assert.That(Achievements.EvaluateRun(data, Run(mode: GameMode.Daily)),
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
            Assert.That(Achievements.EvaluateRun(data, Run(mode: GameMode.Daily)),
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
