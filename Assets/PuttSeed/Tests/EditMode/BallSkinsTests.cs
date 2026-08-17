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
    }
}
