using System.Collections.Generic;
using PuttSeed.Core.Sim;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>Builds the flat-color runtime meshes for course elements.</summary>
    public static class MeshFactory
    {
        /// <summary>Creates a child GameObject rendering a mesh with the shared material.</summary>
        public static GameObject CreateMeshObject(Transform parent, string name, Mesh mesh, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = PaletteMaterials.Shared;
            return go;
        }

        /// <summary>All wall segments as thin quads in a single mesh.</summary>
        public static Mesh Walls(WallSegment[] walls, float halfThickness, Color color)
        {
            var vertices = new List<Vector3>(walls.Length * 4);
            var triangles = new List<int>(walls.Length * 6);
            var colors = new List<Color>(walls.Length * 4);

            foreach (var wall in walls)
            {
                Vector2 a = FixView.ToVector2(wall.A);
                Vector2 b = FixView.ToVector2(wall.B);
                Vector2 dir = (b - a).normalized;
                Vector2 n = new Vector2(-dir.y, dir.x) * halfThickness;
                Vector2 ext = dir * halfThickness; // extend ends so joints close

                int baseIndex = vertices.Count;
                vertices.Add(a - ext + n);
                vertices.Add(b + ext + n);
                vertices.Add(b + ext - n);
                vertices.Add(a - ext - n);
                triangles.AddRange(new[] { baseIndex, baseIndex + 1, baseIndex + 2, baseIndex, baseIndex + 2, baseIndex + 3 });
                for (int i = 0; i < 4; i++)
                {
                    colors.Add(color);
                }
            }

            return Build(vertices, triangles, colors);
        }

        /// <summary>A polygon zone as a triangle fan (decorator zones are convex quads).</summary>
        public static Mesh Zone(ZonePolygon zone, Color color)
        {
            var verts = zone.Vertices;
            var vertices = new List<Vector3>(verts.Length);
            var colors = new List<Color>(verts.Length);
            foreach (var v in verts)
            {
                vertices.Add(FixView.ToVector2(v));
                colors.Add(color);
            }

            var triangles = new List<int>((verts.Length - 2) * 3);
            for (int i = 2; i < verts.Length; i++)
            {
                triangles.AddRange(new[] { 0, i - 1, i });
            }

            return Build(vertices, triangles, colors);
        }

        /// <summary>A filled disc (bumpers, hole, ball).</summary>
        public static Mesh Disc(Vector2 center, float radius, Color color, int segments = 28)
        {
            var vertices = new List<Vector3>(segments + 1) { center };
            var colors = new List<Color>(segments + 1) { color };
            var triangles = new List<int>(segments * 3);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                vertices.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                colors.Add(color);
                triangles.AddRange(new[] { 0, 1 + i, 1 + (i + 1) % segments });
            }

            return Build(vertices, triangles, colors);
        }

        /// <summary>An axis-aligned rectangle (poles, shadows, simple props).</summary>
        public static Mesh Quad(Vector2 min, Vector2 max, Color color)
        {
            var vertices = new List<Vector3>
            {
                new Vector3(min.x, min.y), new Vector3(min.x, max.y),
                new Vector3(max.x, max.y), new Vector3(max.x, min.y),
            };
            var triangles = new List<int> { 0, 1, 2, 0, 2, 3 };
            var colors = new List<Color> { color, color, color, color };
            return Build(vertices, triangles, colors);
        }

        /// <summary>A single triangle (the flag pennant).</summary>
        public static Mesh Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            var vertices = new List<Vector3> { a, b, c };
            var triangles = new List<int> { 0, 1, 2 };
            var colors = new List<Color> { color, color, color };
            return Build(vertices, triangles, colors);
        }

        /// <summary>
        /// Horizontal mowed-grass bands covering a rectangle: alternating tones
        /// every <paramref name="bandHeight"/> units, aligned to world Y so the
        /// pattern is stable across courses.
        /// </summary>
        public static Mesh Stripes(Vector2 min, Vector2 max, float bandHeight, Color a, Color b)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();

            int firstBand = Mathf.FloorToInt(min.y / bandHeight);
            int lastBand = Mathf.CeilToInt(max.y / bandHeight);
            for (int band = firstBand; band < lastBand; band++)
            {
                float y0 = band * bandHeight;
                float y1 = y0 + bandHeight;
                var color = (band & 1) == 0 ? a : b;
                int baseIndex = vertices.Count;
                vertices.Add(new Vector3(min.x, y0));
                vertices.Add(new Vector3(min.x, y1));
                vertices.Add(new Vector3(max.x, y1));
                vertices.Add(new Vector3(max.x, y0));
                triangles.AddRange(new[] { baseIndex, baseIndex + 1, baseIndex + 2, baseIndex, baseIndex + 2, baseIndex + 3 });
                for (int i = 0; i < 4; i++)
                {
                    colors.Add(color);
                }
            }

            return Build(vertices, triangles, colors);
        }

        /// <summary>An annulus (tee marker, rims).</summary>
        public static Mesh Ring(Vector2 center, float innerRadius, float outerRadius, Color color, int segments = 32)
        {
            var vertices = new List<Vector3>(segments * 2);
            var colors = new List<Color>(segments * 2);
            var triangles = new List<int>(segments * 6);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vertices.Add(center + dir * innerRadius);
                vertices.Add(center + dir * outerRadius);
                colors.Add(color);
                colors.Add(color);
                int j = (i + 1) % segments;
                triangles.AddRange(new[]
                {
                    i * 2, j * 2, i * 2 + 1,
                    i * 2 + 1, j * 2, j * 2 + 1,
                });
            }

            return Build(vertices, triangles, colors);
        }

        /// <summary>
        /// A polygon outline (closed) or stroke (open): one thin quad per edge
        /// plus a joint disc per vertex, so corners stay filled at any angle.
        /// </summary>
        public static Mesh Outline(Vector2[] points, float width, Color color, bool closed = true)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();
            float half = width * 0.5f;

            int edges = closed ? points.Length : points.Length - 1;
            for (int i = 0; i < edges; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Length];
                var dir = (b - a).normalized;
                var n = new Vector2(-dir.y, dir.x) * half;
                int baseIndex = vertices.Count;
                vertices.Add(a + n);
                vertices.Add(b + n);
                vertices.Add(b - n);
                vertices.Add(a - n);
                triangles.AddRange(new[] { baseIndex, baseIndex + 1, baseIndex + 2, baseIndex, baseIndex + 2, baseIndex + 3 });
                for (int c = 0; c < 4; c++)
                {
                    colors.Add(color);
                }

                AppendDisc(vertices, triangles, colors, a, half, color, 10);
            }

            if (!closed && points.Length > 0)
            {
                AppendDisc(vertices, triangles, colors, points[points.Length - 1], half, color, 10);
            }

            return Build(vertices, triangles, colors);
        }

        /// <summary>Half-disc caps at every wall endpoint — smooth vector ends.</summary>
        public static Mesh WallCaps(WallSegment[] walls, float radius, Color color)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();
            foreach (var wall in walls)
            {
                AppendDisc(vertices, triangles, colors, FixView.ToVector2(wall.A), radius, color, 12);
                AppendDisc(vertices, triangles, colors, FixView.ToVector2(wall.B), radius, color, 12);
            }

            return Build(vertices, triangles, colors);
        }

        private static void AppendDisc(List<Vector3> vertices, List<int> triangles, List<Color> colors,
            Vector2 center, float radius, Color color, int segments)
        {
            int baseIndex = vertices.Count;
            vertices.Add(center);
            colors.Add(color);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                vertices.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                colors.Add(color);
                triangles.AddRange(new[] { baseIndex, baseIndex + 1 + i, baseIndex + 1 + (i + 1) % segments });
            }
        }

        private static Mesh Build(List<Vector3> vertices, List<int> triangles, List<Color> colors)
        {
            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
