#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// The day's wind, drawn on the course: an arrow pointing the way the air
    /// pushes, with a barb for each step of strength — the convention every
    /// weather map uses, and for the same reason.
    ///
    /// A windy day used to announce itself with two words in the top bar and
    /// then say nothing about which way. The physics were never unfair — the
    /// generator proves the hole solvable under the wind — but a force you can
    /// only discover by losing a shot to it is not a mechanic, it is a
    /// surprise. Presentation only: the vane reads the config, and the sim
    /// never learns the vane exists.
    /// </summary>
    public sealed class WindVane : MonoBehaviour
    {
        /// <summary>
        /// The badge's outer edge — the ring, which is its widest part. The
        /// clamp that keeps it out from under the top bar measures with this,
        /// so the number has to be the real extent and not the disc's.
        /// </summary>
        private const float Radius = 0.315f;

        /// <summary>Full strength for barb purposes: three barbs, no more.</summary>
        private const float StrengthPerBarb = 0.33f;

        /// <summary>
        /// The reading scale. The sim's wind is an ACCELERATION in course
        /// units, so there is no honest conversion to a weather speed — this
        /// is a dial face, not a measurement. Both units come off this one
        /// number, so the mph and the km/s always describe the same wind.
        /// </summary>
        private const float MilesPerHourPerUnit = 20f;

        private Transform? _needle;
        private Camera? _camera;
        private Vector3 _anchor;
        private float _heading;

        /// <summary>
        /// Builds the badge and points it downwind. The needle is a child
        /// built centred on the origin, so turning it turns the arrow rather
        /// than swinging it around the course (a mesh carrying absolute
        /// coordinates cannot be rotated in place).
        /// </summary>
        public void Build(Vector2 wind, Vector3 anchor)
        {
            _anchor = anchor;
            transform.position = anchor;
            var cream = PaletteMaterials.Ball;
            MeshFactory.CreateMeshObject(transform, "Badge",
                MeshFactory.Disc(Vector2.zero, 0.3f, PaletteMaterials.VaneBadge, segments: 40), 0f);
            MeshFactory.CreateMeshObject(transform, "BadgeRing",
                MeshFactory.Ring(Vector2.zero, 0.285f, Radius,
                    new Color(cream.r, cream.g, cream.b, 0.22f), segments: 44), -0.001f);

            var needleGo = new GameObject("Needle");
            needleGo.transform.SetParent(transform, false);
            needleGo.transform.localPosition = new Vector3(0f, 0f, -0.002f);
            _needle = needleGo.transform;

            MeshFactory.CreateMeshObject(needleGo.transform, "Shaft",
                MeshFactory.Quad(new Vector2(-0.17f, -0.017f), new Vector2(0.09f, 0.017f), cream), 0f);
            MeshFactory.CreateMeshObject(needleGo.transform, "Head",
                MeshFactory.Triangle(new Vector2(0.07f, -0.082f), new Vector2(0.07f, 0.082f),
                    new Vector2(0.245f, 0f), cream), 0f);

            // Strength: one barb per step, capped at three. The wind is one
            // fixed strength today, so this always draws two — it is derived
            // from the vector rather than from that fact, because the day the
            // strength varies is the day nobody remembers this line exists.
            int barbs = Mathf.Clamp(Mathf.RoundToInt(wind.magnitude / StrengthPerBarb), 1, 3);
            for (int i = 0; i < barbs; i++)
            {
                float x = -0.165f + i * 0.055f;
                MeshFactory.CreateMeshObject(needleGo.transform, "Barb",
                    MeshFactory.Quad(new Vector2(x, -0.072f), new Vector2(x + 0.024f, 0.072f), cream), 0f);
            }

            _heading = Mathf.Atan2(wind.y, wind.x) * Mathf.Rad2Deg;
            _needle.localEulerAngles = new Vector3(0f, 0f, _heading);
        }

        /// <summary>
        /// The wind as a number a person can say out loud, in the unit their
        /// language uses. Barbs answer "which way, roughly how much" at a
        /// glance; this answers "how much" exactly.
        /// </summary>
        public static string SpeedLabel(Vector2 wind)
        {
            float milesPerHour = wind.magnitude * MilesPerHourPerUnit;
            return Loc.Current == Loc.Language.Turkish
                ? Mathf.RoundToInt(milesPerHour * 1.609f) + " km/s"
                : Mathf.RoundToInt(milesPerHour) + " mph";
        }

        private void LateUpdate()
        {
            if (_needle == null)
            {
                return;
            }

            KeepOnScreen();

            // The same two-sine wobble the menu pennant waves with: a vane that
            // holds perfectly still reads as a printed icon, and the player
            // stops seeing it by the second hole.
            float t = Time.time;
            float sway = Mathf.Sin(t * 1.7f) * 3.4f + Mathf.Sin(t * 3.9f) * 1.1f;
            _needle.localEulerAngles = new Vector3(0f, 0f, _heading + sway);
        }

        /// <summary>
        /// Holds the badge inside the band of screen the UI leaves free.
        ///
        /// Its anchor is a course corner pushed out onto the grass, which is a
        /// world position — and the camera frames the course UNDER the top bar
        /// and OVER the button row, so on a course that reaches the top of its
        /// band, that grass is behind the scoreboard. Clamping happens in
        /// viewport space, which costs nothing and comes out right on the wide
        /// courses the camera rolls ninety degrees as well.
        /// </summary>
        private void KeepOnScreen()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    return;
                }
            }

            var view = _camera.WorldToViewportPoint(_anchor);
            float halfY = Radius / (2f * Mathf.Max(0.01f, _camera.orthographicSize));
            float halfX = halfY / Mathf.Max(0.01f, _camera.aspect);

            // The lower ceiling of the two the camera uses, so the badge clears
            // a hint chip on the holes that show one.
            view.x = Mathf.Clamp(view.x, 0.04f + halfX, 0.96f - halfX);
            view.y = Mathf.Clamp(view.y,
                CameraFramer.BottomChrome + halfY + 0.01f,
                CameraFramer.TopChromeWithHint - halfY - 0.01f);
            transform.position = _camera.ViewportToWorldPoint(view);
        }
    }
}
