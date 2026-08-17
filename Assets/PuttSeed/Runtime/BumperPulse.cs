#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// A barely-there idle breath on a bumper disc — springy things should
    /// look alive before they are ever hit. Phases are staggered per bumper.
    /// </summary>
    public sealed class BumperPulse : MonoBehaviour
    {
        /// <summary>Per-bumper phase offset so they never pulse in unison.</summary>
        public float phase;

        private void LateUpdate()
        {
            float scale = 1f + 0.022f * Mathf.Sin(Time.time * 1.3f + phase);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
