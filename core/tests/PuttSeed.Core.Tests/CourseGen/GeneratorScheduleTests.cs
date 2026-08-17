using NUnit.Framework;
using PuttSeed.Core.CourseGen;

namespace PuttSeed.Core.Tests.CourseGen
{
    [TestFixture]
    public class GeneratorScheduleTests
    {
        [Test]
        public void DaysBeforeCutover_UseVersion1()
        {
            Assert.That(GeneratorSchedule.VersionForDay(0), Is.EqualTo(1));
            Assert.That(GeneratorSchedule.VersionForDay(GeneratorSchedule.V2FromDay - 1), Is.EqualTo(1));
        }

        [Test]
        public void CutoverAndLater_UseVersion2()
        {
            Assert.That(GeneratorSchedule.VersionForDay(GeneratorSchedule.V2FromDay), Is.EqualTo(2));
            Assert.That(GeneratorSchedule.VersionForDay(GeneratorSchedule.V2FromDay + 1000), Is.EqualTo(2));
        }

        [Test]
        public void NegativeDays_ClampToVersion1()
        {
            Assert.That(GeneratorSchedule.VersionForDay(-5), Is.EqualTo(1));
        }

        [Test]
        public void ConfigForVersion_MapsBothVersions()
        {
            Assert.That(GeneratorConfig.ForVersion(1), Is.SameAs(GeneratorConfig.V1));
            Assert.That(GeneratorConfig.ForVersion(2), Is.SameAs(GeneratorConfig.V2));
        }

        [Test]
        public void ConfigForVersion_RejectsUnknown()
        {
            Assert.That(() => GeneratorConfig.ForVersion(3), Throws.ArgumentException);
            Assert.That(() => GeneratorConfig.ForVersion(0), Throws.ArgumentException);
        }

        [Test]
        public void V1_IsTheFrozenDefault_WithZeroNewElementBudgets()
        {
            Assert.That(GeneratorConfig.V1, Is.SameAs(GeneratorConfig.Default),
                "Default must stay the frozen v1 — journey and old replays depend on it");
            Assert.That(GeneratorConfig.V1.MaxGates, Is.Zero);
            Assert.That(GeneratorConfig.V1.MaxRamps, Is.Zero);
            Assert.That(GeneratorConfig.V1.MaxPortals, Is.Zero);
            Assert.That(GeneratorConfig.V1.MaxWindmills, Is.Zero);
        }

        [Test]
        public void V2_KeepsLegacyBudgets_AndOpensNewOnes()
        {
            var v1 = GeneratorConfig.V1;
            var v2 = GeneratorConfig.V2;
            Assert.That(v2.MaxBumpers, Is.EqualTo(v1.MaxBumpers));
            Assert.That(v2.MaxSand, Is.EqualTo(v1.MaxSand));
            Assert.That(v2.MaxWater, Is.EqualTo(v1.MaxWater));
            Assert.That(v2.MaxIce, Is.EqualTo(v1.MaxIce));
            Assert.That(v2.MaxGates + v2.MaxRamps + v2.MaxPortals + v2.MaxWindmills,
                Is.GreaterThan(0), "v2 must actually enable something new");
        }
    }
}
