#nullable enable
using PuttSeed.Core.Sim;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Rebuilds the static course visuals (walls, zones, bumpers, hole) as
    /// flat-color meshes whenever a course loads. Reads CourseData only —
    /// never mutates sim state.
    /// </summary>
    public sealed class CourseRenderer : MonoBehaviour
    {
        private const float WallHalfThickness = 0.06f;

        /// <summary>Clears and rebuilds all course meshes.</summary>
        public void Rebuild(CourseData course)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // Mowed-grass stripes: the felt gets a subtle two-tone banding
            // stretched well past the walls so the camera never sees an edge.
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var wall in course.Walls)
            {
                var a = FixView.ToVector2(wall.A);
                var b = FixView.ToVector2(wall.B);
                min = Vector2.Min(min, Vector2.Min(a, b));
                max = Vector2.Max(max, Vector2.Max(a, b));
            }

            var margin = new Vector2(6f, 6f);
            MeshFactory.CreateMeshObject(transform, "Stripes",
                MeshFactory.Stripes(min - margin, max + margin, 0.85f,
                    PaletteMaterials.Felt, PaletteMaterials.FeltLight), 0.05f);

            foreach (var zone in course.IceZones)
            {
                MeshFactory.CreateMeshObject(transform, "Ice", MeshFactory.Zone(zone, PaletteMaterials.Ice), -0.008f);
            }

            foreach (var zone in course.SandZones)
            {
                MeshFactory.CreateMeshObject(transform, "Sand", MeshFactory.Zone(zone, PaletteMaterials.Sand), -0.01f);
            }

            foreach (var zone in course.WaterZones)
            {
                MeshFactory.CreateMeshObject(transform, "Water", MeshFactory.Zone(zone, PaletteMaterials.Water), -0.02f);
            }

            MeshFactory.CreateMeshObject(transform, "Hole",
                MeshFactory.Disc(FixView.ToVector2(course.HolePosition), 0.15f, PaletteMaterials.Hole), -0.03f);

            foreach (var bumper in course.Bumpers)
            {
                var shadow = MeshFactory.CreateMeshObject(transform, "BumperShadow",
                    MeshFactory.Disc(FixView.ToVector2(bumper.Center), FixView.ToFloat(bumper.Radius), PaletteMaterials.Shadow), -0.035f);
                shadow.transform.localPosition = new Vector3(ShadowOffset.x, ShadowOffset.y, -0.035f);
                MeshFactory.CreateMeshObject(transform, "Bumper",
                    MeshFactory.Disc(FixView.ToVector2(bumper.Center), FixView.ToFloat(bumper.Radius), PaletteMaterials.Bumper), -0.04f);
            }

            // Fake drop shadow: the wall mesh again, nudged down-right in a
            // translucent dark, one layer behind the real walls.
            var wallShadow = MeshFactory.CreateMeshObject(transform, "WallShadow",
                MeshFactory.Walls(course.Walls, WallHalfThickness, PaletteMaterials.Shadow), -0.045f);
            wallShadow.transform.localPosition = new Vector3(ShadowOffset.x, ShadowOffset.y, -0.045f);

            MeshFactory.CreateMeshObject(transform, "Walls",
                MeshFactory.Walls(course.Walls, WallHalfThickness, PaletteMaterials.Wall), -0.05f);
        }

        private static readonly Vector2 ShadowOffset = new Vector2(0.05f, -0.06f);
    }
}
