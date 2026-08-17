#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// A soft edge vignette parented to the camera: a frame mesh whose inner
    /// ring is transparent and outer ring dark, scaled every frame to the
    /// orthographic view so the darkening always hugs the screen edges.
    /// </summary>
    public sealed class VignetteView : MonoBehaviour
    {
        private const float InnerExtent = 0.68f; // where darkening begins (of half-view)
        private const float OuterExtent = 1.45f;
        private const float MaxAlpha = 0.24f;

        private Camera _cam = null!;

        /// <summary>Builds the frame mesh under the camera.</summary>
        public void Initialize(Camera cam)
        {
            _cam = cam;
            transform.SetParent(cam.transform, false);
            transform.localPosition = new Vector3(0f, 0f, 9.1f); // just before the UI

            var clear = new Color(0f, 0f, 0f, 0f);
            var dark = new Color(0f, 0f, 0f, MaxAlpha);
            var vertices = new Vector3[8];
            var colors = new Color[8];
            for (int i = 0; i < 4; i++)
            {
                float sx = (i == 1 || i == 2) ? 1f : -1f;
                float sy = i < 2 ? 1f : -1f;
                vertices[i] = new Vector3(sx * InnerExtent, sy * InnerExtent);
                colors[i] = clear;
                vertices[i + 4] = new Vector3(sx * OuterExtent, sy * OuterExtent);
                colors[i + 4] = dark;
            }

            var triangles = new int[]
            {
                0, 4, 5, 0, 5, 1,
                1, 5, 6, 1, 6, 2,
                2, 6, 7, 2, 7, 3,
                3, 7, 4, 3, 4, 0,
            };

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = PaletteMaterials.Shared;
        }

        private void LateUpdate()
        {
            float h = _cam.orthographicSize;
            transform.localScale = new Vector3(h * _cam.aspect, h, 1f);
        }
    }
}
