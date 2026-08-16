using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Unity;
using UnityEngine;

namespace PuttSeed.Unity.Tests
{
    public class FeelConfigTests
    {
        [Test]
        public void Quantize_SnapsToTheFixedGrid()
        {
            Assert.That(FeelConfig.Quantize(0.988f).Raw, Is.EqualTo(Fix64.FromFraction(9880, 10000).Raw));
            Assert.That(FeelConfig.Quantize(8f).Raw, Is.EqualTo(Fix64.FromInt(8).Raw));
            Assert.That(FeelConfig.Quantize(1.2f).Raw, Is.EqualTo(Fix64.FromFraction(12000, 10000).Raw));
        }

        [Test]
        public void BuildSimConfig_IsDeterministic_ForEqualValues()
        {
            var a = ScriptableObject.CreateInstance<FeelConfig>();
            var b = ScriptableObject.CreateInstance<FeelConfig>();
            var ca = a.BuildSimConfig();
            var cb = b.BuildSimConfig();

            Assert.That(ca.MaxShotSpeed.Raw, Is.EqualTo(cb.MaxShotSpeed.Raw));
            Assert.That(ca.RollDamping.Raw, Is.EqualTo(cb.RollDamping.Raw));
            Assert.That(ca.SandDamping.Raw, Is.EqualTo(cb.SandDamping.Raw));
            Assert.That(ca.WallRestitution.Raw, Is.EqualTo(cb.WallRestitution.Raw));
            Assert.That(ca.BumperRestitution.Raw, Is.EqualTo(cb.BumperRestitution.Raw));
            Assert.That(ca.HoleRadius.Raw, Is.EqualTo(cb.HoleRadius.Raw));
            Assert.That(ca.HoleCaptureSpeedSq.Raw, Is.EqualTo(cb.HoleCaptureSpeedSq.Raw));
            Assert.That(ca.RestSpeedEpsSq.Raw, Is.EqualTo(cb.RestSpeedEpsSq.Raw));
            Assert.That(ca.RestTicksRequired, Is.EqualTo(cb.RestTicksRequired));

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void DefaultAsset_MatchesCoreDefaults_WhereKnobsOverlap()
        {
            // The asset defaults were chosen to mirror SimConfig.Default so
            // switching to FeelConfig does not change the tuned Week-1 feel.
            var feel = ScriptableObject.CreateInstance<FeelConfig>();
            var built = feel.BuildSimConfig();
            var core = PuttSeed.Core.Sim.SimConfig.Default;

            Assert.That(built.Dt.Raw, Is.EqualTo(core.Dt.Raw));
            Assert.That(built.BallRadius.Raw, Is.EqualTo(core.BallRadius.Raw));
            Assert.That(built.MaxShotSpeed.Raw, Is.EqualTo(core.MaxShotSpeed.Raw));
            Assert.That(built.RollDamping.Raw, Is.EqualTo(core.RollDamping.Raw));
            Assert.That(built.SandDamping.Raw, Is.EqualTo(core.SandDamping.Raw));

            // Intentional divergence (2026-08-16 feel pass): ice slides more
            // than core's frozen 0.997 baseline.
            Assert.That(built.IceDamping.Raw, Is.EqualTo(FeelConfig.Quantize(0.9985f).Raw));
            Assert.That(built.WallRestitution.Raw, Is.EqualTo(core.WallRestitution.Raw));
            Assert.That(built.BumperRestitution.Raw, Is.EqualTo(core.BumperRestitution.Raw));
            Assert.That(built.BumperMaxExitSpeed.Raw, Is.EqualTo(core.BumperMaxExitSpeed.Raw));
            Assert.That(built.HoleRadius.Raw, Is.EqualTo(core.HoleRadius.Raw));
            // Squared fields are built by squaring the quantized knob, which can
            // land an ulp or two off core's exact fraction — inconsequential.
            Assert.That(built.RestSpeedEpsSq.Raw, Is.EqualTo(core.RestSpeedEpsSq.Raw).Within(2));
            Assert.That(built.RimRestitution.Raw, Is.EqualTo(core.RimRestitution.Raw).Within(2));

            // Intentional divergence from core defaults (2026-08-16 feel pass):
            // capture threshold raised to 1.5 so medium-pace putts drop. Core's
            // Default (1.2) remains the frozen test-fixture baseline.
            var expectedCapture = FeelConfig.Quantize(1.5f);
            Assert.That(built.HoleCaptureSpeedSq.Raw,
                Is.EqualTo((expectedCapture * expectedCapture).Raw).Within(2));
            Assert.That(built.RestTicksRequired, Is.EqualTo(core.RestTicksRequired));

            Object.DestroyImmediate(feel);
        }

        [Test]
        public void PlayConfig_TouchCaptureOnEasyAndNormal_ThresholdOnHard()
        {
            var feel = ScriptableObject.CreateInstance<FeelConfig>();
            var baseConfig = feel.BuildSimConfig();

            var easy = feel.BuildPlayConfig(baseConfig, PuttSeed.Core.CourseGen.Difficulty.Easy);
            var normal = feel.BuildPlayConfig(baseConfig, PuttSeed.Core.CourseGen.Difficulty.Normal);
            var hard = feel.BuildPlayConfig(baseConfig, PuttSeed.Core.CourseGen.Difficulty.Hard);

            // Touch capture: threshold far above any reachable speed².
            Assert.That(easy.HoleCaptureSpeedSq.Raw, Is.EqualTo(Fix64.FromInt(1_000_000).Raw));
            Assert.That(normal.HoleCaptureSpeedSq.Raw, Is.EqualTo(Fix64.FromInt(1_000_000).Raw));
            Assert.That(hard.HoleCaptureSpeedSq.Raw, Is.EqualTo(baseConfig.HoleCaptureSpeedSq.Raw));

            // Everything else must carry over unchanged.
            Assert.That(easy.RollDamping.Raw, Is.EqualTo(baseConfig.RollDamping.Raw));
            Assert.That(easy.IceDamping.Raw, Is.EqualTo(baseConfig.IceDamping.Raw));
            Assert.That(easy.HoleRadius.Raw, Is.EqualTo(baseConfig.HoleRadius.Raw));

            // The toggle turns the relaxation off entirely.
            feel.touchCaptureBelowHard = false;
            var strict = feel.BuildPlayConfig(baseConfig, PuttSeed.Core.CourseGen.Difficulty.Easy);
            Assert.That(strict.HoleCaptureSpeedSq.Raw, Is.EqualTo(baseConfig.HoleCaptureSpeedSq.Raw));

            Object.DestroyImmediate(feel);
        }
    }
}
