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
    ///
    /// Wide holes are also ROLLED: a course wider than it is tall gets turned
    /// 90°, so the long side of the phone is spent on the long side of the
    /// hole instead of on empty felt. (Built 2026-08-19, removed the same day,
    /// restored on request.) The roll is rendering only — the drag is read
    /// through the camera before it is quantized, so the same gesture yields
    /// the same shot index at either orientation, and shots, replays and the
    /// simulation never learn about it.
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

        /// <summary>The view roll for a course: 90° when it is wider than tall.</summary>
        public static float RollFor(Vector2 halfSize) => halfSize.x > halfSize.y ? 90f : 0f;

        /// <summary>
        /// Half the view height needed to hold a course inside the free band.
        /// "Along" is the world axis the screen's HEIGHT measures and "across"
        /// the one its width measures; the roll swaps which is which, and that
        /// swap is the whole trick. The band is a fraction of screen height, so
        /// a course limited by its long axis pays for the chrome and one
        /// limited across pays nothing.
        /// </summary>
        public static float OrthographicSizeFor(Vector2 halfSize, float aspect, float band, bool rolled)
        {
            aspect = Mathf.Max(aspect, 0.01f);
            band = Mathf.Clamp(band, 0.2f, 1f);
            float halfAlong = rolled ? halfSize.x : halfSize.y;
            float halfAcross = rolled ? halfSize.y : halfSize.x;
            return Mathf.Max((halfAlong + Padding) / band, (halfAcross + Padding) / aspect);
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

            float roll = RollFor(halfSize);
            cam.orthographic = true;
            cam.transform.rotation = Quaternion.Euler(0f, 0f, roll);

            float size = OrthographicSizeFor(halfSize, cam.aspect, topFraction - BottomChrome, roll != 0f);
            cam.orthographicSize = size;

            // The band offset is a SCREEN measurement, so it travels along the
            // camera's own up axis — which the roll has already turned.
            var offset = cam.transform.up * CameraOffsetFor(size, BottomChrome, topFraction);
            cam.transform.position = new Vector3(center.x + offset.x, center.y + offset.y, -10f);
            cam.backgroundColor = PaletteMaterials.Rough;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
