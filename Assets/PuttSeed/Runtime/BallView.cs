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

        private MeshRenderer _renderer = null!;

        /// <summary>Creates the ball visuals and subscribes to run resets.</summary>
        public void Initialize(SimRunner runner)
        {
            _runner = runner;

            var mesh = MeshFactory.Disc(Vector2.zero, 0.1f, PaletteMaterials.Ball);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = PaletteMaterials.Shared;
            // Invisible until the first course is actually loaded — a ball
            // floating over an empty field must never render.
            _renderer.enabled = false;

            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.time = 0.6f;
            _trail.startWidth = 0.09f;
            _trail.endWidth = 0.01f;
            _trail.material = PaletteMaterials.Shared;
            _trail.startColor = new Color(1f, 1f, 1f, 0.5f);
            _trail.endColor = new Color(1f, 1f, 1f, 0f);
            _trail.sortingOrder = -1;

            _trail.enabled = false;

            // Soft drop shadow trailing the ball down-right, one layer behind.
            var shadowGo = new GameObject("BallShadow");
            shadowGo.transform.SetParent(transform, false);
            shadowGo.transform.localPosition = new Vector3(0.04f, -0.05f, 0.012f);
            shadowGo.AddComponent<MeshFilter>().sharedMesh =
                MeshFactory.Disc(Vector2.zero, 0.1f, PaletteMaterials.Shadow);
            shadowGo.AddComponent<MeshRenderer>().sharedMaterial = PaletteMaterials.Shared;
            shadowGo.SetActive(false);

            runner.RunReset += () =>
            {
                _renderer.enabled = true;
                _trail.enabled = true;
                _trail.Clear();
                shadowGo.SetActive(true);
            };
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
