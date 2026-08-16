#nullable enable
using System.Collections.Generic;
using PuttSeed.Core.Sim;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// GDD controls: drag anywhere; an aim line appears from the ball opposite
    /// to the drag (slingshot); release fires. The line doubles as the power
    /// bar (length and color). Only forwards quantized input to the sim.
    /// In teaching contexts a dotted preview shows the TRUE trajectory up to
    /// the first impact — a throwaway GolfSim replays the exact quantized shot
    /// that release would fire, so the dots never lie.
    /// </summary>
    public sealed class DragAimController : MonoBehaviour
    {
        private const int PreviewMaxTicks = 110;
        private const int PreviewSampleEvery = 6;
        private const int MaxDots = 18;

        /// <summary>Preview policy hook (wired by the bootstrap); null = never.</summary>
        public System.Func<bool>? previewAllowed;

        private SimRunner _runner = null!;
        private Camera _camera = null!;
        private LineRenderer _line = null!;

        private bool _dragging;
        private Vector2 _dragStartWorld;
        private Vector2 _aim;

        private GameObject[] _dots = System.Array.Empty<GameObject>();
        private readonly List<Vector3> _previewPoints = new List<Vector3>();
        private ShotInput _lastPreviewShot;
        private bool _previewValid;

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

            var dotMesh = MeshFactory.Disc(Vector2.zero, 0.055f, new Color(0.97f, 0.96f, 0.90f, 0.55f));
            _dots = new GameObject[MaxDots];
            for (int i = 0; i < MaxDots; i++)
            {
                _dots[i] = MeshFactory.CreateMeshObject(transform, $"PreviewDot{i}", dotMesh, -0.45f);
                _dots[i].SetActive(false);
            }
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
                UpdatePreview(canAim);
            }

            if (_dragging && PointerReleased())
            {
                _dragging = false;
                _line.enabled = false;
                HidePreview();
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
                HidePreview();
            }
        }

        /// <summary>
        /// Re-simulates the would-be shot on a throwaway sim (same course,
        /// same play config, RestoreRest for a bit-identical launch state) and
        /// lays dots along the path until the first impact or ~a second out.
        /// Recomputes only when the QUANTIZED shot changes — dragging within
        /// one angle/power step costs nothing.
        /// </summary>
        private void UpdatePreview(bool canAim)
        {
            var sim = _runner.Sim;
            var gen = _runner.Generation;
            if (!canAim || sim == null || gen == null || previewAllowed == null || !previewAllowed())
            {
                HidePreview();
                return;
            }

            var feel = _runner.feel;
            float maxDrag = feel != null ? feel.maxDragLength : 2.5f;
            float exponent = feel != null ? feel.powerCurveExponent : 1.35f;
            float minDrag = feel != null ? feel.minDragLength : 0.15f;
            if (_aim.magnitude < minDrag)
            {
                HidePreview();
                return;
            }

            var shot = InputQuantizer.FromDrag(_aim, maxDrag, exponent);
            if (!_previewValid || shot.AngleIndex != _lastPreviewShot.AngleIndex
                || shot.PowerIndex != _lastPreviewShot.PowerIndex)
            {
                ComputePreview(sim, gen, shot);
                _lastPreviewShot = shot;
                _previewValid = true;
            }

            for (int i = 0; i < _dots.Length; i++)
            {
                bool on = i < _previewPoints.Count;
                _dots[i].SetActive(on);
                if (on)
                {
                    _dots[i].transform.position = _previewPoints[i];
                }
            }
        }

        private void ComputePreview(PuttSeed.Core.Sim.GolfSim sim,
            PuttSeed.Core.CourseGen.GenerationResult gen, ShotInput shot)
        {
            _previewPoints.Clear();
            var preview = new PuttSeed.Core.Sim.GolfSim(gen.Course, _runner.PlayConfig);
            preview.RestoreRest(sim.Ball.Position, sim.Strokes);
            preview.Shoot(shot);

            var prev = FixView.ToVector2(preview.Ball.Position);
            for (int t = 1; t <= PreviewMaxTicks && _previewPoints.Count < MaxDots; t++)
            {
                preview.Tick();
                var cur = FixView.ToVector2(preview.Ball.Position);
                bool impact = preview.WallHitCount > 0 || preview.BumperHitCount > 0
                    || preview.WaterEntryCount > 0 || preview.IsHoled;
                if (impact)
                {
                    // Water teleports the ball back within the tick — the last
                    // pre-impact position is the honest end of the preview.
                    _previewPoints.Add(new Vector3(prev.x, prev.y, -0.45f));
                    break;
                }

                if (t % PreviewSampleEvery == 0)
                {
                    _previewPoints.Add(new Vector3(cur.x, cur.y, -0.45f));
                }

                if (preview.IsAtRest)
                {
                    break;
                }

                prev = cur;
            }
        }

        private void HidePreview()
        {
            _previewValid = false;
            for (int i = 0; i < _dots.Length; i++)
            {
                _dots[i].SetActive(false);
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
