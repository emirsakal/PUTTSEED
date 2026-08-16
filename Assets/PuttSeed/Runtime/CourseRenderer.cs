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
                MeshFactory.CreateMeshObject(transform, "Bumper",
                    MeshFactory.Disc(FixView.ToVector2(bumper.Center), FixView.ToFloat(bumper.Radius), PaletteMaterials.Bumper), -0.04f);
            }

            MeshFactory.CreateMeshObject(transform, "Walls",
                MeshFactory.Walls(course.Walls, WallHalfThickness, PaletteMaterials.Wall), -0.05f);
        }
    }
}
