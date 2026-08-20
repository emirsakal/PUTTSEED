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
        /// <summary>Full strength for barb purposes: three barbs, no more.</summary>
        private const float StrengthPerBarb = 0.33f;

        private Transform? _needle;
        private float _heading;

        /// <summary>
        /// Builds the badge and points it downwind. The needle is a child
        /// built centred on the origin, so turning it turns the arrow rather
        /// than swinging it around the course (a mesh carrying absolute
        /// coordinates cannot be rotated in place).
        /// </summary>
        public void Build(Vector2 wind)
        {
            var cream = PaletteMaterials.Ball;
            MeshFactory.CreateMeshObject(transform, "Badge",
                MeshFactory.Disc(Vector2.zero, 0.3f, PaletteMaterials.VaneBadge, segments: 40), 0f);
            MeshFactory.CreateMeshObject(transform, "BadgeRing",
                MeshFactory.Ring(Vector2.zero, 0.285f, 0.315f,
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

        private void LateUpdate()
        {
            if (_needle == null)
            {
                return;
            }

            // The same two-sine wobble the menu pennant waves with: a vane that
            // holds perfectly still reads as a printed icon, and the player
            // stops seeing it by the second hole.
            float t = Time.time;
            float sway = Mathf.Sin(t * 1.7f) * 3.4f + Mathf.Sin(t * 3.9f) * 1.1f;
            _needle.localEulerAngles = new Vector3(0f, 0f, _heading + sway);
        }
    }
}
