using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    public class LocTests
    {
        [TearDown]
        public void TearDown()
        {
            Loc.Apply("en"); // never leak a language into other tests
        }

        [Test]
        public void English_IsIdentity()
        {
            Loc.Apply("en");
            Assert.That(Loc.Tr("Practice"), Is.EqualTo("Practice"));
        }

        [Test]
        public void Turkish_TranslatesKnown_PassesThroughUnknown()
        {
            Loc.Apply("tr");
            Assert.That(Loc.Tr("Practice"), Is.EqualTo("Antrenman"));
            Assert.That(Loc.Tr("Some brand new string"), Is.EqualTo("Some brand new string"));
        }

        [Test]
        public void ExplicitCodes_WinOverAuto()
        {
            Loc.Apply("tr");
            Assert.That(Loc.Current, Is.EqualTo(Loc.Language.Turkish));
            Loc.Apply("en");
            Assert.That(Loc.Current, Is.EqualTo(Loc.Language.English));
        }

        [Test]
        public void EveryTranslation_KeepsItsFormatPlaceholders()
        {
            foreach (var pair in Loc.Turkish)
            {
                Assert.That(pair.Value, Is.Not.Empty, $"empty translation for '{pair.Key}'");
                for (int i = 0; i < 5; i++)
                {
                    string slot = "{" + i + "}";
                    Assert.That(pair.Value.Contains(slot), Is.EqualTo(pair.Key.Contains(slot)),
                        $"placeholder {slot} mismatch in '{pair.Key}' -> '{pair.Value}'");
                }
            }
        }

        [Test]
        public void EveryAchievementAndSkin_HasATurkishEntry()
        {
            foreach (var def in Achievements.All)
            {
                if (def.Title != "Ace") // golf vocabulary stays universal
                {
                    Assert.That(Loc.Turkish.ContainsKey(def.Title), Is.True, def.Title);
                }

                Assert.That(Loc.Turkish.ContainsKey(def.Detail), Is.True, def.Detail);
            }

            foreach (var skin in BallSkins.All)
            {
                Assert.That(Loc.Turkish.ContainsKey(skin.Name), Is.True, skin.Name);
            }
        }

        [Test]
        public void EveryTutorialHint_HasATurkishEntry()
        {
            foreach (var stage in TutorialConfig.Stages)
            {
                Assert.That(Loc.Turkish.ContainsKey(stage.Hint), Is.True, stage.Hint);
            }
        }
    }
}
