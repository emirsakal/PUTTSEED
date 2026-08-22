#nullable enable
using PuttSeed.Core.Sim;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Draws a course into a small texture, so the menu can show TODAY'S hole
    /// instead of a date string: the day's shape becomes the day's identity.
    /// Pure presentation — it reads geometry and writes pixels, and follows
    /// the active palette so the colorblind setting carries over.
    /// </summary>
    public static class CourseThumbnail
    {
        /// <summary>
        /// Renders the course at <paramref name="longSide"/> pixels on its
        /// longer axis. The texture's aspect follows the course's own bounds,
        /// so a Sprite with preserveAspect never stretches the hole.
        /// </summary>
        public static Texture2D Render(CourseData course, int longSide = 320)
        {
            longSide = Mathf.Clamp(longSide, 32, 1024);

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var wall in course.Walls)
            {
                var a = FixView.ToVector2(wall.A);
                var b = FixView.ToVector2(wall.B);
                min = Vector2.Min(min, Vector2.Min(a, b));
                max = Vector2.Max(max, Vector2.Max(a, b));
            }

            if (course.Walls.Length == 0)
            {
                min = Vector2.zero;
                max = Vector2.one;
            }

            // A little air so the outer wall never sits flush against the edge.
            var pad = new Vector2(0.45f, 0.45f);
            min -= pad;
            max += pad;
            var span = Vector2.Max(max - min, new Vector2(0.01f, 0.01f));

            int width, height;
            if (span.x >= span.y)
            {
                width = longSide;
                height = Mathf.Clamp(Mathf.RoundToInt(longSide * span.y / span.x), 16, longSide);
            }
            else
            {
                height = longSide;
                width = Mathf.Clamp(Mathf.RoundToInt(longSide * span.x / span.y), 16, longSide);
            }

            // Wall strokes stay legible at thumbnail scale: at least a pixel
            // and a bit, whatever the course's size on screen.
            float unitsPerPixel = span.x / width;
            float wallHalf = Mathf.Max(unitsPerPixel * 1.1f, 0.07f);

            var hole = FixView.ToVector2(course.HolePosition);
            var start = FixView.ToVector2(course.StartPosition);
            // The cup is 0.15 in the sim; at thumbnail scale it has to be a
            // mark you can find, not a scale model.
            float holeRadius = Mathf.Max(0.22f, unitsPerPixel * 2.5f);
            float ballRadius = Mathf.Max(unitsPerPixel * 1.6f, 0.09f);

            Color32 felt = PaletteMaterials.Felt;
            Color32 wallInk = PaletteMaterials.Wall;
            Color32 holeInk = PaletteMaterials.Hole;
            Color32 ballInk = PaletteMaterials.Ball;
            Color32 sand = PaletteMaterials.SandColor;
            Color32 water = PaletteMaterials.WaterColor;
            Color32 ice = PaletteMaterials.IceColor;
            Color32 bumper = PaletteMaterials.BumperColor;
            Color32 gate = PaletteMaterials.Gate;
            Color32 portal = PaletteMaterials.Portal;

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float wy = min.y + (y + 0.5f) * span.y / height;
                for (int x = 0; x < width; x++)
                {
                    var p = new Vector2(min.x + (x + 0.5f) * span.x / width, wy);

                    // Priority is readability, not physics: the marks a player
                    // looks for first win over the ground under them.
                    Color32 c;
                    bool onGate = WithinGate(p, course.Gates, wallHalf);
                    if (onGate || WithinWall(p, course.Walls, wallHalf))
                    {
                        c = onGate ? gate : wallInk;
                    }
                    else if ((p - hole).sqrMagnitude <= holeRadius * holeRadius)
                    {
                        c = holeInk;
                    }
                    else if ((p - start).sqrMagnitude <= ballRadius * ballRadius)
                    {
                        c = ballInk;
                    }
                    else if (WithinDisc(p, course.Bumpers))
                    {
                        c = bumper;
                    }
                    else if (WithinPortal(p, course.Portals))
                    {
                        c = portal;
                    }
                    else if (InAnyZone(p, course.WaterZones))
                    {
                        c = water;
                    }
                    else if (InAnyZone(p, course.IceZones))
                    {
                        c = ice;
                    }
                    else if (InAnyZone(p, course.SandZones) || InAnyRamp(p, course.Ramps))
                    {
                        c = sand;
                    }
                    else
                    {
                        c = felt;
                    }

                    pixels[y * width + x] = c;
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// The course as an ETCHING: transparent ground, cream walls, a dot
        /// for the cup — a watermark, not a picture.
        ///
        /// The journey grid tried the full-colour render first and it looked
        /// exactly as wrong as it sounds: bright felt-green cards scattered
        /// among dark cells, each thumbnail shouting its own palette inside a
        /// panel that whispers. A cell's picture has to speak the CELL's
        /// language — one ink, no ground — so the level number and stars stay
        /// the loudest things in it.
        /// </summary>
        public static Texture2D RenderEtch(CourseData course, int longSide = 96)
        {
            longSide = Mathf.Clamp(longSide, 32, 512);

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var wall in course.Walls)
            {
                var a = FixView.ToVector2(wall.A);
                var b = FixView.ToVector2(wall.B);
                min = Vector2.Min(min, Vector2.Min(a, b));
                max = Vector2.Max(max, Vector2.Max(a, b));
            }

            if (course.Walls.Length == 0)
            {
                min = Vector2.zero;
                max = Vector2.one;
            }

            var pad = new Vector2(0.45f, 0.45f);
            min -= pad;
            max += pad;
            var span = Vector2.Max(max - min, new Vector2(0.01f, 0.01f));

            int width, height;
            if (span.x >= span.y)
            {
                width = longSide;
                height = Mathf.Clamp(Mathf.RoundToInt(longSide * span.y / span.x), 16, longSide);
            }
            else
            {
                height = longSide;
                width = Mathf.Clamp(Mathf.RoundToInt(longSide * span.x / span.y), 16, longSide);
            }

            float unitsPerPixel = span.x / width;
            float wallHalf = Mathf.Max(unitsPerPixel * 1.2f, 0.08f);
            var hole = FixView.ToVector2(course.HolePosition);
            float holeRadius = Mathf.Max(0.24f, unitsPerPixel * 2.5f);

            var ink = new Color32(247, 245, 230, 255);
            var faint = new Color32(247, 245, 230, 110);
            var clear = new Color32(0, 0, 0, 0);

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float wy = min.y + (y + 0.5f) * span.y / height;
                for (int x = 0; x < width; x++)
                {
                    var p = new Vector2(min.x + (x + 0.5f) * span.x / width, wy);
                    Color32 c;
                    if (WithinWall(p, course.Walls, wallHalf))
                    {
                        c = ink;
                    }
                    else if ((p - hole).sqrMagnitude <= holeRadius * holeRadius)
                    {
                        c = ink;
                    }
                    else if (WithinDisc(p, course.Bumpers) || WithinPortal(p, course.Portals))
                    {
                        c = faint; // hazards are texture in an etching, not colour
                    }
                    else
                    {
                        c = clear;
                    }

                    pixels[y * width + x] = c;
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static bool WithinWall(Vector2 p, WallSegment[] walls, float halfWidth)
        {
            float halfSq = halfWidth * halfWidth;
            for (int i = 0; i < walls.Length; i++)
            {
                if (DistanceToSegmentSq(p, FixView.ToVector2(walls[i].A), FixView.ToVector2(walls[i].B)) <= halfSq)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool WithinGate(Vector2 p, OneWayGate[] gates, float halfWidth)
        {
            float halfSq = halfWidth * halfWidth;
            for (int i = 0; i < gates.Length; i++)
            {
                if (DistanceToSegmentSq(p, FixView.ToVector2(gates[i].A), FixView.ToVector2(gates[i].B)) <= halfSq)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool WithinDisc(Vector2 p, Bumper[] bumpers)
        {
            for (int i = 0; i < bumpers.Length; i++)
            {
                float r = FixView.ToFloat(bumpers[i].Radius);
                if ((p - FixView.ToVector2(bumpers[i].Center)).sqrMagnitude <= r * r)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Both mouths of every pair — a portal you cannot see the
        /// far end of is a trapdoor, not a portal.</summary>
        private static bool WithinPortal(Vector2 p, Portal[] portals)
        {
            for (int i = 0; i < portals.Length; i++)
            {
                float r = FixView.ToFloat(portals[i].Radius);
                float rSq = r * r;
                if ((p - FixView.ToVector2(portals[i].Entry)).sqrMagnitude <= rSq
                    || (p - FixView.ToVector2(portals[i].Exit)).sqrMagnitude <= rSq)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool InAnyRamp(Vector2 p, RampZone[] ramps)
        {
            for (int i = 0; i < ramps.Length; i++)
            {
                if (InPolygon(p, ramps[i].Area))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool InAnyZone(Vector2 p, ZonePolygon[] zones)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (InPolygon(p, zones[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Even-odd crossing test in render space. The sim has its own
        /// fixed-point Contains; this one exists so the thumbnail never
        /// converts floats back into Fix64 — the boundary stays one-way.
        /// </summary>
        private static bool InPolygon(Vector2 p, ZonePolygon zone)
        {
            var verts = zone.Vertices;
            bool inside = false;
            for (int i = 0, j = verts.Length - 1; i < verts.Length; j = i++)
            {
                var a = FixView.ToVector2(verts[i]);
                var b = FixView.ToVector2(verts[j]);
                if (a.y > p.y != b.y > p.y
                    && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static float DistanceToSegmentSq(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            if (lengthSq < 1e-8f)
            {
                return (p - a).sqrMagnitude;
            }

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
            return (p - (a + ab * t)).sqrMagnitude;
        }
    }
}
