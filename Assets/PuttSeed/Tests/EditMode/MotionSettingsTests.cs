using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// Reduced motion has to take exactly four things and leave the rest. The
    /// tempting mistake is to treat it as a volume knob on juice and quietly
    /// drop the splash too — but the splash is how a player learns they just
    /// lost a stroke to water, and an accessibility setting that hides
    /// information is a broken game rather than a considerate one.
    /// </summary>
    public class MotionSettingsTests
    {
        [Test]
        public void FullMotion_AllowsEverything()
        {
            foreach (MotionEffect effect in System.Enum.GetValues(typeof(MotionEffect)))
            {
                Assert.That(MotionSettings.Allows(effect, reducedMotion: false), Is.True, effect.ToString());
            }
        }

        [Test]
        public void ReducedMotion_TakesTheFourDecorativeEffects()
        {
            Assert.That(MotionSettings.Allows(MotionEffect.Shake, true), Is.False);
            Assert.That(MotionSettings.Allows(MotionEffect.SlowMo, true), Is.False);
            Assert.That(MotionSettings.Allows(MotionEffect.Letterbox, true), Is.False);
            Assert.That(MotionSettings.Allows(MotionEffect.Confetti, true), Is.False);
            Assert.That(MotionSettings.Allows(MotionEffect.CameraPush, true), Is.False);
        }

        [Test]
        public void ReducedMotion_KeepsEveryEffectThatCarriesInformation()
        {
            Assert.That(MotionSettings.Allows(MotionEffect.Splash, true), Is.True);
            Assert.That(MotionSettings.Allows(MotionEffect.Puff, true), Is.True);
            Assert.That(MotionSettings.Allows(MotionEffect.StarReveal, true), Is.True);
        }

        [Test]
        public void NothingElseIsSuppressed()
        {
            // "All four and nothing else" as one assertion, so a fifth effect
            // added to Calming later fails here rather than surprising a player.
            var expected = MotionEffect.Shake | MotionEffect.SlowMo | MotionEffect.Letterbox
                | MotionEffect.Confetti | MotionEffect.CameraPush;
            Assert.That(MotionSettings.Calming, Is.EqualTo(expected));
        }

        [Test]
        public void TheSettingSurvivesASaveRoundTrip()
        {
            var data = new SaveData();
            Assert.That(data.reducedMotion, Is.False, "full motion is the default");

            data.reducedMotion = true;
            var json = UnityEngine.JsonUtility.ToJson(data);
            var loaded = UnityEngine.JsonUtility.FromJson<SaveData>(json);
            Assert.That(loaded.reducedMotion, Is.True);
        }
    }
}
