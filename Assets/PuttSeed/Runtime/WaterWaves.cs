#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// The only living ground: two short wave dashes drift across a water
    /// zone, fading in and out at the ends of their run. Pure presentation,
    /// destroyed with the course view on rebuild.
    /// </summary>
    public sealed class WaterWaves : MonoBehaviour
    {
        private const int DashCount = 2;

        private static readonly Color WaveColor = new Color(0.62f, 0.8f, 0.97f);

        private Vector2[] _quad = System.Array.Empty<Vector2>();
        private LineRenderer[] _dashes = System.Array.Empty<LineRenderer>();
        private float[] _phases = System.Array.Empty<float>();

        /// <summary>Sets the zone quad and builds the dash renderers.</summary>
        public void Initialize(Vector2[] quad)
        {
            _quad = quad;
            _dashes = new LineRenderer[DashCount];
            _phases = new float[DashCount];
            for (int i = 0; i < DashCount; i++)
            {
                var go = new GameObject($"Wave{i}");
                go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.widthMultiplier = 0.035f;
                line.material = PaletteMaterials.Shared;
                line.sortingOrder = 3;
                _dashes[i] = line;
                _phases[i] = i * 0.5f; // stagger the two runs
            }
        }

        private void Update()
        {
            for (int i = 0; i < _dashes.Length; i++)
            {
                _phases[i] += Time.deltaTime * 0.12f;
                float run = _phases[i] % 1f;
                float u = Mathf.Lerp(0.12f, 0.72f, run);
                float v = 0.3f + 0.4f * (i / (float)Mathf.Max(1, DashCount - 1));

                var a = CourseRenderer.Bilerp(_quad, u, v);
                var b = CourseRenderer.Bilerp(_quad, u + 0.14f, v);
                _dashes[i].SetPosition(0, new Vector3(a.x, a.y, -0.022f));
                _dashes[i].SetPosition(1, new Vector3(b.x, b.y, -0.022f));

                // Fade in over the first fifth of the run, out over the last.
                float alpha = Mathf.Clamp01(Mathf.Min(run / 0.2f, (1f - run) / 0.2f)) * 0.55f;
                var color = new Color(WaveColor.r, WaveColor.g, WaveColor.b, alpha);
                _dashes[i].startColor = color;
                _dashes[i].endColor = color;
            }
        }
    }
}
