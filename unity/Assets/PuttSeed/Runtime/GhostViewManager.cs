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

        /// <summary>Wires the runner.</summary>
        public void Initialize(SimRunner runner)
        {
            _runner = runner;
        }

        private void LateUpdate()
        {
            if (_runner == null)
            {
                return;
            }

            var ghosts = _runner.Ghosts;
            while (_views.Count < ghosts.Count)
            {
                _views.Add(CreateGhostView(_views.Count));
            }

            while (_views.Count > ghosts.Count)
            {
                Destroy(_views[_views.Count - 1]);
                _views.RemoveAt(_views.Count - 1);
            }

            for (int i = 0; i < ghosts.Count; i++)
            {
                var p = _runner.GhostRenderPosition(ghosts[i]);
                _views[i].transform.position = new Vector3(p.x, p.y, -0.045f);
            }
        }

        private GameObject CreateGhostView(int index)
        {
            var go = new GameObject($"Ghost{index}");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Disc(Vector2.zero, 0.1f, PaletteMaterials.Ghost);
            go.AddComponent<MeshRenderer>().sharedMaterial = PaletteMaterials.Shared;

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 0.07f;
            trail.endWidth = 0.01f;
            trail.material = PaletteMaterials.Shared;
            trail.startColor = new Color(1f, 1f, 1f, 0.2f);
            trail.endColor = new Color(1f, 1f, 1f, 0f);
            trail.sortingOrder = -2;
            return go;
        }
    }
}
