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

        /// <summary>Whether the player asked for reduced motion (wired by the bootstrap).</summary>
        public System.Func<bool>? reducedMotion;

        private Coroutine? _intro;

        /// <summary>The day's twist, for the themed light (see DailyTint).</summary>
        private PuttSeed.Core.Daily.DailyMutator _mutator;

        /// <summary>
        /// Clears and rebuilds all course meshes. The seed nudges the felt
        /// tone a touch warmer or cooler — every day's course has its own
        /// light, identical for every player (presentation only; the sim
        /// never sees colors).
        /// </summary>
        public void Rebuild(CourseData course, ulong seed = 0,
            PuttSeed.Core.Daily.DailyMutator mutator = PuttSeed.Core.Daily.DailyMutator.None)
        {
            _mutator = mutator;
            if (_intro != null)
            {
                StopCoroutine(_intro);
                _intro = null;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // Mowed-grass stripes on a MAT that ends: the felt reaches a short
            // way past the walls, then the rough takes over. Stripes used to
            // run six units out precisely so the camera never saw an edge,
            // which cost the hole its shape — the ground inside the walls
            // looked exactly like the ground outside them. The first attempt
            // at this overdid the contrast and was reverted; the tone came
            // back on 2026-08-19 with the two greens far closer together.
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var wall in course.Walls)
            {
                var a = FixView.ToVector2(wall.A);
                var b = FixView.ToVector2(wall.B);
                min = Vector2.Min(min, Vector2.Min(a, b));
                max = Vector2.Max(max, Vector2.Max(a, b));
            }

            var margin = new Vector2(0.35f, 0.35f);
            var courseCenter = (min + max) * 0.5f;

            // The rough gets light without getting busy: two enormous, barely
            // visible discs, the same trick the menu background uses. Mowing
            // the rough ACROSS the mat was tried first and cut on sight — two
            // repeating directions on one screen fight each other, and the
            // eye ends up with nowhere to rest (2026-08-19).
            float span = Mathf.Max(max.x - min.x, max.y - min.y);
            var lift = DailyTint(PaletteMaterials.RoughLight, seed);
            var blobColor = new Color(lift.r, lift.g, lift.b, 0.6f);
            for (int i = 0; i < 2; i++)
            {
                // Seed-derived: every day's rough sits differently, and every
                // device draws that day identically.
                float ax = (((seed >> (i * 13 + 5)) & 0xFF) / 255f - 0.5f) * 2.4f;
                float ay = (((seed >> (i * 13 + 19)) & 0xFF) / 255f - 0.5f) * 2.8f;
                MeshFactory.CreateMeshObject(transform, "RoughBlob",
                    MeshFactory.Disc(courseCenter + new Vector2(ax * span, ay * span),
                        span * (0.8f + i * 0.4f), blobColor, segments: 64), 0.07f);
            }

            // The fringe: a collar of longer grass around the green, so the
            // mat ends in a cut rather than a seam. It is the outermost green
            // thing, so the drop shadow hangs off IT.
            var fringe = new Vector2(0.1f, 0.1f);
            var greenMin = min - margin - fringe;
            var greenMax = max + margin + fringe;

            var shadowOffset = new Vector2(0.14f, -0.16f);
            MeshFactory.CreateMeshObject(transform, "MatShadow",
                MeshFactory.Quad(greenMin + shadowOffset, greenMax + shadowOffset,
                    new Color(0f, 0f, 0f, 0.16f)), 0.06f);

            MeshFactory.CreateMeshObject(transform, "Fringe",
                MeshFactory.Quad(greenMin, greenMax,
                    DailyTint(PaletteMaterials.Fringe, seed)), 0.055f);

            // The mowing runs across the SCREEN, not across the world. The
            // camera rolls 90° on wide courses (see CameraFramer), and stripes
            // left in world space turn with it — so the ground's pattern would
            // arrive horizontal on some holes and vertical on others, which
            // reads as a game unsure which way is up.
            //
            // The mesh is therefore built CENTRED on the origin (it normally
            // carries absolute coordinates) so the object can be turned about
            // the course itself, and its extents are swapped first so the
            // turned rectangle still covers the same ground.
            float roll = CameraFramer.RollFor(max - min);
            var matHalf = (max - min) * 0.5f + margin;
            var meshHalf = roll != 0f ? new Vector2(matHalf.y, matHalf.x) : matHalf;
            var stripes = MeshFactory.CreateMeshObject(transform, "Stripes",
                MeshFactory.Stripes(-meshHalf, meshHalf, 0.85f,
                    DailyTint(PaletteMaterials.Felt, seed),
                    DailyTint(PaletteMaterials.FeltLight, seed)), 0.05f);
            stripes.transform.localPosition = new Vector3(courseCenter.x, courseCenter.y, 0.05f);
            stripes.transform.localRotation = Quaternion.Euler(0f, 0f, roll);

            int zoneIndex = 0;
            foreach (var zone in course.IceZones)
            {
                MeshFactory.CreateMeshObject(transform, "Ice", MeshFactory.Zone(zone, PaletteMaterials.IceColor), -0.008f);
                DecorateZone(zone, zoneIndex++, ZoneKind.Ice);
            }

            foreach (var zone in course.SandZones)
            {
                // A pit, not a patch: the darker base shows only as a rim
                // around the inset fill, and that ring of shade is what makes
                // the sand read as LOWER than the felt.
                var sandBase = PaletteMaterials.SandColor;
                MeshFactory.CreateMeshObject(transform, "SandBase", MeshFactory.Zone(zone,
                    new Color(sandBase.r * 0.78f, sandBase.g * 0.76f, sandBase.b * 0.72f)), -0.0098f);
                MeshFactory.CreateMeshObject(transform, "Sand",
                    MeshFactory.Zone(ShrunkTowardCentroid(zone, 0.9f), sandBase), -0.01f);
                DecorateZone(zone, zoneIndex++, ZoneKind.Sand);
            }

            foreach (var zone in course.WaterZones)
            {
                MeshFactory.CreateMeshObject(transform, "Water", MeshFactory.Zone(zone, PaletteMaterials.WaterColor), -0.02f);
                DecorateZone(zone, zoneIndex++, ZoneKind.Water);
            }

            // Tee marker: the start pad is always readable (also after resets).
            // Under the ring, an actual PAD — a breath-lighter disc of mown
            // felt, so "start here" is a place on the ground and not only a
            // symbol floating over it.
            var padTone = DailyTint(PaletteMaterials.FeltLight, seed);
            MeshFactory.CreateMeshObject(transform, "TeePad",
                MeshFactory.Disc(FixView.ToVector2(course.StartPosition), 0.24f,
                    new Color(Mathf.Clamp01(padTone.r * 1.05f), Mathf.Clamp01(padTone.g * 1.05f),
                        Mathf.Clamp01(padTone.b * 1.05f))), -0.013f);
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

            // The rail's lit edge: a thinner wall in a lighter tone, offset
            // AGAINST the shadow, so every wall shows a bright sliver on the
            // side the light comes from and reads as extruded rather than
            // drawn. Same geometry, so it miters at the joints for free.
            var edge = MeshFactory.CreateMeshObject(transform, "WallEdge",
                MeshFactory.Walls(course.Walls, WallHalfThickness * 0.5f, PaletteMaterials.WallEdge), -0.052f);
            edge.transform.localPosition = new Vector3(-ShadowOffset.x * 0.42f, -ShadowOffset.y * 0.42f, -0.052f);
            var edgeCaps = MeshFactory.CreateMeshObject(transform, "WallEdgeCaps",
                MeshFactory.WallCaps(course.Walls, WallHalfThickness * 0.5f, PaletteMaterials.WallEdge), -0.052f);
            edgeCaps.transform.localPosition = new Vector3(-ShadowOffset.x * 0.42f, -ShadowOffset.y * 0.42f, -0.052f);

            BuildElementWave(course);
            BuildWindVane(course, min, max);
            BuildWindStreaks(min, max, seed);

            _intro = StartCoroutine(IntroReveal());
        }

        /// <summary>
        /// Dresses a windy day in drifting streaks — the wind made visible
        /// where the player is actually looking, not only in the vane's
        /// corner. Skipped under reduced motion: continuous ambient drift is
        /// the exact thing that setting removes, and the vane keeps the
        /// information.
        /// </summary>
        private void BuildWindStreaks(Vector2 min, Vector2 max, ulong seed)
        {
            if (runner == null)
            {
                return;
            }

            var wind = FixView.ToVector2(runner.PlayConfig.Wind);
            if (wind.sqrMagnitude < 0.0001f)
            {
                return;
            }

            bool reduced = reducedMotion != null && reducedMotion();
            if (!MotionSettings.Allows(MotionEffect.WindStreaks, reduced))
            {
                return;
            }

            var go = new GameObject("WindStreaks");
            go.transform.SetParent(transform, false);
            go.AddComponent<WindStreaks>().Build(wind, min, max, seed);
        }

        /// <summary>
        /// Puts the day's wind on the course, when there is one. The wind
        /// lives in the config the ball is PLAYED under, not in the course
        /// data — a themed day turns a physics knob, it does not add geometry
        /// — so the vane asks the runner rather than the course.
        /// </summary>
        private void BuildWindVane(CourseData course, Vector2 min, Vector2 max)
        {
            if (runner == null)
            {
                return;
            }

            var wind = FixView.ToVector2(runner.PlayConfig.Wind);
            if (wind.sqrMagnitude < 0.0001f)
            {
                return;
            }

            // The quietest corner: the one whose nearest landmark — tee or cup
            // — is farthest off, so the badge never sits where the ball has
            // business. Deterministic, so a course always wears it in the same
            // place, and every player sees it in that place.
            var start = FixView.ToVector2(course.StartPosition);
            var hole = FixView.ToVector2(course.HolePosition);
            var corner = min;
            float quietest = -1f;
            for (int i = 0; i < 4; i++)
            {
                var candidate = new Vector2(i < 2 ? min.x : max.x, (i % 2) == 0 ? min.y : max.y);
                float nearest = Mathf.Min(
                    Vector2.Distance(candidate, start), Vector2.Distance(candidate, hole));
                if (nearest > quietest)
                {
                    quietest = nearest;
                    corner = candidate;
                }
            }

            // Pushed diagonally out of play, onto the fringe. The camera keeps
            // eight tenths of a unit of grass past the wall and the badge is
            // three tenths across, so it lands on grass and stays on screen at
            // the tightest fit.
            var outward = (corner - (min + max) * 0.5f).normalized;
            var vane = new GameObject("WindVane");
            vane.transform.SetParent(transform, false);
            vane.AddComponent<WindVane>().Build(wind, new Vector3(
                corner.x + outward.x * 0.42f, corner.y + outward.y * 0.42f, -0.03f));
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

                    // The conveyor read: one bright chevron gliding downhill.
                    // Decorative ambient motion, same class as the wind
                    // streaks, gated the same way — the static arrows keep
                    // carrying the direction without it.
                    if (!(reducedMotion != null && reducedMotion()))
                    {
                        var flowGo = new GameObject("RampFlow");
                        flowGo.transform.SetParent(transform, false);
                        flowGo.AddComponent<RampFlow>().Initialize(quad, dir);
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

            int portalIndex = 0;
            foreach (var portal in course.Portals)
            {
                var mouth = FixView.ToVector2(portal.Entry);
                float radius = FixView.ToFloat(portal.Radius);
                MeshFactory.CreateMeshObject(transform, "PortalGlow",
                    MeshFactory.Disc(mouth, radius, new Color(
                        PaletteMaterials.Portal.r, PaletteMaterials.Portal.g,
                        PaletteMaterials.Portal.b, 0.20f)), -0.018f);

                // The ring is a DASHED one on its own pivot, turning slowly —
                // paired mouths counter-rotate, and a thing that turns reads
                // as switched on. Origin-centred so the object can spin in
                // place (a mesh with absolute coordinates orbits the world
                // origin instead — the stripe lesson, learned once already).
                var ringGo = new GameObject("PortalRing");
                ringGo.transform.SetParent(transform, false);
                ringGo.transform.localPosition = new Vector3(mouth.x, mouth.y, -0.019f);
                const int dashes = 4;
                for (int d = 0; d < dashes; d++)
                {
                    float a0 = d * (360f / dashes) * Mathf.Deg2Rad;
                    var seg = new Vector2[9];
                    for (int k = 0; k <= 8; k++)
                    {
                        float ang = a0 + k * (300f / dashes / 8f) * Mathf.Deg2Rad;
                        seg[k] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius * 0.91f;
                    }

                    MeshFactory.CreateMeshObject(ringGo.transform, "PortalDash",
                        MeshFactory.Outline(seg, radius * 0.18f, PaletteMaterials.Portal,
                            closed: false), 0f);
                }

                if (!(reducedMotion != null && reducedMotion()))
                {
                    ringGo.AddComponent<SlowSpin>().degreesPerSecond =
                        portalIndex % 2 == 0 ? 22f : -22f;
                }

                MeshFactory.CreateMeshObject(transform, "PortalCore",
                    MeshFactory.Disc(mouth, radius * 0.30f, PaletteMaterials.Portal), -0.019f);
                portalIndex++;
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

                    // A cream cap on each tip — the fastest-moving point wears
                    // the brightest mark, so the sweep radius reads at a
                    // glance and the hub's cream dot has kin.
                    MeshFactory.CreateMeshObject(bladesGo.transform, "MillBladeTip",
                        MeshFactory.Disc(dir * blade, 0.05f,
                            new Color(0.97f, 0.96f, 0.90f)), -0.001f);
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
            if (name == "Stripes" || name == "MatShadow"
                || name == "RoughBlob" || name == "Fringe")
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

        /// <summary>
        /// Seed-derived subtle felt tint (±3% warm/cool shift), plus the
        /// day's weather: a themed day grades the LIGHT before the top bar
        /// gets a word in — icy cools the green a touch, bouncy warms it,
        /// windy washes it slightly pale. All of it small on purpose, all of
        /// it a pure function of seed and mutator, so every device grades the
        /// same day identically.
        /// </summary>
        private Color DailyTint(Color felt, ulong seed)
        {
            if (seed == 0)
            {
                return felt;
            }

            uint h = (uint)(seed ^ (seed >> 32)) * 2654435761u;
            float warm = ((h & 0xFF) / 255f - 0.5f) * 0.055f;
            float bright = (((h >> 8) & 0xFF) / 255f - 0.5f) * 0.04f;
            var tinted = new Color(
                Mathf.Clamp01(felt.r + warm + bright),
                Mathf.Clamp01(felt.g + bright),
                Mathf.Clamp01(felt.b - warm * 0.7f + bright),
                felt.a);

            switch (_mutator)
            {
                case PuttSeed.Core.Daily.DailyMutator.Icy:
                    return new Color(
                        Mathf.Clamp01(tinted.r * 0.93f),
                        Mathf.Clamp01(tinted.g * 1.00f),
                        Mathf.Clamp01(tinted.b * 1.09f), tinted.a);
                case PuttSeed.Core.Daily.DailyMutator.Bouncy:
                    return new Color(
                        Mathf.Clamp01(tinted.r * 1.07f),
                        Mathf.Clamp01(tinted.g * 1.00f),
                        Mathf.Clamp01(tinted.b * 0.93f), tinted.a);
                case PuttSeed.Core.Daily.DailyMutator.Windy:
                    float grey = (tinted.r + tinted.g + tinted.b) / 3f;
                    return new Color(
                        Mathf.Clamp01(Mathf.Lerp(tinted.r, grey, 0.12f) * 1.03f),
                        Mathf.Clamp01(Mathf.Lerp(tinted.g, grey, 0.12f) * 1.03f),
                        Mathf.Clamp01(Mathf.Lerp(tinted.b, grey, 0.12f) * 1.03f), tinted.a);
                default:
                    return tinted;
            }
        }

        /// <summary>A zone shrunk toward its centroid — the sand pit's inset fill.</summary>
        private static PuttSeed.Core.Sim.ZonePolygon ShrunkTowardCentroid(
            PuttSeed.Core.Sim.ZonePolygon zone, float factor)
        {
            var verts = zone.Vertices;
            var cx = PuttSeed.Core.FixedMath.Fix64.Zero;
            var cy = PuttSeed.Core.FixedMath.Fix64.Zero;
            for (int i = 0; i < verts.Length; i++)
            {
                cx += verts[i].X;
                cy += verts[i].Y;
            }

            var n = PuttSeed.Core.FixedMath.Fix64.FromInt(verts.Length);
            cx /= n;
            cy /= n;
            var k = PuttSeed.Core.FixedMath.Fix64.FromFraction((int)(factor * 1000f), 1000);
            var shrunk = new PuttSeed.Core.FixedMath.Vec2Fix[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                shrunk[i] = new PuttSeed.Core.FixedMath.Vec2Fix(
                    cx + (verts[i].X - cx) * k, cy + (verts[i].Y - cy) * k);
            }

            return new PuttSeed.Core.Sim.ZonePolygon(shrunk);
        }

        /// <summary>Bilinear point inside a quad zone (u along, v across).</summary>
        internal static Vector2 Bilerp(Vector2[] quad, float u, float v)
            => Vector2.Lerp(Vector2.Lerp(quad[0], quad[1], u), Vector2.Lerp(quad[3], quad[2], u), v);

        private static readonly Vector2 ShadowOffset = new Vector2(0.05f, -0.06f);
    }
}
