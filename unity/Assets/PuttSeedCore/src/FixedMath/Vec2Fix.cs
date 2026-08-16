using System;

namespace PuttSeed.Core.FixedMath
{
    /// <summary>
    /// 2D vector of <see cref="Fix64"/> components. Value struct used for all
    /// positions, velocities and normals in the simulation. Y-up convention.
    /// </summary>
    public readonly struct Vec2Fix : IEquatable<Vec2Fix>
    {
        /// <summary>X component.</summary>
        public Fix64 X { get; }

        /// <summary>Y component.</summary>
        public Fix64 Y { get; }

        /// <summary>Creates a vector from two fixed-point components.</summary>
        public Vec2Fix(Fix64 x, Fix64 y)
        {
            X = x;
            Y = y;
        }

        /// <summary>The zero vector.</summary>
        public static readonly Vec2Fix Zero = new Vec2Fix(Fix64.Zero, Fix64.Zero);

        /// <summary>Component-wise addition.</summary>
        public static Vec2Fix operator +(Vec2Fix a, Vec2Fix b) => new Vec2Fix(a.X + b.X, a.Y + b.Y);

        /// <summary>Component-wise subtraction.</summary>
        public static Vec2Fix operator -(Vec2Fix a, Vec2Fix b) => new Vec2Fix(a.X - b.X, a.Y - b.Y);

        /// <summary>Negation.</summary>
        public static Vec2Fix operator -(Vec2Fix a) => new Vec2Fix(-a.X, -a.Y);

        /// <summary>Scales by a scalar.</summary>
        public static Vec2Fix operator *(Vec2Fix v, Fix64 s) => new Vec2Fix(v.X * s, v.Y * s);

        /// <summary>Scales by a scalar.</summary>
        public static Vec2Fix operator *(Fix64 s, Vec2Fix v) => new Vec2Fix(v.X * s, v.Y * s);

        /// <summary>Divides by a scalar.</summary>
        public static Vec2Fix operator /(Vec2Fix v, Fix64 s) => new Vec2Fix(v.X / s, v.Y / s);

        /// <summary>Dot product.</summary>
        public static Fix64 Dot(Vec2Fix a, Vec2Fix b) => a.X * b.X + a.Y * b.Y;

        /// <summary>Squared length (cheap; prefer over <see cref="Length"/> for comparisons).</summary>
        public Fix64 LengthSq() => X * X + Y * Y;

        /// <summary>Length via <see cref="Fix64.Sqrt"/>.</summary>
        public Fix64 Length() => Fix64.Sqrt(LengthSq());

        /// <summary>Counter-clockwise perpendicular: (x, y) -> (-y, x).</summary>
        public Vec2Fix Perp() => new Vec2Fix(-Y, X);

        /// <summary>Exact component equality.</summary>
        public static bool operator ==(Vec2Fix a, Vec2Fix b) => a.X == b.X && a.Y == b.Y;

        /// <summary>Exact component inequality.</summary>
        public static bool operator !=(Vec2Fix a, Vec2Fix b) => !(a == b);

        /// <inheritdoc />
        public bool Equals(Vec2Fix other) => this == other;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Vec2Fix other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => unchecked(X.GetHashCode() * 397 ^ Y.GetHashCode());

        /// <inheritdoc />
        public override string ToString() => $"({X}, {Y})";
    }
}
