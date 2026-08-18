#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Rotates a windmill's blade container to mirror the SIM's blade phase
    /// exactly: angle = (phase0 + omega * ticks-since-shot) in 1024-step
    /// units. No wall-clock time is involved — the mill turns only while the
    /// ball rolls, and freezes the moment it rests, exactly like the physics.
    /// </summary>
    public sealed class WindmillView : MonoBehaviour
    {
        private SimRunner? _runner;
        private int _phase0;
        private int _omegaSteps;

        /// <summary>Binds the view to a mill's parameters and the live sim.</summary>
        public void Initialize(SimRunner runner, int phase0, int omegaSteps)
        {
            _runner = runner;
            _phase0 = phase0;
            _omegaSteps = omegaSteps;
            Apply(0);
        }

        private void Update()
        {
            Apply(_runner != null && _runner.Sim != null ? _runner.Sim.MillClock : 0);
        }

        private void Apply(int ticks)
        {
            float degrees = (_phase0 + (long)_omegaSteps * ticks) % 1024L * (360f / 1024f);
            transform.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }
    }
}
