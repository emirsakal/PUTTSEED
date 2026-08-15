using NUnit.Framework;
using PuttSeed.Core.Daily;

namespace PuttSeed.Core.Tests.Daily
{
    [TestFixture]
    public class DailySeedTests
    {
        [Test]
        public void SameDate_SameSeed()
        {
            Assert.That(DailySeed.FromUtcDate(2026, 8, 15), Is.EqualTo(DailySeed.FromUtcDate(2026, 8, 15)));
        }

        [Test]
        public void DifferentDates_DifferentSeeds()
        {
            var seen = new System.Collections.Generic.HashSet<ulong>();
            for (int day = 1; day <= 28; day++)
            {
                for (int month = 1; month <= 12; month++)
                {
                    Assert.That(seen.Add(DailySeed.FromUtcDate(2026, month, day)), Is.True,
                        $"seed collision at 2026-{month:D2}-{day:D2}");
                }
            }
        }

        [Test]
        public void ConsecutiveDays_ProduceUnrelatedSeeds()
        {
            // SplitMix64 mixing: consecutive dates must not produce near-identical
            // seeds. Crude check: high 32 bits differ.
            var a = DailySeed.FromUtcDate(2026, 8, 15);
            var b = DailySeed.FromUtcDate(2026, 8, 16);
            Assert.That(a >> 32, Is.Not.EqualTo(b >> 32));
        }

        [Test]
        public void YearBoundaries_AreDistinct()
        {
            Assert.That(DailySeed.FromUtcDate(2026, 12, 31), Is.Not.EqualTo(DailySeed.FromUtcDate(2027, 1, 1)));
            Assert.That(DailySeed.FromUtcDate(2026, 1, 1), Is.Not.EqualTo(DailySeed.FromUtcDate(2027, 1, 1)));
        }

        [Test]
        public void GoldenSeeds_AreStable()
        {
            // Frozen values: the daily seed derivation is a cross-device contract.
            Assert.That(DailySeed.FromUtcDate(2026, 8, 15), Is.EqualTo(13205062183649627085UL),
                $"actual 2026-08-15: {DailySeed.FromUtcDate(2026, 8, 15)}UL");
            Assert.That(DailySeed.FromUtcDate(2026, 1, 1), Is.EqualTo(16888636937873607525UL),
                $"actual 2026-01-01: {DailySeed.FromUtcDate(2026, 1, 1)}UL");
        }
    }
}
