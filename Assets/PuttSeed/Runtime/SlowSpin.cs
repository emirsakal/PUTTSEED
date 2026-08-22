#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Turns a thing slowly and forever — the portal ring's "I am active".
    /// A degrees-per-second and nothing else; anything that wants to spin
    /// with meaning (the windmill) has its own component reading the sim.
    /// </summary>
    public sealed class SlowSpin : MonoBehaviour
    {
        /// <summary>Degrees per second; sign picks the direction.</summary>
        public float degreesPerSecond = 24f;

        private void LateUpdate()
        {
            transform.localEulerAngles = new Vector3(0f, 0f,
                transform.localEulerAngles.z + degreesPerSecond * Time.deltaTime);
        }
    }
}
