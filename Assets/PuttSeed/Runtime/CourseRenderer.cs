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

        [Tooltip("When set, the build-in reveal waits for the cover to lift.")]
        public LoadingOverlay? overlay;

        private Coroutine? _intro;

        /// <summary>
        /// Clears and rebuilds all course meshes. The seed nudges the felt
        /// tone a touch warmer or cooler — every day's course has its own
        /// light, identical for every player (presentation only; the sim
        /// never sees colors).
        /// </summary>
        public void Rebuild(CourseData course, ulong seed = 0)
        {
            if (_intro != null)
            {
                StopCoroutine(_intro);
                _intro = null;
            }

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
                    DailyTint(PaletteMaterials.Felt, seed),
                    DailyTint(PaletteMaterials.FeltLight, seed)), 0.05f);

            int zoneIndex = 0;
            foreach (var zone in course.IceZones)
            {
                MeshFactory.CreateMeshObject(transform, "Ice", MeshFactory.Zone(zone, PaletteMaterials.Ice), -0.008f);
                DecorateZone(zone, zoneIndex++, ZoneKind.Ice);
            }

            foreach (var zone in course.SandZones)
            {
                MeshFactory.CreateMeshObject(transform, "Sand", MeshFactory.Zone(zone, PaletteMaterials.Sand), -0.01f);
                DecorateZone(zone, zoneIndex++, ZoneKind.Sand);
            }

            foreach (var zone in course.WaterZones)
            {
                MeshFactory.CreateMeshObject(transform, "Water", MeshFactory.Zone(zone, PaletteMaterials.Water), -0.02f);
                DecorateZone(zone, zoneIndex++, ZoneKind.Water);
            }

            // Tee marker: the start pad is always readable (also after resets).
            MeshFactory.CreateMeshObject(transform, "Tee",
                MeshFactory.Ring(FixView.ToVector2(course.StartPosition), 0.15f, 0.185f,
                    new Color(0.97f, 0.96f, 0.90f, 0.45f)), -0.015f);

            // The cup gets depth: light rim, cup, darker bottom.
            var holePos = FixView.ToVector2(course.HolePosition);
            MeshFactory.CreateMeshObject(transform, "HoleRim",
                MeshFactory.Ring(holePos, 0.15f, 0.175f, new Color(1f, 1f, 1f, 0.16f)), -0.029f);
            MeshFactory.CreateMeshObject(transform, "Hole",
                MeshFactory.Disc(holePos, 0.15f, PaletteMaterials.Hole), -0.03f);
            MeshFactory.CreateMeshObject(transform, "HoleBottom",
                MeshFactory.Disc(holePos, 0.085f, new Color(0.02f, 0.02f, 0.035f)), -0.031f);

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
            var capShadow = MeshFactory.CreateMeshObject(transform, "WallCapShadow",
                MeshFactory.WallCaps(course.Walls, WallHalfThickness, PaletteMaterials.Shadow), -0.045f);
            capShadow.transform.localPosition = new Vector3(ShadowOffset.x, ShadowOffset.y, -0.045f);

            MeshFactory.CreateMeshObject(transform, "Walls",
                MeshFactory.Walls(course.Walls, WallHalfThickness, PaletteMaterials.Wall), -0.05f);
            MeshFactory.CreateMeshObject(transform, "WallCaps",
                MeshFactory.WallCaps(course.Walls, WallHalfThickness, PaletteMaterials.Wall), -0.05f);

            _intro = StartCoroutine(IntroReveal());
        }

        /// <summary>
        /// Build-in reveal: once the loading cover lifts, elements fade in as
        /// a little stage entrance — ground first, then walls, bumpers, and
        /// the cup/tee last. Alpha rides a MaterialPropertyBlock so meshes
        /// stay untouched.
        /// </summary>
        private System.Collections.IEnumerator IntroReveal()
        {
            var renderers = GetComponentsInChildren<MeshRenderer>();
            var delays = new float[renderers.Length];
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                delays[i] = GroupDelay(renderers[i].gameObject.name);
                block.SetColor("_Color", new Color(1f, 1f, 1f, 0f));
                renderers[i].SetPropertyBlock(block);
            }

            while (overlay != null && overlay.IsShown)
            {
                yield return null;
            }

            const float fade = 0.25f;
            const float total = 0.24f + fade;
            for (float t = 0f; t < total; t += Time.deltaTime)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null)
                    {
                        continue; // a rebuild mid-reveal destroys children
                    }

                    float a = Mathf.Clamp01((t - delays[i]) / fade);
                    block.SetColor("_Color", new Color(1f, 1f, 1f, a));
                    renderers[i].SetPropertyBlock(block);
                }

                yield return null;
            }

            block.SetColor("_Color", Color.white);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].SetPropertyBlock(block);
                }
            }

            _intro = null;
        }

        private static float GroupDelay(string name)
        {
            if (name == "Stripes")
            {
                return 0f; // the ground is simply there
            }

            if (name.StartsWith("Wall", System.StringComparison.Ordinal))
            {
                return 0.08f;
            }

            if (name.StartsWith("Bumper", System.StringComparison.Ordinal))
            {
                return 0.16f;
            }

            if (name.StartsWith("Hole", System.StringComparison.Ordinal) || name == "Tee")
            {
                return 0.24f;
            }

            return 0.02f; // zones and their dressing
        }

        private enum ZoneKind
        {
            Sand,
            Ice,
            Water,
        }

        /// <summary>
        /// Zone dressing: a darker contour plus a kind-specific inner detail —
        /// sand speckles, ice sheen strokes, drifting water dashes. Seeded per
        /// zone index so every rebuild of a course looks identical.
        /// </summary>
        private void DecorateZone(ZonePolygon zone, int zoneIndex, ZoneKind kind)
        {
            var quad = new Vector2[zone.Vertices.Length];
            for (int i = 0; i < quad.Length; i++)
            {
                quad[i] = FixView.ToVector2(zone.Vertices[i]);
            }

            var baseColor = kind switch
            {
                ZoneKind.Sand => PaletteMaterials.Sand,
                ZoneKind.Ice => PaletteMaterials.Ice,
                _ => PaletteMaterials.Water,
            };
            var contour = new Color(baseColor.r * 0.72f, baseColor.g * 0.72f, baseColor.b * 0.72f, 0.9f);
            float z = kind == ZoneKind.Water ? -0.021f : kind == ZoneKind.Sand ? -0.011f : -0.009f;
            MeshFactory.CreateMeshObject(transform, "ZoneEdge",
                MeshFactory.Outline(quad, 0.045f, contour), z);

            if (quad.Length != 4)
            {
                return; // details use bilinear sampling; decorator zones are quads
            }

            var rng = new System.Random(zoneIndex * 7919 + 17);
            if (kind == ZoneKind.Sand)
            {
                var speckle = new Color(0.72f, 0.62f, 0.41f, 0.85f);
                int count = 8 + rng.Next(5);
                for (int i = 0; i < count; i++)
                {
                    var p = Bilerp(quad, 0.1f + 0.8f * (float)rng.NextDouble(), 0.1f + 0.8f * (float)rng.NextDouble());
                    MeshFactory.CreateMeshObject(transform, "Speckle",
                        MeshFactory.Disc(p, 0.02f + 0.015f * (float)rng.NextDouble(), speckle, 8), -0.0115f);
                }
            }
            else if (kind == ZoneKind.Ice)
            {
                var sheen = new Color(1f, 1f, 1f, 0.3f);
                for (int i = 0; i < 3; i++)
                {
                    float u = 0.22f + 0.28f * i + 0.06f * (float)rng.NextDouble();
                    var a = Bilerp(quad, u - 0.04f, 0.2f);
                    var b = Bilerp(quad, u + 0.1f, 0.8f);
                    MeshFactory.CreateMeshObject(transform, "Sheen",
                        MeshFactory.Outline(new[] { a, b }, 0.03f, sheen, closed: false), -0.0095f);
                }
            }
            else
            {
                var wavesGo = new GameObject("WaterWaves");
                wavesGo.transform.SetParent(transform, false);
                wavesGo.AddComponent<WaterWaves>().Initialize(quad);
            }
        }

        /// <summary>Seed-derived subtle felt tint (±3% warm/cool shift).</summary>
        private static Color DailyTint(Color felt, ulong seed)
        {
            if (seed == 0)
            {
                return felt;
            }

            uint h = (uint)(seed ^ (seed >> 32)) * 2654435761u;
            float warm = ((h & 0xFF) / 255f - 0.5f) * 0.055f;
            float bright = (((h >> 8) & 0xFF) / 255f - 0.5f) * 0.04f;
            return new Color(
                Mathf.Clamp01(felt.r + warm + bright),
                Mathf.Clamp01(felt.g + bright),
                Mathf.Clamp01(felt.b - warm * 0.7f + bright),
                felt.a);
        }

        /// <summary>Bilinear point inside a quad zone (u along, v across).</summary>
        internal static Vector2 Bilerp(Vector2[] quad, float u, float v)
            => Vector2.Lerp(Vector2.Lerp(quad[0], quad[1], u), Vector2.Lerp(quad[3], quad[2], u), v);

        private static readonly Vector2 ShadowOffset = new Vector2(0.05f, -0.06f);
    }
}
