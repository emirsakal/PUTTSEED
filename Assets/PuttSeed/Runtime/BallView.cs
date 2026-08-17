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
        private static readonly Color TrailDefault = new Color(1f, 1f, 1f, 0.5f);
        private static readonly Color TrailIce = new Color(0.68f, 0.85f, 1f, 0.55f);
        private static readonly Color TrailSand = new Color(0.9f, 0.82f, 0.6f, 0.5f);

        private SimRunner _runner = null!;
        private TrailRenderer _trail = null!;
        private float _squash;
        private float _popIn;
        private Transform? _spin;
        private float _spinAngle;
        private Color _trailColor = TrailDefault;

        private MeshRenderer _renderer = null!;

        /// <summary>Creates the ball visuals and subscribes to run resets.</summary>
        public void Initialize(SimRunner runner) => Initialize(runner, PaletteMaterials.Ball);

        /// <summary>Creates the ball visuals with a cosmetic skin color.</summary>
        public void Initialize(SimRunner runner, Color ballColor)
        {
            _runner = runner;

            var mesh = MeshFactory.Disc(Vector2.zero, 0.1f, ballColor);
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

            // Three faint dimples rotating with speed — sells the roll on an
            // otherwise flat disc (stylized: rate follows speed, not heading).
            var spinGo = new GameObject("Spin");
            spinGo.transform.SetParent(transform, false);
            spinGo.transform.localPosition = new Vector3(0f, 0f, -0.004f);
            _spin = spinGo.transform;
            var dimple = new Color(0.8f, 0.8f, 0.76f);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 2f * Mathf.PI / 3f;
                var dotGo = new GameObject($"Dimple{i}");
                dotGo.transform.SetParent(spinGo.transform, false);
                dotGo.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 0.048f, Mathf.Sin(angle) * 0.048f, 0f);
                dotGo.AddComponent<MeshFilter>().sharedMesh =
                    MeshFactory.Disc(Vector2.zero, 0.02f, dimple);
                dotGo.AddComponent<MeshRenderer>().sharedMaterial = PaletteMaterials.Shared;
            }

            // A fixed up-left highlight completes the sphere read (it stays
            // put while the dimples spin underneath it).
            var highlightGo = new GameObject("Highlight");
            highlightGo.transform.SetParent(transform, false);
            highlightGo.transform.localPosition = new Vector3(-0.032f, 0.032f, -0.006f);
            highlightGo.AddComponent<MeshFilter>().sharedMesh =
                MeshFactory.Disc(Vector2.zero, 0.028f, new Color(1f, 1f, 1f, 0.75f));
            highlightGo.AddComponent<MeshRenderer>().sharedMaterial = PaletteMaterials.Shared;

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

        /// <summary>Scale-in pop after a teleport (the water reset).</summary>
        public void PopIn() => _popIn = 1f;

        private void LateUpdate()
        {
            if (_runner == null || _runner.Sim == null)
            {
                return;
            }

            var p = _runner.BallRenderPosition;
            transform.position = new Vector3(p.x, p.y, -0.06f);

            if (_spin != null)
            {
                float speed = FixView.ToVector2(_runner.Sim!.Ball.Velocity).magnitude;
                _spinAngle -= speed * Time.deltaTime * 340f; // deg — wheel-rate at r=0.1
                _spin.localEulerAngles = new Vector3(0f, 0f, _spinAngle);
            }

            float pop = 1f;
            if (_popIn > 0f)
            {
                _popIn = Mathf.Max(0f, _popIn - Time.deltaTime * 5f);
                pop = Mathf.SmoothStep(0.25f, 1f, 1f - _popIn);
            }

            if (_squash > 0f)
            {
                _squash = Mathf.Max(0f, _squash - Time.deltaTime * 8f);
                float k = _squash * 0.22f;
                transform.localScale = new Vector3((1f + k) * pop, (1f - k) * pop, 1f);
            }
            else
            {
                transform.localScale = Vector3.one * pop;
            }

            // The trail borrows the ground's tone: icy blue on ice, warm tan
            // in sand, cream elsewhere — eased so transitions never snap.
            var target = InZone(_runner.Generation?.Course.IceZones) ? TrailIce
                : InZone(_runner.Generation?.Course.SandZones) ? TrailSand
                : TrailDefault;
            _trailColor = Color.Lerp(_trailColor, target, Time.deltaTime * 6f);
            _trail.startColor = _trailColor;
        }

        private bool InZone(PuttSeed.Core.Sim.ZonePolygon[]? zones)
        {
            var sim = _runner.Sim;
            if (zones == null || sim == null)
            {
                return false;
            }

            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i].Contains(sim.Ball.Position))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
