using PuttSeed.Core.FixedMath;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// The ONLY place fixed-point values become floats: the render boundary.
    /// Conversions here are strictly one-way (sim -> presentation); nothing
    /// returned by these helpers may ever flow back into the simulation.
    /// </summary>
    public static class FixView
    {
        private const double One = 4294967296.0; // 2^32

        /// <summary>Fixed to float, for rendering only.</summary>
        public static float ToFloat(Fix64 value) => (float)(value.Raw / One);

        /// <summary>Fixed vector to Vector2, for rendering only.</summary>
        public static Vector2 ToVector2(Vec2Fix value) => new Vector2(ToFloat(value.X), ToFloat(value.Y));

        /// <summary>Fixed vector to Vector3 at a given z, for rendering only.</summary>
        public static Vector3 ToVector3(Vec2Fix value, float z = 0f)
            => new Vector3(ToFloat(value.X), ToFloat(value.Y), z);
    }
}
