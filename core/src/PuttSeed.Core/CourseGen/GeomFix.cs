using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.CourseGen
{
    /// <summary>Fixed-point geometry predicates used by course generation.</summary>
    public static class GeomFix
    {
        /// <summary>Sign of the cross product (b-a) x (c-a): orientation of the triple.</summary>
        public static int Orientation(Vec2Fix a, Vec2Fix b, Vec2Fix c)
        {
            var cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
            return Fix64.Sign(cross);
        }

        /// <summary>
        /// True when segments AB and CD properly cross (interiors intersect).
        /// Collinear overlaps and shared-endpoint touches count as crossing —
        /// the conservative choice for self-avoidance checks.
        /// </summary>
        public static bool SegmentsProperlyIntersect(Vec2Fix a, Vec2Fix b, Vec2Fix c, Vec2Fix d)
        {
            int o1 = Orientation(a, b, c);
            int o2 = Orientation(a, b, d);
            int o3 = Orientation(c, d, a);
            int o4 = Orientation(c, d, b);

            if (o1 != o2 && o3 != o4)
            {
                return true;
            }

            // Collinear cases: reject only if a collinear point lies on the segment.
            if (o1 == 0 && OnSegment(a, b, c)) return true;
            if (o2 == 0 && OnSegment(a, b, d)) return true;
            if (o3 == 0 && OnSegment(c, d, a)) return true;
            if (o4 == 0 && OnSegment(c, d, b)) return true;
            return false;
        }

        private static bool OnSegment(Vec2Fix a, Vec2Fix b, Vec2Fix p)
            => p.X >= Fix64.Min(a.X, b.X) && p.X <= Fix64.Max(a.X, b.X)
            && p.Y >= Fix64.Min(a.Y, b.Y) && p.Y <= Fix64.Max(a.Y, b.Y);
    }
}
