using NUnit.Framework;
using PuttSeed.Core.Daily;

namespace PuttSeed.Core.Tests.Daily
{
    [TestFixture]
    public class GauntletWeekTests
    {
        [Test]
        public void DayZero_IsTheEpoch()
        {
            DailyCalendar.ToDate(0, out int y, out int m, out int d);
            Assert.That((y, m, d), Is.EqualTo((2020, 1, 1)));
        }

        [TestCase(0, 2020, 1, 1)]
        [TestCase(1, 2020, 1, 2)]
        [TestCase(30, 2020, 1, 31)]
        [TestCase(31, 2020, 2, 1)]
        [TestCase(59, 2020, 2, 29, Description = "2020 is a leap year")]
        [TestCase(60, 2020, 3, 1)]
        [TestCase(365, 2020, 12, 31)]
        [TestCase(366, 2021, 1, 1)]
        [TestCase(424, 2021, 2, 28)]
        [TestCase(425, 2021, 3, 1, Description = "2021 is not a leap year")]
        [TestCase(1857, 2025, 1, 31)]
        [TestCase(2420, 2026, 8, 17)]
        public void ToDate_MatchesTheCalendar(int dayNumber, int year, int month, int day)
        {
            DailyCalendar.ToDate(dayNumber, out int y, out int m, out int d);
            Assert.That((y, m, d), Is.EqualTo((year, month, day)));
        }

        [Test]
        public void ToDate_AdvancesOneDayAtATime_OverFiveYears()
        {
            // The integer conversion has no DateTime to lean on, so walk it:
            // every step must move exactly one calendar day forward.
            DailyCalendar.ToDate(0, out int py, out int pm, out int pd);
            for (int day = 1; day <= 1830; day++)
            {
                DailyCalendar.ToDate(day, out int y, out int m, out int d);
                bool nextDaySameMonth = y == py && m == pm && d == pd + 1;
                bool firstOfNextMonth = y == py && m == pm + 1 && d == 1;
                bool firstOfNextYear = y == py + 1 && m == 1 && pm == 12 && d == 1;
                Assert.That(nextDaySameMonth || firstOfNextMonth || firstOfNextYear, Is.True,
                    $"day {day}: {py}-{pm}-{pd} jumped to {y}-{m}-{d}");
                (py, pm, pd) = (y, m, d);
            }
        }

        [Test]
        public void SeedForDay_MatchesTheDailySeedForThatDate()
        {
            // The gauntlet must play the very same courses the dailies did.
            for (int day = 0; day < 400; day += 7)
            {
                DailyCalendar.ToDate(day, out int y, out int m, out int d);
                Assert.That(DailyCalendar.SeedForDay(day),
                    Is.EqualTo(DailySeed.FromUtcDate(y, m, d)), $"day {day}");
            }
        }

        [Test]
        public void Weeks_AreSevenConsecutiveDays()
        {
            Assert.That(GauntletWeek.FirstDay(0), Is.Zero);
            Assert.That(GauntletWeek.FirstDay(3), Is.EqualTo(21));
            for (int hole = 0; hole < GauntletWeek.Length; hole++)
            {
                Assert.That(GauntletWeek.DayOfHole(3, hole), Is.EqualTo(21 + hole));
                Assert.That(GauntletWeek.WeekOf(21 + hole), Is.EqualTo(3));
            }
        }

        [Test]
        public void OnlyFullyElapsedWeeks_ArePlayable()
        {
            // Mid-week on week 5 (days 35..41): week 4 is the newest complete
            // one, and week 5 must stay shut until its last day has happened.
            int today = 38;
            Assert.That(GauntletWeek.LatestCompleteWeek(today), Is.EqualTo(4));
            Assert.That(GauntletWeek.IsPlayable(4, today), Is.True);
            Assert.That(GauntletWeek.IsPlayable(5, today), Is.False, "this week is still running");
            Assert.That(GauntletWeek.IsPlayable(-1, today), Is.False);
        }

        [Test]
        public void FirstWeekEver_IsNotPlayableUntilItEnds()
        {
            Assert.That(GauntletWeek.LatestCompleteWeek(0), Is.EqualTo(-1));
            Assert.That(GauntletWeek.IsPlayable(0, 3), Is.False);
            Assert.That(GauntletWeek.IsPlayable(0, 7), Is.True, "day 7 starts week 1, so week 0 is done");
        }

        [Test]
        public void EveryHole_HasItsOwnCourse()
        {
            var seen = new System.Collections.Generic.HashSet<ulong>();
            for (int hole = 0; hole < GauntletWeek.Length; hole++)
            {
                Assert.That(seen.Add(GauntletWeek.SeedForHole(300, hole)), Is.True,
                    $"hole {hole} repeats an earlier seed");
            }
        }
    }
}
