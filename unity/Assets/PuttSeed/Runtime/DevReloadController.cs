#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Editor-only feel-iteration helper: pressing R reloads the current seed,
    /// re-reading FeelConfig (a load rebuilds the sim config, regenerates the
    /// course under it and resets the run). Compiled out of player builds so
    /// device behavior is untouched.
    /// </summary>
    public sealed class DevReloadController : MonoBehaviour
    {
#if UNITY_EDITOR
        private SimRunner _runner = null!;
        private CourseRenderer _courseRenderer = null!;
        private Camera _camera = null!;

        /// <summary>Wires dependencies (called by the bootstrap).</summary>
        public void Initialize(SimRunner runner, CourseRenderer courseRenderer, Camera cam)
        {
            _runner = runner;
            _courseRenderer = courseRenderer;
            _camera = cam;
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.R) || _runner == null || _runner.Generation == null)
            {
                return;
            }

            _runner.LoadSeed(_runner.Seed);
            _courseRenderer.Rebuild(_runner.Generation.Course);
            CameraFramer.Frame(_camera, _runner.Generation.Course);
            Debug.Log("PuttSeed: reloaded seed with current FeelConfig values.");
        }
#else
        /// <summary>No-op in player builds.</summary>
        public void Initialize(SimRunner runner, CourseRenderer courseRenderer, Camera cam)
        {
        }
#endif
    }
}
