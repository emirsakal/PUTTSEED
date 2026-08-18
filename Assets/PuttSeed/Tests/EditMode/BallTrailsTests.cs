using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    public class BallTrailsTests
    {
        private static SaveData WithStars(int levels, int starsEach)
        {
            var data = new SaveData();
            for (int i = 0; i < levels; i++)
            {
                data.journeyStars.Add(starsEach);
            }

            return data;
        }

        [Test]
        public void FirstTrail_IsAlwaysUnlocked()
        {
            Assert.That(BallTrails.IsUnlocked(BallTrails.All[0], new SaveData()), Is.True);
            Assert.That(BallTrails.UnlockedCount(new SaveData()), Is.EqualTo(1));
        }

        [Test]
        public void UnknownId_FallsBackToTheDefault()
        {
            Assert.That(BallTrails.Resolve("nope"), Is.SameAs(BallTrails.All[0]));
            Assert.That(BallTrails.Resolve("blaze").Id, Is.EqualTo("blaze"));
        }

        [Test]
        public void AchievementGates_FollowTheAchievement()
        {
            var spark = BallTrails.Resolve("spark"); // gated on bank_shot
            var data = new SaveData();
            Assert.That(BallTrails.IsUnlocked(spark, data), Is.False);

            data.achievements.Add("bank_shot");
            Assert.That(BallTrails.IsUnlocked(spark, data), Is.True);
        }

        [Test]
        public void StarGates_StepAcrossTheCampaign()
        {
            var blaze = BallTrails.Resolve("blaze");   // 100 stars
            var aurora = BallTrails.Resolve("aurora"); // 200
            var prism = BallTrails.Resolve("prism");   // 300

            var at99 = WithStars(33, 3); // 99
            Assert.That(BallTrails.IsUnlocked(blaze, at99), Is.False);

            var at100 = WithStars(50, 2); // 100
            Assert.That(BallTrails.IsUnlocked(blaze, at100), Is.True);
            Assert.That(BallTrails.IsUnlocked(aurora, at100), Is.False);

            var at200 = WithStars(100, 2); // 200
            Assert.That(BallTrails.IsUnlocked(aurora, at200), Is.True);
            Assert.That(BallTrails.IsUnlocked(prism, at200), Is.False);

            var perfect = WithStars(100, 3); // 300 — every level, every star
            Assert.That(BallTrails.IsUnlocked(prism, perfect), Is.True);
        }

        [Test]
        public void StarGates_StayWithinTheCampaignTotal()
        {
            // 100 levels x 3 stars: a gate above 300 could never be reached.
            int maxStars = JourneyConfig.Seeds.Length * 3;
            foreach (var trail in BallTrails.All)
            {
                Assert.That(trail.RequiredJourneyStars, Is.InRange(0, maxStars), trail.Id);
            }
        }

        [Test]
        public void EveryAchievementGate_NamesARealAchievement()
        {
            foreach (var trail in BallTrails.All)
            {
                if (trail.RequiredAchievement != null)
                {
                    Assert.That(Achievements.Find(trail.RequiredAchievement), Is.Not.Null, trail.Id);
                }
            }
        }

        [Test]
        public void UnlockHint_IsNeverEmptyForALockedTrail()
        {
            foreach (var trail in BallTrails.All)
            {
                if (!BallTrails.IsUnlocked(trail, new SaveData()))
                {
                    Assert.That(BallTrails.UnlockHint(trail), Is.Not.Empty, trail.Id);
                }
            }
        }

        [Test]
        public void Ids_AreUnique()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var trail in BallTrails.All)
            {
                Assert.That(seen.Add(trail.Id), Is.True, $"duplicate trail id {trail.Id}");
            }
        }
    }
}
