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

        /// <summary>Aim preference hook: true = drag toward the target, false
        /// (default) = slingshot. Wired from the saved setting.</summary>
        public System.Func<bool>? aimDirect;

        /// <summary>True while the player is mid-drag (ready halo yields).</summary>
        public bool IsDragging => _dragging;

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

        private const int MaxAimDashes = 14;
        private GameObject[] _aimDashes = System.Array.Empty<GameObject>();
        private MeshRenderer[] _aimDashRenderers = System.Array.Empty<MeshRenderer>();

        private MaterialPropertyBlock _tintBlock = null!; // Unity API — created in Initialize, never in the ctor

        // The aim colour rides power continuously. A stepped ladder (notches
        // at 25/50/75/100%, then the same quarters as colour snaps) was built
        // and tried on device: the bars fought the arrow and the snapping read
        // worse than the smooth climb. Kept smooth deliberately (2026-08-18).
        // The ramp's two ends live in PaletteMaterials now, because they have
        // to change with the colorblind setting — see PowerLow there.

        /// <summary>
        /// The aim inside the cancel zone. Releasing a drag shorter than
        /// minDragLength fires nothing, and nothing used to SAY so — the line
        /// stayed green and eager right up to the release that ignored it.
        /// Grey is the universal color of "this will not happen".
        /// </summary>
        private static readonly Color CancelTint = new Color(0.72f, 0.74f, 0.72f, 0.42f);

        /// <summary>Wires dependencies (called by the bootstrap).</summary>
        public void Initialize(SimRunner runner, Camera cam)
        {
            _runner = runner;
            _camera = cam;
            // The line renderer now draws only the arrowhead (a 3-point V);
            // the shaft is a run of dashes so the aim reads as designed intent.
            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = 3;
            _line.startWidth = 0.06f;
            _line.endWidth = 0.06f;
            _line.material = PaletteMaterials.Shared;
            _line.sortingOrder = 10;
            _line.enabled = false;

            _tintBlock = new MaterialPropertyBlock();
            var dashMesh = MeshFactory.Disc(Vector2.zero, 0.04f, Color.white);
            _aimDashes = new GameObject[MaxAimDashes];
            _aimDashRenderers = new MeshRenderer[MaxAimDashes];
            for (int i = 0; i < MaxAimDashes; i++)
            {
                _aimDashes[i] = MeshFactory.CreateMeshObject(transform, $"AimDash{i}", dashMesh, -0.5f);
                _aimDashRenderers[i] = _aimDashes[i].GetComponent<MeshRenderer>();
                _aimDashes[i].SetActive(false);
            }

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
                if (aimDirect != null && aimDirect())
                {
                    _aim = -_aim; // direct: the drag points where the ball goes
                }

                DrawAimLine();
                UpdatePreview(canAim);
            }

            if (_dragging && PointerReleased())
            {
                _dragging = false;
                _line.enabled = false;
                HideAimDashes();
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
                HideAimDashes();
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

            // The trajectory dots pick up BEYOND the power arrow's tip — the
            // two dotted lines must never overlap and blur into each other.
            float power = InputQuantizer.PowerFraction(_aim, maxDrag, exponent);
            float cutoff = 0.4f + power * 1.8f + 0.18f;
            float cutoffSq = cutoff * cutoff;
            var ball = _runner.BallRenderPosition;
            int visible = 0;
            for (int i = 0; i < _previewPoints.Count && visible < _dots.Length; i++)
            {
                var p = new Vector2(_previewPoints[i].x, _previewPoints[i].y);
                if ((p - ball).sqrMagnitude <= cutoffSq)
                {
                    continue;
                }

                _dots[visible].SetActive(true);
                _dots[visible].transform.position = _previewPoints[i];
                visible++;
            }

            for (int i = visible; i < _dots.Length; i++)
            {
                _dots[i].SetActive(false);
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
            float minDrag = feel != null ? feel.minDragLength : 0.15f;
            var color = _aim.magnitude < minDrag
                ? CancelTint
                : Color.Lerp(PaletteMaterials.PowerLow, PaletteMaterials.PowerHigh, power);
            var tip = ball + dir * length;

            // Dashed shaft: evenly spaced dots from the ball to just short of
            // the arrowhead, tinted by power.
            _tintBlock.SetColor("_Color", color);
            int dashes = Mathf.Clamp(Mathf.FloorToInt((length - 0.25f) / 0.16f), 2, MaxAimDashes);
            for (int i = 0; i < _aimDashes.Length; i++)
            {
                bool on = i < dashes;
                _aimDashes[i].SetActive(on);
                if (on)
                {
                    float d = 0.22f + i * 0.16f;
                    _aimDashes[i].transform.position = new Vector3(
                        ball.x + dir.x * d, ball.y + dir.y * d, -0.5f);
                    _aimDashRenderers[i].SetPropertyBlock(_tintBlock);
                }
            }

            // Arrowhead: a V at the tip pointing along the aim.
            var perp = new Vector2(-dir.y, dir.x);
            var back = tip - dir * 0.2f;
            _line.enabled = true;
            _line.SetPosition(0, new Vector3(back.x + perp.x * 0.13f, back.y + perp.y * 0.13f, -0.5f));
            _line.SetPosition(1, new Vector3(tip.x, tip.y, -0.5f));
            _line.SetPosition(2, new Vector3(back.x - perp.x * 0.13f, back.y - perp.y * 0.13f, -0.5f));
            _line.startColor = color;
            _line.endColor = color;
        }

        private void HideAimDashes()
        {
            for (int i = 0; i < _aimDashes.Length; i++)
            {
                _aimDashes[i].SetActive(false);
            }
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
