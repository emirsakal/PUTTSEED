#nullable enable
using PuttSeed.Core.Sim;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// GDD controls: drag anywhere; an aim line appears from the ball opposite
    /// to the drag (slingshot); release fires. The line doubles as the power
    /// bar (length and color). Only forwards quantized input to the sim.
    /// </summary>
    public sealed class DragAimController : MonoBehaviour
    {
        private SimRunner _runner = null!;
        private Camera _camera = null!;
        private LineRenderer _line = null!;

        private bool _dragging;
        private Vector2 _dragStartWorld;
        private Vector2 _aim;

        private static readonly Color LowPower = new Color(0.55f, 0.9f, 0.55f);
        private static readonly Color HighPower = new Color(0.95f, 0.35f, 0.3f);

        /// <summary>Wires dependencies (called by the bootstrap).</summary>
        public void Initialize(SimRunner runner, Camera cam)
        {
            _runner = runner;
            _camera = cam;
            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.startWidth = 0.07f;
            _line.endWidth = 0.02f;
            _line.material = PaletteMaterials.Shared;
            _line.sortingOrder = 10;
            _line.enabled = false;
        }

        private void Update()
        {
            var sim = _runner != null ? _runner.Sim : null;
            if (sim == null)
            {
                return;
            }

            bool canAim = sim.IsAtRest && !sim.IsHoled && !sim.IsFailed;

            if (PointerDown() && canAim)
            {
                _dragging = true;
                _dragStartWorld = PointerWorld();
            }

            if (_dragging && PointerHeld())
            {
                _aim = _dragStartWorld - PointerWorld();
                DrawAimLine();
            }

            if (_dragging && PointerReleased())
            {
                _dragging = false;
                _line.enabled = false;
                var feel = _runner.feel;
                float maxDrag = feel != null ? feel.maxDragLength : 2.5f;
                float exponent = feel != null ? feel.powerCurveExponent : 1.35f;
                float minDrag = feel != null ? feel.minDragLength : 0.15f;
                if (_aim.magnitude >= minDrag && canAim)
                {
                    _runner.TryShoot(InputQuantizer.FromDrag(_aim, maxDrag, exponent));
                }
            }

            if (!canAim && _dragging)
            {
                _dragging = false;
                _line.enabled = false;
            }
        }

        private void DrawAimLine()
        {
            var feel = _runner.feel;
            float maxDrag = feel != null ? feel.maxDragLength : 2.5f;
            float exponent = feel != null ? feel.powerCurveExponent : 1.35f;
            float power = InputQuantizer.PowerFraction(_aim, maxDrag, exponent);

            var ball = _runner.BallRenderPosition;
            var dir = _aim.sqrMagnitude > 0.0001f ? _aim.normalized : Vector2.right;
            float length = 0.4f + power * 1.8f;

            _line.enabled = true;
            _line.SetPosition(0, new Vector3(ball.x, ball.y, -0.5f));
            _line.SetPosition(1, new Vector3(ball.x + dir.x * length, ball.y + dir.y * length, -0.5f));
            var color = Color.Lerp(LowPower, HighPower, power);
            _line.startColor = color;
            _line.endColor = color;
        }

        private Vector2 PointerWorld()
        {
            Vector3 screen = Input.touchCount > 0
                ? (Vector3)Input.GetTouch(0).position
                : Input.mousePosition;
            var world = _camera.ScreenToWorldPoint(screen);
            return new Vector2(world.x, world.y);
        }

        private static bool PointerDown()
            => Input.touchCount > 0
                ? Input.GetTouch(0).phase == TouchPhase.Began
                : Input.GetMouseButtonDown(0);

        private static bool PointerHeld()
            => Input.touchCount > 0 || Input.GetMouseButton(0);

        private static bool PointerReleased()
        {
            if (Input.touchCount > 0)
            {
                var phase = Input.GetTouch(0).phase;
                return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
            }

            return Input.GetMouseButtonUp(0);
        }
    }
}
