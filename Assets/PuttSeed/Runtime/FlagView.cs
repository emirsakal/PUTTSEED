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

        private SimRunner _runner = null!;
        private GameObject? _flagRoot;
        private Vector2 _holePosition;
        private float _raise;
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
            if (_flagRoot != null)
            {
                float eased = Mathf.SmoothStep(0f, 1f, _raise);
                _flagRoot.transform.position = new Vector3(
                    _holePosition.x, _holePosition.y + eased * RaiseHeight, -0.055f);
            }
        }

        private void Build(PuttSeed.Core.CourseGen.GenerationResult gen)
        {
            _builtFor = gen;
            _holePosition = FixView.ToVector2(gen.Course.HolePosition);
            _raise = 0f;

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

            MeshFactory.CreateMeshObject(_flagRoot.transform, "Pennant",
                MeshFactory.Triangle(
                    new Vector2(0.018f, 0.85f),
                    new Vector2(0.42f, 0.74f),
                    new Vector2(0.018f, 0.63f), PaletteMaterials.Flag), -0.002f);

            _flagRoot.transform.position = new Vector3(_holePosition.x, _holePosition.y, -0.055f);
        }
    }
}
