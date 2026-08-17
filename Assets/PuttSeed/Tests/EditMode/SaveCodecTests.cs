using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    public class SaveCodecTests
    {
        [Test]
        public void RoundTrip_PreservesTheSave()
        {
            var data = new SaveData { streak = 4, bestStreak = 9, practicePlayed = 12 };
            data.achievements.Add("ace");
            data.days.Add(new DayRecord { day = 100, bestStrokes = 2, bestStars = 3, completed = true });

            var code = SaveCodec.Export(data);
            Assert.That(code, Does.StartWith(SaveCodec.Prefix));
            Assert.That(SaveCodec.TryImport(code, out var back), Is.True);
            Assert.That(back.streak, Is.EqualTo(4));
            Assert.That(back.bestStreak, Is.EqualTo(9));
            Assert.That(back.achievements, Does.Contain("ace"));
            Assert.That(back.days[0].bestStars, Is.EqualTo(3));
        }

        [Test]
        public void Import_ScansSurroundingText()
        {
            var code = SaveCodec.Export(new SaveData { streak = 2 });
            Assert.That(SaveCodec.TryImport($"my backup:\n{code}\ncheers", out var back), Is.True);
            Assert.That(back.streak, Is.EqualTo(2));
        }

        [TestCase("")]
        [TestCase("PUTT-AQMAAAAAAAAAAmD_A2B8Ag")]
        [TestCase("PUTTSAVE-!!!notbase64!!!")]
        public void Import_RejectsGarbage(string text)
        {
            Assert.That(SaveCodec.TryImport(text, out _), Is.False);
        }
    }
}
