#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// The hole's flag: a pole and red pennant matching the menu emblem. Pure
    /// presentation — it lifts smoothly out of the cup while the ball is close
    /// (or sunk) so the target never hides the action.
    /// </summary>
    public sealed class FlagView : MonoBehaviour
    {
        private const float RaiseDistance = 1.2f;
        private const float RaiseHeight = 0.55f;
        private const float RaiseSpeed = 7f;

        /// <summary>When set, the grow-in entrance waits for the cover to lift.</summary>
        public LoadingOverlay? overlay;

        private SimRunner _runner = null!;
        private GameObject? _flagRoot;
        private Transform? _pennant;
        private LineRenderer? _holePulse;
        private Vector2 _holePosition;
        private float _raise;
        private float _droop;
        private float _pulsePhase;
        private object? _builtFor;

        /// <summary>Wires the runner; visuals build on each course load.</summary>
        public void Initialize(SimRunner runner)
        {
            _runner = runner;
        }

        private void LateUpdate()
        {
            var gen = _runner.Generation;
            var sim = _runner.Sim;
            if (gen == null || sim == null)
            {
                return;
            }

            if (!ReferenceEquals(_builtFor, gen))
            {
                Build(gen);
            }

            bool ballClose = sim.IsHoled
                || Vector2.Distance(_runner.BallRenderPosition, _holePosition) < RaiseDistance;
            float target = ballClose ? 1f : 0f;
            _raise = Mathf.MoveTowards(_raise, target, Time.deltaTime * RaiseSpeed);

            // Out of strokes: the flag gives up. It leans off the cup and the
            // pennant stops waving — the failure moment had nothing to look at.
            _droop = Mathf.MoveTowards(_droop, sim.IsFailed ? 1f : 0f,
                Time.deltaTime * (sim.IsFailed ? 2.4f : 6f));
            float drooped = Mathf.SmoothStep(0f, 1f, _droop);

            if (_flagRoot != null)
            {
                float eased = Mathf.SmoothStep(0f, 1f, _raise);
                _flagRoot.transform.position = new Vector3(
                    _holePosition.x,
                    _holePosition.y + eased * RaiseHeight - drooped * 0.05f,
                    -0.055f);
                _flagRoot.transform.localEulerAngles = new Vector3(0f, 0f, -16f * drooped);
            }

            // The pennant waves gently, a touch livelier while raised.
            if (_pennant != null)
            {
                float wave = (Mathf.Sin(Time.time * 2.4f) * (3f + _raise * 3f)
                    + Mathf.Sin(Time.time * 5.1f) * 1.2f) * (1f - drooped);
                _pennant.localEulerAngles = new Vector3(0f, 0f, wave);
            }

            // The cup breathes while the ball hunts it (never once it's in).
            if (_holePulse != null)
            {
                bool pulse = ballClose && !sim.IsHoled;
                _holePulse.enabled = pulse;
                if (pulse)
                {
                    _pulsePhase += Time.deltaTime * 5f;
                    float wave = (Mathf.Sin(_pulsePhase) + 1f) * 0.5f;
                    float radius = 0.19f + wave * 0.03f;
                    var color = new Color(0.05f, 0.07f, 0.06f, 0.35f + wave * 0.25f);
                    _holePulse.startColor = color;
                    _holePulse.endColor = color;
                    for (int i = 0; i < 32; i++)
                    {
                        float angle = i * 2f * Mathf.PI / 32f;
                        _holePulse.SetPosition(i, new Vector3(
                            _holePosition.x + Mathf.Cos(angle) * radius,
                            _holePosition.y + Mathf.Sin(angle) * radius,
                            -0.031f));
                    }
                }
            }
        }

        private void Build(PuttSeed.Core.CourseGen.GenerationResult gen)
        {
            _builtFor = gen;
            _holePosition = FixView.ToVector2(gen.Course.HolePosition);
            _raise = 0f;
            _droop = 0f;

            if (_flagRoot != null)
            {
                Destroy(_flagRoot);
            }

            _flagRoot = new GameObject("FlagRoot");
            _flagRoot.transform.SetParent(transform, false);

            // Shadow, pole and pennant are all local to the root so the whole
            // flag lifts as one piece.
            var shadow = MeshFactory.CreateMeshObject(_flagRoot.transform, "PoleShadow",
                MeshFactory.Quad(new Vector2(-0.018f, 0f), new Vector2(0.018f, 0.85f), PaletteMaterials.Shadow), 0.004f);
            shadow.transform.localPosition = new Vector3(0.05f, -0.06f, 0.004f);

            MeshFactory.CreateMeshObject(_flagRoot.transform, "Pole",
                MeshFactory.Quad(new Vector2(-0.018f, 0f), new Vector2(0.018f, 0.85f),
                    new Color(0.92f, 0.90f, 0.85f)), 0f);

            // The pennant pivots at its pole attachment so it can wave.
            var pennant = MeshFactory.CreateMeshObject(_flagRoot.transform, "Pennant",
                MeshFactory.Triangle(
                    new Vector2(0f, 0.11f),
                    new Vector2(0.4f, 0f),
                    new Vector2(0f, -0.11f), PaletteMaterials.FlagColor), -0.002f);
            pennant.transform.localPosition = new Vector3(0.018f, 0.74f, -0.002f);
            _pennant = pennant.transform;

            _flagRoot.transform.position = new Vector3(_holePosition.x, _holePosition.y, -0.055f);

            if (_holePulse == null)
            {
                var pulseGo = new GameObject("HolePulse");
                pulseGo.transform.SetParent(transform, false);
                _holePulse = pulseGo.AddComponent<LineRenderer>();
                _holePulse.loop = true;
                _holePulse.positionCount = 32;
                _holePulse.widthMultiplier = 0.03f;
                _holePulse.material = PaletteMaterials.Shared;
                _holePulse.sortingOrder = 5;
                _holePulse.enabled = false;
            }

            StartCoroutine(GrowIn(_flagRoot));
        }

        /// <summary>The flag pops up last, after the course has faded in.</summary>
        private System.Collections.IEnumerator GrowIn(GameObject flagRoot)
        {
            flagRoot.transform.localScale = Vector3.zero;
            while (overlay != null && overlay.IsShown)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.3f);
            const float grow = 0.22f;
            for (float t = 0f; t < grow && flagRoot != null; t += Time.deltaTime)
            {
                float k = t / grow;
                float scale = k < 0.75f
                    ? Mathf.SmoothStep(0f, 1.08f, k / 0.75f)  // slight overshoot
                    : Mathf.Lerp(1.08f, 1f, (k - 0.75f) / 0.25f);
                flagRoot.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            if (flagRoot != null)
            {
                flagRoot.transform.localScale = Vector3.one;
            }
        }
    }
}
