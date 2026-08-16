#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Renders the player ball at the interpolated sim position with a fading
    /// trail. Pure presentation: reads positions, never touches the sim.
    /// </summary>
    public sealed class BallView : MonoBehaviour
    {
        private SimRunner _runner = null!;
        private TrailRenderer _trail = null!;
        private float _squash;

        /// <summary>Creates the ball visuals and subscribes to run resets.</summary>
        public void Initialize(SimRunner runner)
        {
            _runner = runner;

            var mesh = MeshFactory.Disc(Vector2.zero, 0.1f, PaletteMaterials.Ball);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = PaletteMaterials.Shared;

            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.time = 0.6f;
            _trail.startWidth = 0.09f;
            _trail.endWidth = 0.01f;
            _trail.material = PaletteMaterials.Shared;
            _trail.startColor = new Color(1f, 1f, 1f, 0.5f);
            _trail.endColor = new Color(1f, 1f, 1f, 0f);
            _trail.sortingOrder = -1;

            runner.RunReset += () => _trail.Clear();
        }

        /// <summary>Impact juice: a brief squash pulse that eases back to round.</summary>
        public void Squash() => _squash = 1f;

        private void LateUpdate()
        {
            if (_runner == null || _runner.Sim == null)
            {
                return;
            }

            var p = _runner.BallRenderPosition;
            transform.position = new Vector3(p.x, p.y, -0.06f);

            if (_squash > 0f)
            {
                _squash = Mathf.Max(0f, _squash - Time.deltaTime * 8f);
                float k = _squash * 0.22f;
                transform.localScale = new Vector3(1f + k, 1f - k, 1f);
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }
    }
}
