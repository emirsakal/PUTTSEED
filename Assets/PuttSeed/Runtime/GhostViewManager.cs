#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Keeps one translucent ball view per ghost in the runner, created and
    /// destroyed as the ghost list changes (GDD: ghosts render as translucent
    /// balls with a light trail).
    /// </summary>
    public sealed class GhostViewManager : MonoBehaviour
    {
        private SimRunner _runner = null!;
        private readonly List<GameObject> _views = new List<GameObject>();
        private readonly List<string> _viewLabels = new List<string>();

        /// <summary>Wires the runner.</summary>
        public void Initialize(SimRunner runner)
        {
            _runner = runner;
        }

        /// <summary>Ghost tint by role, so several ghosts stay tellable apart.</summary>
        public static Color ColorFor(string label) => label switch
        {
            "author" => new Color(1f, 0.9f, 0.5f, 0.38f),   // amber: the generator's line
            "best" => new Color(0.55f, 0.8f, 1f, 0.38f),    // blue: your best run
            "import" => new Color(1f, 0.6f, 0.75f, 0.38f),  // pink: someone else's run
            _ => PaletteMaterials.Ghost,
        };

        private void LateUpdate()
        {
            if (_runner == null)
            {
                return;
            }

            var ghosts = _runner.Ghosts;
            while (_views.Count > ghosts.Count)
            {
                Destroy(_views[_views.Count - 1]);
                _views.RemoveAt(_views.Count - 1);
                _viewLabels.RemoveAt(_viewLabels.Count - 1);
            }

            for (int i = 0; i < ghosts.Count; i++)
            {
                // Removals can shift roles onto existing indices — rebuild any
                // view whose label no longer matches its ghost.
                if (i < _views.Count && _viewLabels[i] != ghosts[i].Label)
                {
                    Destroy(_views[i]);
                    _views[i] = CreateGhostView(i, ghosts[i].Label);
                    _viewLabels[i] = ghosts[i].Label;
                }
                else if (i >= _views.Count)
                {
                    _views.Add(CreateGhostView(i, ghosts[i].Label));
                    _viewLabels.Add(ghosts[i].Label);
                }

                var p = _runner.GhostRenderPosition(ghosts[i]);
                _views[i].transform.position = new Vector3(p.x, p.y, -0.045f);
            }
        }

        private GameObject CreateGhostView(int index, string label)
        {
            var color = ColorFor(label);
            var go = new GameObject($"Ghost{index}");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Disc(Vector2.zero, 0.1f, color);
            var ghostRenderer = go.AddComponent<MeshRenderer>();
            ghostRenderer.sharedMaterial = PaletteMaterials.Shared;
            ghostRenderer.sortingOrder = SortingLayers.GhostBall;

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 0.07f;
            trail.endWidth = 0.01f;
            trail.material = PaletteMaterials.Shared;
            trail.startColor = new Color(color.r, color.g, color.b, 0.2f);
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.sortingOrder = SortingLayers.GhostTrail;
            return go;
        }
    }
}
