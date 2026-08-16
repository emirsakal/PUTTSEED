using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// Simple polygon zone (sand, water) with an even-odd ray-cast containment
    /// test in fixed point. Vertices are stored in order (either winding);
    /// polygons may be concave but must not self-intersect.
    /// </summary>
    public readonly struct ZonePolygon
    {
        /// <summary>Polygon vertices in order.</summary>
        public Vec2Fix[] Vertices { get; }

        /// <summary>Creates a zone from ordered vertices (stored as-is; caller must not mutate).</summary>
        public ZonePolygon(Vec2Fix[] vertices)
        {
            Vertices = vertices;
        }

        /// <summary>
        /// Even-odd test: casts a ray toward +x and counts edge crossings. The
        /// half-open rule (one endpoint strictly above, the other not) counts
        /// each vertex exactly once, so points level with vertices classify
        /// consistently.
        /// </summary>
        public bool Contains(Vec2Fix point)
        {
            var verts = Vertices;
            bool inside = false;
            for (int i = 0, j = verts.Length - 1; i < verts.Length; j = i++)
            {
                var a = verts[j];
                var b = verts[i];
                if ((b.Y > point.Y) == (a.Y > point.Y))
                {
                    continue; // edge does not cross the ray's horizontal line
                }

                // x of the edge at the ray's y (b.Y != a.Y is guaranteed above).
                var xCross = b.X + (point.Y - b.Y) * (a.X - b.X) / (a.Y - b.Y);
                if (point.X < xCross)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
