using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// The input quantization boundary: converts an analog slingshot drag into
    /// the discrete <see cref="ShotInput"/> the sim accepts. All rounding
    /// happens HERE, once — the sim never sees floats, which is what makes
    /// replays tiny and cross-device identical.
    /// </summary>
    public static class InputQuantizer
    {
        /// <summary>
        /// Maps an aim vector (slingshot: drag start minus current pointer) to
        /// a quantized shot. Power follows <c>t^exponent</c> over the clamped
        /// drag fraction, giving finer control near low power for exponent &gt; 1.
        /// </summary>
        public static ShotInput FromDrag(Vector2 aimVector, float maxDragLength, float powerExponent)
        {
            float angleRadians = Mathf.Atan2(aimVector.y, aimVector.x);
            int angleIndex = Mathf.RoundToInt(angleRadians / (2f * Mathf.PI) * FixTrig.AngleSteps)
                & (FixTrig.AngleSteps - 1);

            float t = maxDragLength <= 0f ? 0f : Mathf.Clamp01(aimVector.magnitude / maxDragLength);
            float curved = Mathf.Pow(t, powerExponent);
            int powerIndex = Mathf.Clamp(Mathf.RoundToInt(curved * 255f), 0, 255);

            return new ShotInput(angleIndex, powerIndex);
        }

        /// <summary>Normalized 0..1 power preview for UI (same curve as the shot).</summary>
        public static float PowerFraction(Vector2 aimVector, float maxDragLength, float powerExponent)
        {
            float t = maxDragLength <= 0f ? 0f : Mathf.Clamp01(aimVector.magnitude / maxDragLength);
            return Mathf.Pow(t, powerExponent);
        }
    }
}
