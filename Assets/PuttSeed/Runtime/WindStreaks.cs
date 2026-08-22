#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Faint streaks drifting across the grass on a windy day.
    ///
    /// The vane names the wind — direction, barbs, a number in the top bar —
    /// but it lives in the quietest corner of the course, and a player lining
    /// up a putt is looking at the ball. These carry the same fact to where
    /// the eyes already are: a handful of barely-there lines sliding the way
    /// the air pushes, at a pace that follows its strength, so the three
    /// tiers read apart without anyone glancing at the corner.
    ///
    /// Presentation only: reads a wind vector at build time and never touches
    /// the sim. Everything about the dressing is derived from the SEED, so
    /// every device dresses the same day identically — not because the sim
    /// needs it, but because two players comparing screenshots should not
    /// find different weather.
    ///
    /// Reduced motion turns these off entirely (see MotionSettings): they are
    /// continuous ambient drift, exactly the thing that setting exists to
    /// remove, and the vane keeps carrying the information.
    /// </summary>
    public sealed class WindStreaks : MonoBehaviour
    {
        /// <summary>How many streaks a windy day wears. Few, on purpose —
        /// cross-mown stripes were cut for being eye-tiring, and this stays on
        /// the right side of that line by staying sparse.</summary>
        public const int Count = 5;

        private const float BaseAlpha = 0.13f;
        private const float StreakLength = 0.9f;
        private const float StreakWidth = 0.032f;

        /// <summary>World units per second per unit of wind strength.</summary>
        private const float SpeedPerWind = 2.2f;

        private readonly Transform[] _streaks = new Transform[Count];
        private readonly MeshRenderer[] _renderers = new MeshRenderer[Count];
        private readonly float[] _lanes = new float[Count];
        private readonly float[] _phases = new float[Count];
        private readonly float[] _speedScales = new float[Count];

        private MaterialPropertyBlock? _block;
        private Vector2 _axis;
        private Vector2 _perp;
        private Vector2 _entry;
        private float _span;
        private float _speed;
        private float _born;

        /// <summary>Builds the streaks for one course's wind and bounds.</summary>
        public void Build(Vector2 wind, Vector2 min, Vector2 max, ulong seed)
        {
            _axis = wind.normalized;
            _perp = new Vector2(-_axis.y, _axis.x);

            var centre = (min + max) * 0.5f;
            var half = (max - min) * 0.5f;

            // The bounds projected onto the travel axis: how far a streak has
            // to go, and how wide the lanes can spread.
            float halfAlong = Mathf.Abs(half.x * _axis.x) + Mathf.Abs(half.y * _axis.y);
            float halfAcross = Mathf.Abs(half.x * _perp.x) + Mathf.Abs(half.y * _perp.y);
            _span = 2f * (halfAlong + StreakLength);
            _entry = centre - _axis * (halfAlong + StreakLength);
            _speed = wind.magnitude * SpeedPerWind;
            _born = Time.time;
            _block = new MaterialPropertyBlock();

            float heading = Mathf.Atan2(_axis.y, _axis.x) * Mathf.Rad2Deg;
            for (int i = 0; i < Count; i++)
            {
                _lanes[i] = ((((seed >> (i * 11 + 3)) & 0xFF) / 255f) * 2f - 1f) * halfAcross * 0.85f;
                _phases[i] = (((seed >> (i * 7 + 29)) & 0xFF) / 255f) * _span;
                _speedScales[i] = 0.85f + (((seed >> (i * 5 + 17)) & 0xFF) / 255f) * 0.35f;

                var go = MeshFactory.CreateMeshObject(transform, "Streak",
                    MeshFactory.Quad(
                        new Vector2(-StreakLength * 0.5f, -StreakWidth * 0.5f),
                        new Vector2(StreakLength * 0.5f, StreakWidth * 0.5f),
                        new Color(1f, 1f, 1f, BaseAlpha)),
                    -0.02f);
                go.transform.localEulerAngles = new Vector3(0f, 0f, heading);
                _streaks[i] = go.transform;
                _renderers[i] = go.GetComponent<MeshRenderer>();
            }
        }

        /// <summary>
        /// Visibility across one crossing: in over the first fifth of the
        /// journey, out over the last, full in between — so streaks are born
        /// and die softly instead of popping at the course edge.
        /// </summary>
        public static float FadeFor(float fraction)
            => Mathf.Clamp01(Mathf.Min(fraction, 1f - fraction) / 0.2f);

        private void LateUpdate()
        {
            if (_block == null)
            {
                return;
            }

            // The course reveal fades everything in via property blocks; these
            // animate their own, so they would win that fight rudely. Growing
            // in over the first moments keeps the same manner instead.
            float grown = Mathf.Clamp01((Time.time - _born) / 0.6f);
            float age = Time.time - _born;
            for (int i = 0; i < Count; i++)
            {
                float travelled = (_phases[i] + age * _speed * _speedScales[i]) % _span;
                var p = _entry + _axis * travelled + _perp * _lanes[i];
                _streaks[i].localPosition = new Vector3(p.x, p.y, -0.02f);
                _block.SetColor("_Color",
                    new Color(1f, 1f, 1f, FadeFor(travelled / _span) * grown));
                _renderers[i].SetPropertyBlock(_block);
            }
        }
    }
}
