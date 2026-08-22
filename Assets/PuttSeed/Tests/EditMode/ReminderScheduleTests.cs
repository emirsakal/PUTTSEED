using System;
using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// When the reminder may fire. The hole flips at UTC midnight and players
    /// do not live there, so the naive "every morning at ten" fires on holes
    /// the player already answered — reliably so west of Greenwich, where an
    /// evening session answers TOMORROW's UTC day. A notification that lies
    /// once is a notification that gets turned off.
    /// </summary>
    public class ReminderScheduleTests
    {
        private static readonly TimeSpan Turkey = TimeSpan.FromHours(3);
        private static readonly TimeSpan California = TimeSpan.FromHours(-8);

        [Test]
        public void EveningPlayerInTurkey_IsNudgedTomorrowMorning()
        {
            // 22:00 in Turkey, today's hole answered.
            var nowUtc = new DateTime(2026, 8, 22, 19, 0, 0);
            var fires = DailyReminder.NextFires(nowUtc, Turkey, todayAnswered: true, count: 3);

            // Tomorrow 10:00 local = 07:00 UTC.
            Assert.That(fires[0], Is.EqualTo(new DateTime(2026, 8, 23, 7, 0, 0)));
            Assert.That(fires.Count, Is.EqualTo(3));
        }

        [Test]
        public void EveningPlayerInCalifornia_SkipsTheMorningOfTheHoleTheyAlreadyPlayed()
        {
            // Monday 20:00 in California is TUESDAY 04:00 UTC — the session
            // answered Tuesday's hole. Tuesday 10:00 local is still Tuesday in
            // UTC, so the first honest nudge is Wednesday morning.
            var nowUtc = new DateTime(2026, 8, 25, 4, 0, 0); // Tue UTC
            var fires = DailyReminder.NextFires(nowUtc, California, todayAnswered: true, count: 3);

            Assert.That(fires[0], Is.EqualTo(new DateTime(2026, 8, 26, 18, 0, 0)),
                "the first fire must be Wednesday 10:00 local (18:00 UTC), not Tuesday's");
        }

        [Test]
        public void AnUnansweredMorning_IsNudgedTheSameDay()
        {
            // 09:00 local in Turkey, hole not yet played: the nudge comes at
            // 10:00 today rather than waiting for tomorrow.
            var nowUtc = new DateTime(2026, 8, 22, 6, 0, 0);
            var fires = DailyReminder.NextFires(nowUtc, Turkey, todayAnswered: false, count: 3);

            Assert.That(fires[0], Is.EqualTo(new DateTime(2026, 8, 22, 7, 0, 0)));
        }

        [Test]
        public void EveryFireLandsOnItsOwnUnansweredDay()
        {
            foreach (var offset in new[] { Turkey, California, TimeSpan.Zero, TimeSpan.FromHours(13) })
            {
                var nowUtc = new DateTime(2026, 8, 22, 15, 30, 0);
                var fires = DailyReminder.NextFires(nowUtc, offset, todayAnswered: true, count: 3);

                Assert.That(fires.Count, Is.EqualTo(3));
                int previousDay = ModeController.DayNumber(nowUtc);
                foreach (var fireUtc in fires)
                {
                    int day = ModeController.DayNumber(fireUtc);
                    Assert.That(day, Is.GreaterThan(previousDay),
                        $"offset {offset}: two nudges for one hole, or one for an answered one");
                    Assert.That(fireUtc, Is.GreaterThan(nowUtc));
                    previousDay = day;
                }
            }
        }

        [Test]
        public void TheSwitchAndTheOffer_SurviveASaveRoundTrip()
        {
            var data = new SaveData { reminderEnabled = true, reminderAsked = true };
            var loaded = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(data));

            Assert.That(loaded.reminderEnabled, Is.True);
            Assert.That(loaded.reminderAsked, Is.True);
            Assert.That(new SaveData().reminderEnabled, Is.False, "the reminder is opt-in, never a default");
            Assert.That(new SaveData().reminderAsked, Is.False, "a fresh save has not been offered it");
        }
    }
}
