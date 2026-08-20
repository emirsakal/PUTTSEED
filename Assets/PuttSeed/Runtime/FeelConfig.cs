using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Every feel knob in one asset for on-device tuning. Floats live only in
    /// the inspector; <see cref="BuildSimConfig"/> quantizes them onto a fixed
    /// 1/10000 grid at the boundary, so identical asset values produce a
    /// bit-identical simulation on every device. This asset ships with the
    /// build — changing it changes the daily course for everyone equally.
    /// </summary>
    [CreateAssetMenu(fileName = "FeelConfig", menuName = "PuttSeed/Feel Config")]
    public sealed class FeelConfig : ScriptableObject
    {
        [Header("Friction (per-tick damping factors)")]
        [Range(0.9f, 0.999f)] public float rollDamping = 0.988f;
        [Range(0.8f, 0.99f)] public float sandDamping = 0.94f;
        [Tooltip("Barely below 1: the ball slides on and on across ice. " +
            "Raised from the core default 0.997 for a more dramatic glide (2026-08-16 feel pass).")]
        [Range(0.99f, 0.9995f)] public float iceDamping = 0.9985f;

        [Header("Shot")]
        [Range(2f, 16f)] public float maxShotSpeed = 8f;
        [Tooltip("Drag distance in world units for full power.")]
        [Range(0.5f, 6f)] public float maxDragLength = 2.5f;
        [Tooltip("Power curve exponent: >1 gives fine control at low power.")]
        [Range(0.5f, 3f)] public float powerCurveExponent = 1.35f;
        [Tooltip("Drags shorter than this cancel the shot instead of firing.")]
        [Range(0.05f, 0.5f)] public float minDragLength = 0.15f;

        [Header("Walls & bumpers")]
        [Range(0.3f, 1f)] public float wallRestitution = 0.8f;
        [Range(1f, 2f)] public float bumperRestitution = 1.2f;
        [Range(2f, 16f)] public float bumperMaxExitSpeed = 8f;

        [Header("Hole")]
        [Range(0.1f, 0.3f)] public float holeRadius = 0.15f;
        [Tooltip("Ball is captured when overlapping the cup below this speed. " +
            "Raised from the core default 1.2 so medium-pace putts drop (2026-08-16 feel pass).")]
        [Range(0.4f, 3f)] public float captureSpeed = 1.5f;
        [Tooltip("How lively a too-fast ball bounces off the rim (lower = dies at the lip).")]
        [Range(0.1f, 1f)] public float rimRestitution = 0.4f;
        [Tooltip("Easy/Normal courses capture on ANY touch (no rim-out); Hard keeps a speed threshold. " +
            "The rule follows the course's rated difficulty, so replays stay identical on every device.")]
        public bool touchCaptureBelowHard = true;
        [Tooltip("How much more forgiving Hard's capture is at PLAY time than the threshold courses are " +
            "generated and solved under. 1 = exactly as strict as the proof; 1.7 lets a firm putt drop.")]
        [Range(1f, 3f)] public float hardCaptureSlack = 1.7f;

        [Header("Rest detection")]
        [Range(0.005f, 0.1f)] public float restSpeed = 0.02f;
        [Range(2, 20)] public int restTicksRequired = 6;

        /// <summary>Quantizes a feel float onto the fixed 1/10000 grid (the determinism boundary).</summary>
        public static Fix64 Quantize(float value)
            => Fix64.FromFraction(Mathf.RoundToInt(value * 10000f), 10000);

        /// <summary>
        /// The config the ball is PLAYED under. Generation and solving always
        /// use <see cref="BuildSimConfig"/> (the stricter threshold rule), so
        /// the solvability proof holds; play may then relax capture to
        /// touch-anything on Easy/Normal courses. The rated difficulty is a
        /// deterministic function of the seed, so this stays replay-safe.
        /// </summary>
        public SimConfig BuildPlayConfig(SimConfig baseConfig, PuttSeed.Core.CourseGen.Difficulty difficulty)
        {
            if (!touchCaptureBelowHard)
            {
                return baseConfig;
            }

            if (difficulty == PuttSeed.Core.CourseGen.Difficulty.Hard)
            {
                // Hard keeps a lip: a ball CAN still arrive too fast to drop,
                // which is most of what makes a hard hole hard. It stopped
                // being all-or-nothing, though — the threshold the course was
                // proven solvable under is a floor, not a ceiling, and a firm
                // putt that finds the middle of the cup now falls in. Relaxing
                // capture can only ever add ways to finish, so the proof holds.
                var relaxed = Quantize(captureSpeed * Mathf.Max(1f, hardCaptureSlack));
                return baseConfig.WithHoleCapture(relaxed * relaxed);
            }

            // Any overlap captures: a threshold far above any reachable speed².
            return baseConfig.WithHoleCapture(Fix64.FromInt(1_000_000));
        }

        /// <summary>Builds the deterministic sim config from the current knob values.</summary>
        public SimConfig BuildSimConfig()
        {
            var capture = Quantize(captureSpeed);
            var rest = Quantize(restSpeed);
            return SimConfig.Create(
                dt: Fix64.FromFraction(1, 120),
                ballRadius: Fix64.FromFraction(1, 10),
                maxShotSpeed: Quantize(maxShotSpeed),
                rollDamping: Quantize(rollDamping),
                sandDamping: Quantize(sandDamping),
                iceDamping: Quantize(iceDamping),
                wallRestitution: Quantize(wallRestitution),
                maxTravelPerSubStep: Fix64.FromFraction(1, 20),
                bumperRestitution: Quantize(bumperRestitution),
                bumperMaxExitSpeed: Quantize(bumperMaxExitSpeed),
                holeRadius: Quantize(holeRadius),
                holeCaptureSpeedSq: capture * capture,
                rimRestitution: Quantize(rimRestitution),
                restSpeedEpsSq: rest * rest,
                restTicksRequired: restTicksRequired);
        }
    }
}
