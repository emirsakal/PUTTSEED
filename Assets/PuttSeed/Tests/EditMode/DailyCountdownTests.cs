using System;
using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    public class DailyCountdownTests
    {
        [Test]
        public void JustBeforeMidnight_SecondsRemain()
        {
            var now = new DateTime(2026, 8, 17, 23, 59, 30, DateTimeKind.Utc);
            Assert.That(DailyCountdown.UntilNextHole(now), Is.EqualTo(TimeSpan.FromSeconds(30)));
        }

        [Test]
        public void AtMidnight_FullDayRemains()
        {
            var now = new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc);
            Assert.That(DailyCountdown.UntilNextHole(now), Is.EqualTo(TimeSpan.FromHours(24)));
        }

        [TestCase(7, 12, 33, "07:12:33")]
        [TestCase(0, 0, 1, "00:00:01")]
        [TestCase(23, 59, 59, "23:59:59")]
        public void Format_IsHoursMinutesSeconds(int h, int m, int s, string expected)
        {
            Assert.That(DailyCountdown.Format(new TimeSpan(h, m, s)), Is.EqualTo(expected));
        }

        [Test]
        public void Format_ClampsNegativeToZero()
        {
            Assert.That(DailyCountdown.Format(TimeSpan.FromSeconds(-5)), Is.EqualTo("00:00:00"));
        }
    }
}
