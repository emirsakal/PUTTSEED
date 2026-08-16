#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Makes "you can shoot now" unmissable: the instant the ball settles and
    /// aiming becomes possible, a ring pops outward from the ball; while the
    /// game then waits for input, a soft halo breathes around it. Both hide
    /// during drags (the aim line takes over) and whenever the ball moves.
    /// </summary>
    public sealed class ReadyIndicator : MonoBehaviour
    {
        private const int Segments = 40;
        private const float HaloRadius = 0.23f;
        private const float PopDuration = 0.4f;

        private static readonly Color HaloColor = new Color(1f, 0.95f, 0.7f, 1f);

        private SimRunner _runner = null!;
        private DragAimController _drag = null!;
        private LineRenderer _halo = null!;
        private LineRenderer _pop = null!;
        private bool _wasReady;
        private float _popTime = float.MaxValue;
        private float _breathe;

        /// <summary>Wires dependencies (called by the bootstrap).</summary>
        public void Initialize(SimRunner runner, DragAimController drag)
        {
            _runner = runner;
            _drag = drag;
            _halo = CreateRing("ReadyHalo");
            _pop = CreateRing("ReadyPop");
        }

        private LineRenderer CreateRing(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.loop = true;
            line.positionCount = Segments;
            line.widthMultiplier = 0.035f;
            line.material = PaletteMaterials.Shared;
            line.sortingOrder = 12;
            line.enabled = false;
            return line;
        }

        private void LateUpdate()
        {
            var sim = _runner.Sim;
            bool ready = sim != null && sim.IsAtRest && !sim.IsHoled && !sim.IsFailed;

            if (ready && !_wasReady)
            {
                _popTime = 0f; // the settle moment — fire the one-shot ring
            }

            _wasReady = ready;
            var ball = _runner.BallRenderPosition;

            // One-shot pop: a ring expanding out of the ball, fading as it goes.
            if (_popTime < PopDuration && ready)
            {
                _popTime += Time.deltaTime;
                float k = Mathf.Clamp01(_popTime / PopDuration);
                DrawRing(_pop, ball, Mathf.Lerp(0.12f, 0.5f, Mathf.Sqrt(k)),
                    HaloColor.a * (1f - k) * 0.9f);
            }
            else
            {
                _pop.enabled = false;
            }

            // Breathing halo while waiting for input; the drag takes over the
            // "you are acting" read, so the halo yields to it.
            bool showHalo = ready && !_drag.IsDragging;
            if (showHalo)
            {
                _breathe += Time.deltaTime * 2.6f;
                float wave = (Mathf.Sin(_breathe) + 1f) * 0.5f;
                DrawRing(_halo, ball, HaloRadius + wave * 0.035f,
                    Mathf.Lerp(0.14f, 0.32f, wave));
            }
            else
            {
                _halo.enabled = false;
            }
        }

        private static void DrawRing(LineRenderer line, Vector2 center, float radius, float alpha)
        {
            line.enabled = true;
            var color = new Color(HaloColor.r, HaloColor.g, HaloColor.b, alpha);
            line.startColor = color;
            line.endColor = color;
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * 2f * Mathf.PI / Segments;
                line.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius,
                    -0.052f));
            }
        }
    }
}
