#nullable enable
using PuttSeed.Core.Sim;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Frames the orthographic camera on the course bounds with padding,
    /// aspect-aware, once per course load.
    /// </summary>
    public sealed class CameraFramer : MonoBehaviour
    {
        private const float Padding = 0.8f;

        /// <summary>Positions and sizes the camera to contain every wall.</summary>
        public static void Frame(Camera cam, CourseData course)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var wall in course.Walls)
            {
                foreach (var p in new[] { FixView.ToVector2(wall.A), FixView.ToVector2(wall.B) })
                {
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
            }

            var center = (min + max) * 0.5f;
            var halfSize = (max - min) * 0.5f;

            cam.orthographic = true;
            cam.transform.position = new Vector3(center.x, center.y, -10f);
            cam.transform.rotation = Quaternion.identity;
            float aspect = Mathf.Max(cam.aspect, 0.01f);
            cam.orthographicSize = Mathf.Max(halfSize.y + Padding, (halfSize.x + Padding) / aspect);
            cam.backgroundColor = PaletteMaterials.Felt;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
