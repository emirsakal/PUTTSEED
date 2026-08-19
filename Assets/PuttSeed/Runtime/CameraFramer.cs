#nullable enable
using PuttSeed.Core.Sim;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Frames the orthographic camera on the course bounds, aspect-aware, once
    /// per course load — inside the band of screen the UI leaves free.
    ///
    /// The course used to be fitted to the WHOLE screen, so its edges ran under
    /// the top bar and the button row, and a ball resting near the bottom of a
    /// tall hole could sit beneath a button. That is worse than untidy: the
    /// button eats the touch that was meant to aim.
    /// </summary>
    public sealed class CameraFramer : MonoBehaviour
    {
        private const float Padding = 0.8f;

        /// <summary>Screen fraction reserved at the bottom: the button row.</summary>
        public const float BottomChrome = 0.09f;

        /// <summary>Screen fraction the course may reach up to: under the top bar.</summary>
        public const float TopChrome = 0.92f;

        /// <summary>The same, when a hint chip is riding under the top bar.</summary>
        public const float TopChromeWithHint = 0.855f;

        /// <summary>
        /// Half the view height needed to hold a course inside the free band.
        /// The band is a fraction of screen HEIGHT, so a course that is height
        /// limited pays for the chrome and a wide one — limited by width —
        /// pays nothing.
        /// </summary>
        public static float OrthographicSizeFor(Vector2 halfSize, float aspect, float band)
        {
            aspect = Mathf.Max(aspect, 0.01f);
            band = Mathf.Clamp(band, 0.2f, 1f);
            return Mathf.Max((halfSize.y + Padding) / band, (halfSize.x + Padding) / aspect);
        }

        /// <summary>
        /// How far the camera sits from the course centre so the course lands
        /// in the middle of the free band rather than the middle of the glass.
        /// </summary>
        public static float CameraOffsetFor(float size, float bottomFraction, float topFraction)
            => size * (1f - (topFraction + bottomFraction));

        /// <summary>Positions and sizes the camera to contain every wall.</summary>
        public static void Frame(Camera cam, CourseData course, float topFraction = TopChrome)
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
            cam.transform.rotation = Quaternion.identity;
            float size = OrthographicSizeFor(halfSize, cam.aspect, topFraction - BottomChrome);
            cam.orthographicSize = size;
            cam.transform.position = new Vector3(
                center.x,
                center.y + CameraOffsetFor(size, BottomChrome, topFraction),
                -10f);
            cam.backgroundColor = PaletteMaterials.Rough;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
