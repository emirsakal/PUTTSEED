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

        /// <summary>Bounding box corner, cached for the containment early-out.</summary>
        public Vec2Fix Min { get; }

        /// <summary>The opposite corner.</summary>
        public Vec2Fix Max { get; }

        /// <summary>Creates a zone from ordered vertices (stored as-is; caller must not mutate).</summary>
        public ZonePolygon(Vec2Fix[] vertices)
        {
            Vertices = vertices;

            // The crossing test costs a Fix64 DIVISION per straddling edge, and
            // division is a shift-subtract loop. The ball spends nearly all of
            // its life outside any given zone, so the box is cached once here
            // and consulted first.
            var min = vertices.Length > 0 ? vertices[0] : Vec2Fix.Zero;
            var max = min;
            for (int i = 1; i < vertices.Length; i++)
            {
                min = new Vec2Fix(Fix64.Min(min.X, vertices[i].X), Fix64.Min(min.Y, vertices[i].Y));
                max = new Vec2Fix(Fix64.Max(max.X, vertices[i].X), Fix64.Max(max.Y, vertices[i].Y));
            }

            Min = min;
            Max = max;
        }

        /// <summary>
        /// Even-odd test: casts a ray toward +x and counts edge crossings. The
        /// half-open rule (one endpoint strictly above, the other not) counts
        /// each vertex exactly once, so points level with vertices classify
        /// consistently.
        /// </summary>
        public bool Contains(Vec2Fix point)
        {
            // Outside the bounding box is outside the polygon — a comparison,
            // not an approximation, so every case the exact test could decide
            // it still decides.
            if (point.X < Min.X || point.X > Max.X || point.Y < Min.Y || point.Y > Max.Y)
            {
                return false;
            }

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
