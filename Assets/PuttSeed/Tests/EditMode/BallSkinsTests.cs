using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    public class BallSkinsTests
    {
        [Test]
        public void Default_IsAlwaysUnlocked()
        {
            var data = new SaveData();
            Assert.That(BallSkins.IsUnlocked(BallSkins.All[0], data), Is.True);
            Assert.That(BallSkins.UnlockedCount(data), Is.EqualTo(1));
        }

        [Test]
        public void UnknownId_FallsBackToDefault()
        {
            Assert.That(BallSkins.Resolve("nope").Id, Is.EqualTo("cream"));
        }

        [Test]
        public void Cycle_SkipsLockedSkins()
        {
            var data = new SaveData();
            // Nothing unlocked: cycling from cream wraps back to cream.
            Assert.That(BallSkins.NextUnlocked("cream", data).Id, Is.EqualTo("cream"));

            data.achievements.Add("ace"); // unlocks rose
            Assert.That(BallSkins.NextUnlocked("cream", data).Id, Is.EqualTo("rose"));
            Assert.That(BallSkins.NextUnlocked("rose", data).Id, Is.EqualTo("cream"));
        }

        [Test]
        public void EveryGate_IsARealAchievementId()
        {
            foreach (var skin in BallSkins.All)
            {
                if (skin.RequiredAchievement != null)
                {
                    Assert.That(Achievements.Find(skin.RequiredAchievement), Is.Not.Null,
                        $"skin {skin.Id} gates on unknown achievement {skin.RequiredAchievement}");
                }
            }
        }

        [Test]
        public void JourneyGates_StayWithinTheCampaign()
        {
            foreach (var skin in BallSkins.All)
            {
                Assert.That(skin.RequiredJourneyLevel,
                    Is.InRange(0, JourneyConfig.Seeds.Length), skin.Id);
                Assert.That(skin.RequiredJourneyStars,
                    Is.InRange(0, JourneyConfig.Seeds.Length * 3), skin.Id);
            }
        }

        [Test]
        public void JourneyLevelGate_UnlocksOnCompletion()
        {
            var lime = BallSkins.Resolve("lime"); // requires journey level 5
            var data = new SaveData();
            for (int level = 0; level < 4; level++)
            {
                data.journeyStars.Add(1);
            }

            Assert.That(BallSkins.IsUnlocked(lime, data), Is.False, "4 done is not enough");
            data.journeyStars.Add(1);
            Assert.That(BallSkins.IsUnlocked(lime, data), Is.True, "5 done unlocks");
        }

        [Test]
        public void JourneyStarsGate_CountsTotalStars()
        {
            var ember = BallSkins.Resolve("ember"); // requires 150 total stars
            var data = new SaveData();
            for (int level = 0; level < 49; level++)
            {
                data.journeyStars.Add(3); // 147 stars
            }

            Assert.That(BallSkins.IsUnlocked(ember, data), Is.False);
            data.journeyStars.Add(3); // 150 stars total
            Assert.That(BallSkins.IsUnlocked(ember, data), Is.True);
        }
    }
}
