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

        [Tooltip("Windmill views mirror this sim's blade phase.")]
        public SimRunner? runner;

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
                MeshFactory.CreateMeshObject(transform, "Ice", MeshFactory.Zone(zone, PaletteMaterials.IceColor), -0.008f);
                DecorateZone(zone, zoneIndex++, ZoneKind.Ice);
            }

            foreach (var zone in course.SandZones)
            {
                MeshFactory.CreateMeshObject(transform, "Sand", MeshFactory.Zone(zone, PaletteMaterials.SandColor), -0.01f);
                DecorateZone(zone, zoneIndex++, ZoneKind.Sand);
            }

            foreach (var zone in course.WaterZones)
            {
                MeshFactory.CreateMeshObject(transform, "Water", MeshFactory.Zone(zone, PaletteMaterials.WaterColor), -0.02f);
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

            int bumperIndex = 0;
            foreach (var bumper in course.Bumpers)
            {
                var center = FixView.ToVector2(bumper.Center);
                float radius = FixView.ToFloat(bumper.Radius);
                var shadow = MeshFactory.CreateMeshObject(transform, "BumperShadow",
                    MeshFactory.Disc(center, radius, PaletteMaterials.Shadow), -0.035f);
                shadow.transform.localPosition = new Vector3(ShadowOffset.x, ShadowOffset.y, -0.035f);

                // Zero-centered mesh + positioned object, so the idle pulse
                // can scale the disc around its own center.
                var bumperGo = MeshFactory.CreateMeshObject(transform, "Bumper",
                    MeshFactory.Disc(Vector2.zero, radius, PaletteMaterials.BumperColor), -0.04f);
                bumperGo.transform.localPosition = new Vector3(center.x, center.y, -0.04f);
                bumperGo.AddComponent<BumperPulse>().phase = bumperIndex++ * 1.7f;
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

            BuildElementWave(course);

            _intro = StartCoroutine(IntroReveal());
        }

        /// <summary>
        /// The 2026-08 element wave: ramps (arrowed bands), one-way gates
        /// (amber bar + chevrons), portal mouths (rings) and windmills
        /// (blade bar rotating in lockstep with the sim's phase clock).
        /// </summary>
        private void BuildElementWave(CourseData course)
        {
            foreach (var ramp in course.Ramps)
            {
                var quad = new Vector2[ramp.Area.Vertices.Length];
                for (int i = 0; i < quad.Length; i++)
                {
                    quad[i] = FixView.ToVector2(ramp.Area.Vertices[i]);
                }

                // A whisper of shade over the felt plus downhill arrows — the
                // direction is the information, so the arrows do the talking.
                MeshFactory.CreateMeshObject(transform, "Ramp",
                    MeshFactory.Zone(ramp.Area, new Color(0f, 0f, 0f, 0.10f)), -0.007f);
                if (quad.Length == 4)
                {
                    var dir = FixView.ToVector2(ramp.Accel).normalized;
                    var arrow = new Color(0.97f, 0.96f, 0.90f, 0.5f);
                    for (int i = 0; i < 3; i++)
                    {
                        var tip = Bilerp(quad, 0.25f + 0.25f * i, 0.5f) + dir * 0.22f;
                        DrawChevron(tip, dir, 0.16f, arrow, -0.0065f, "RampArrow");
                    }
                }
            }

            foreach (var gate in course.Gates)
            {
                var a = FixView.ToVector2(gate.A);
                var b = FixView.ToVector2(gate.B);
                var pass = FixView.ToVector2(gate.PassNormal).normalized;
                MeshFactory.CreateMeshObject(transform, "Gate",
                    MeshFactory.Outline(new[] { a, b }, 0.05f, PaletteMaterials.Gate, closed: false),
                    -0.042f);

                // Chevrons along the bar, pointing the passable way.
                for (int i = 1; i <= 3; i++)
                {
                    var basePoint = Vector2.Lerp(a, b, i / 4f);
                    DrawChevron(basePoint + pass * 0.16f, pass, 0.14f, PaletteMaterials.Gate,
                        -0.042f, "GateChevron");
                }
            }

            foreach (var portal in course.Portals)
            {
                var mouth = FixView.ToVector2(portal.Entry);
                float radius = FixView.ToFloat(portal.Radius);
                MeshFactory.CreateMeshObject(transform, "PortalGlow",
                    MeshFactory.Disc(mouth, radius, new Color(
                        PaletteMaterials.Portal.r, PaletteMaterials.Portal.g,
                        PaletteMaterials.Portal.b, 0.20f)), -0.018f);
                MeshFactory.CreateMeshObject(transform, "PortalRing",
                    MeshFactory.Ring(mouth, radius * 0.82f, radius, PaletteMaterials.Portal), -0.019f);
                MeshFactory.CreateMeshObject(transform, "PortalCore",
                    MeshFactory.Disc(mouth, radius * 0.30f, PaletteMaterials.Portal), -0.019f);
            }

            foreach (var mill in course.Windmills)
            {
                var pivot = FixView.ToVector2(mill.Pivot);
                float blade = FixView.ToFloat(mill.BladeLength);

                var shadow = MeshFactory.CreateMeshObject(transform, "MillShadow",
                    MeshFactory.Disc(pivot, 0.12f, PaletteMaterials.Shadow), -0.043f);
                shadow.transform.localPosition += new Vector3(ShadowOffset.x, ShadowOffset.y, 0f);

                // Blade container rotates as a whole; blades are wall-colored
                // strokes so "this blocks" reads instantly.
                var bladesGo = new GameObject("MillBlades");
                bladesGo.transform.SetParent(transform, false);
                bladesGo.transform.localPosition = new Vector3(pivot.x, pivot.y, -0.044f);
                for (int b = 0; b < mill.BladeCount; b++)
                {
                    float angle = b * (360f / mill.BladeCount);
                    var dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                    MeshFactory.CreateMeshObject(bladesGo.transform, "MillBlade",
                        MeshFactory.Outline(new[] { Vector2.zero, dir * blade }, 0.07f,
                            PaletteMaterials.Wall, closed: false), 0f);
                }

                if (runner != null)
                {
                    bladesGo.AddComponent<WindmillView>()
                        .Initialize(runner, mill.Phase0, mill.OmegaSteps);
                }

                MeshFactory.CreateMeshObject(transform, "MillPivot",
                    MeshFactory.Disc(pivot, 0.10f, PaletteMaterials.Wall), -0.045f);
                MeshFactory.CreateMeshObject(transform, "MillPivotCap",
                    MeshFactory.Disc(pivot, 0.045f, new Color(0.97f, 0.96f, 0.90f)), -0.046f);
            }
        }

        /// <summary>Two strokes meeting at a tip — a '&gt;' pointing along dir.</summary>
        private void DrawChevron(Vector2 tip, Vector2 dir, float size, Color color, float z, string name)
        {
            var side = new Vector2(-dir.y, dir.x);
            var back = tip - dir * size;
            MeshFactory.CreateMeshObject(transform, name,
                MeshFactory.Outline(new[] { back + side * size * 0.7f, tip }, 0.035f, color,
                    closed: false), z);
            MeshFactory.CreateMeshObject(transform, name,
                MeshFactory.Outline(new[] { back - side * size * 0.7f, tip }, 0.035f, color,
                    closed: false), z);
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
                ZoneKind.Sand => PaletteMaterials.SandColor,
                ZoneKind.Ice => PaletteMaterials.IceColor,
                _ => PaletteMaterials.WaterColor,
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
