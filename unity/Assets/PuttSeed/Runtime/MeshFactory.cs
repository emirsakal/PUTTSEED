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
