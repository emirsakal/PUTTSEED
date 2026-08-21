#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PuttSeed.Unity.Editor
{
    /// <summary>
    /// Draws the art the stores ask for — the launcher icon, its adaptive
    /// layers, and the 1024x500 feature graphic — from the same palette the
    /// game paints itself with.
    ///
    /// It is code rather than a painting for the reason everything else here
    /// is: the felt green, the flag red and the cream are single sources of
    /// truth, and a hand-made PNG drifts from them the first time one changes.
    /// The icon this replaces was two hard-edged circles on flat green, which
    /// at launcher size read as a placeholder, because it was one.
    ///
    /// Everything is drawn at four times the final size and boxed down, so the
    /// edges come out smooth without a line of anti-aliasing code.
    /// </summary>
    public static class StoreArt
    {
        private const int Supersample = 4;

        private static readonly Color Felt = new Color(0.22f, 0.52f, 0.31f);
        private static readonly Color FeltLight = new Color(0.245f, 0.555f, 0.335f);
        private static readonly Color Rough = new Color(0.198f, 0.464f, 0.277f);
        private static readonly Color Cream = new Color(0.97f, 0.97f, 0.95f);
        private static readonly Color Flag = new Color(0.86f, 0.24f, 0.19f);
        private static readonly Color Hole = new Color(0.05f, 0.06f, 0.05f);
        private static readonly Color Shadow = new Color(0f, 0f, 0f, 0.22f);

        [MenuItem("PuttSeed/Generate Store Art")]
        public static void GenerateAll()
        {
            WriteIcon("Assets/PuttSeed/Icon/app-icon.png", 512, forAdaptive: false);
            WriteIcon("Assets/PuttSeed/Icon/adaptive-fg.png", 432, forAdaptive: true);
            WriteBackground("Assets/PuttSeed/Icon/adaptive-bg.png", 432);
            WriteFeatureGraphic("docs/store/feature-graphic.png", 1024, 500);

            AssetDatabase.Refresh();
            Debug.Log("PuttSeed: store art generated (icon, adaptive layers, feature graphic).");
        }

        /// <summary>
        /// The launcher icon: a ball, a cup and a flag on mown felt.
        ///
        /// The adaptive foreground is the same picture pulled inward and left
        /// transparent: a launcher may mask it to a circle, so the art has to
        /// survive losing its corners, and the felt belongs to the background
        /// layer instead.
        /// </summary>
        private static void WriteIcon(string path, int size, bool forAdaptive)
        {
            var art = new Painter(size, size);
            if (!forAdaptive)
            {
                PaintFelt(art, size, size, stripeHeight: size * 0.125f);
            }

            // A square composition reads at 48 pixels only if it is made of
            // three big shapes: ball, cup, flag. Anything finer turns to mud.
            //
            // The adaptive layer is not the same picture scaled: a launcher may
            // mask it to a circle of two thirds the canvas, so its scene is
            // placed against the CENTRE rather than the frame, and small enough
            // that the flag survives the mask. Scaling the framed version
            // instead keeps its off-centre balance and loses the flag.
            // Measured against the mask rather than guessed: the guaranteed
            // region is the middle two thirds, so everything has to sit inside
            // a circle of radius 0.33 from the centre. At the first numbers the
            // pennant's tip landed 1.2 times that out and a round launcher
            // would have cut it off.
            float scale = forAdaptive ? 0.62f : 1f;
            float cupX = forAdaptive ? 0.56f : 0.575f;
            float cupY = forAdaptive ? 0.46f : 0.42f;
            float ballX = forAdaptive ? 0.38f : 0.27f;
            float ballY = forAdaptive ? 0.4f : 0.33f;

            PaintHoleAndFlag(art, new Vector2(size * cupX, size * cupY), size * scale);
            PaintBall(art, new Vector2(size * ballX, size * ballY), size * 0.15f * scale);
            Write(art, path);
        }

        /// <summary>The adaptive background layer: felt to the edges, nothing else.</summary>
        private static void WriteBackground(string path, int size)
        {
            var art = new Painter(size, size);
            PaintFelt(art, size, size, stripeHeight: size * 0.125f);
            Write(art, path);
        }

        /// <summary>
        /// The feature graphic. Wide, so it can say what the icon cannot: a
        /// ball, the line it is about to take, and the hole waiting for it.
        /// No wordmark — the store prints the title beside this image already,
        /// and text baked into a picture cannot be localized.
        /// </summary>
        private static void WriteFeatureGraphic(string path, int width, int height)
        {
            var art = new Painter(width, height);
            PaintFelt(art, width, height, stripeHeight: height * 0.16f);

            // Two enormous, barely-there discs: the same trick the menu and the
            // course use to keep flat green from reading as paper. In the game
            // they are wider than the screen, so their edges are never seen;
            // here the first attempt put both edges INSIDE the frame and the
            // graphic grew two grey arcs that looked like a rendering fault.
            // Bigger and fainter: the wash stays, the outline goes.
            art.Disc(new Vector2(-width * 0.05f, height * 1.25f), height * 1.45f,
                new Color(Rough.r, Rough.g, Rough.b, 0.22f));
            art.Disc(new Vector2(width * 1.02f, -height * 0.3f), height * 1.35f,
                new Color(Rough.r, Rough.g, Rough.b, 0.18f));

            // The putt, dotted from ball to cup: the whole game in one line.
            var ball = new Vector2(width * 0.235f, height * 0.42f);
            var cup = new Vector2(width * 0.68f, height * 0.46f);
            for (float t = 0.12f; t < 0.9f; t += 0.052f)
            {
                art.Disc(Vector2.Lerp(ball, cup, t), height * 0.011f, new Color(1f, 1f, 1f, 0.3f));
            }

            PaintHoleAndFlag(art, cup, height * 1.05f);
            PaintBall(art, ball, height * 0.062f);
            Write(art, path);
        }

        private static void PaintFelt(Painter art, int width, int height, float stripeHeight)
        {
            art.Fill(Felt);
            for (float y = 0f; y < height; y += stripeHeight * 2f)
            {
                art.Rect(new Vector2(0f, y), new Vector2(width, y + stripeHeight), FeltLight);
            }
        }

        /// <summary>The cup, its pole and the pennant, all sized off one number.</summary>
        private static void PaintHoleAndFlag(Painter art, Vector2 cup, float scale)
        {
            float cupRadius = scale * 0.115f;
            art.Ellipse(cup + new Vector2(scale * 0.012f, -scale * 0.012f),
                cupRadius * 1.12f, cupRadius * 0.82f, Shadow);
            art.Ellipse(cup, cupRadius, cupRadius * 0.7f, Hole);

            // The pole rises from the BACK of the cup, so the two read as one
            // object standing in the ground rather than two stacked shapes.
            float poleWidth = scale * 0.019f;
            float poleTop = cup.y + scale * 0.4f;
            art.Rect(new Vector2(cup.x - poleWidth, cup.y),
                new Vector2(cup.x + poleWidth * 0.4f, poleTop), new Color(0.62f, 0.6f, 0.55f));
            art.Rect(new Vector2(cup.x - poleWidth, cup.y),
                new Vector2(cup.x - poleWidth * 0.2f, poleTop), Cream);

            art.Triangle(
                new Vector2(cup.x - poleWidth * 0.2f, poleTop),
                new Vector2(cup.x - poleWidth * 0.2f, poleTop - scale * 0.155f),
                new Vector2(cup.x + scale * 0.265f, poleTop - scale * 0.075f),
                Flag);
        }

        /// <summary>A lit ball with its shadow on the grass.</summary>
        private static void PaintBall(Painter art, Vector2 centre, float radius)
        {
            art.Ellipse(centre + new Vector2(radius * 0.22f, -radius * 0.86f),
                radius * 0.95f, radius * 0.4f, Shadow);
            art.Sphere(centre, radius, Cream);
        }

        private static void Write(Painter art, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var texture = art.Resolve();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        /// <summary>
        /// A supersampled pixel buffer with just enough shapes to draw a golf
        /// hole. Coordinates are in FINAL pixels with y up, which is how the
        /// rest of this project thinks about the world.
        /// </summary>
        private sealed class Painter
        {
            private readonly int _width;
            private readonly int _height;
            private readonly Color[] _pixels;

            public Painter(int width, int height)
            {
                _width = width * Supersample;
                _height = height * Supersample;
                _pixels = new Color[_width * _height];
            }

            public void Fill(Color color)
            {
                for (int i = 0; i < _pixels.Length; i++)
                {
                    _pixels[i] = color;
                }
            }

            public void Rect(Vector2 min, Vector2 max, Color color)
            {
                var lo = min * Supersample;
                var hi = max * Supersample;
                for (int y = Mathf.Max(0, (int)lo.y); y < Mathf.Min(_height, Mathf.CeilToInt(hi.y)); y++)
                {
                    for (int x = Mathf.Max(0, (int)lo.x); x < Mathf.Min(_width, Mathf.CeilToInt(hi.x)); x++)
                    {
                        Blend(x, y, color);
                    }
                }
            }

            public void Disc(Vector2 centre, float radius, Color color)
                => Ellipse(centre, radius, radius, color);

            public void Ellipse(Vector2 centre, float radiusX, float radiusY, Color color)
            {
                var c = centre * Supersample;
                float rx = radiusX * Supersample;
                float ry = radiusY * Supersample;
                for (int y = Mathf.Max(0, (int)(c.y - ry)); y < Mathf.Min(_height, Mathf.CeilToInt(c.y + ry)); y++)
                {
                    for (int x = Mathf.Max(0, (int)(c.x - rx)); x < Mathf.Min(_width, Mathf.CeilToInt(c.x + rx)); x++)
                    {
                        float dx = (x + 0.5f - c.x) / rx;
                        float dy = (y + 0.5f - c.y) / ry;
                        if (dx * dx + dy * dy <= 1f)
                        {
                            Blend(x, y, color);
                        }
                    }
                }
            }

            public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
            {
                a *= Supersample;
                b *= Supersample;
                c *= Supersample;
                int minX = Mathf.Max(0, (int)Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
                int maxX = Mathf.Min(_width, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
                int minY = Mathf.Max(0, (int)Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
                int maxY = Mathf.Min(_height, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
                for (int y = minY; y < maxY; y++)
                {
                    for (int x = minX; x < maxX; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        float d1 = Cross(p, a, b);
                        float d2 = Cross(p, b, c);
                        float d3 = Cross(p, c, a);
                        bool negative = d1 < 0f || d2 < 0f || d3 < 0f;
                        bool positive = d1 > 0f || d2 > 0f || d3 > 0f;
                        if (!(negative && positive))
                        {
                            Blend(x, y, color);
                        }
                    }
                }
            }

            /// <summary>A ball lit from the upper left — the menu emblem's own shading.</summary>
            public void Sphere(Vector2 centre, float radius, Color tint)
            {
                var c = centre * Supersample;
                float r = radius * Supersample;
                var light = new Vector2(-0.5f, 0.62f).normalized;
                for (int y = Mathf.Max(0, (int)(c.y - r)); y < Mathf.Min(_height, Mathf.CeilToInt(c.y + r)); y++)
                {
                    for (int x = Mathf.Max(0, (int)(c.x - r)); x < Mathf.Min(_width, Mathf.CeilToInt(c.x + r)); x++)
                    {
                        var d = new Vector2(x + 0.5f - c.x, y + 0.5f - c.y) / r;
                        float lengthSq = d.sqrMagnitude;
                        if (lengthSq > 1f)
                        {
                            continue;
                        }

                        float height = Mathf.Sqrt(1f - lengthSq);
                        float lambert = Mathf.Clamp01(Vector2.Dot(d, light) * 0.8f + height * 0.6f);
                        float spec = Mathf.Pow(Mathf.Clamp01(1f - (d - light * 0.5f).magnitude * 2.4f), 2f) * 0.45f;
                        float shade = Mathf.Clamp01(0.52f + lambert * 0.5f + spec);
                        Blend(x, y, new Color(tint.r * shade, tint.g * shade, tint.b * shade, 1f));
                    }
                }
            }

            public Texture2D Resolve()
            {
                int width = _width / Supersample;
                int height = _height / Supersample;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                float weight = 1f / (Supersample * Supersample);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var sum = new Color(0f, 0f, 0f, 0f);
                        for (int sy = 0; sy < Supersample; sy++)
                        {
                            for (int sx = 0; sx < Supersample; sx++)
                            {
                                sum += _pixels[(y * Supersample + sy) * _width + x * Supersample + sx];
                            }
                        }

                        texture.SetPixel(x, y, sum * weight);
                    }
                }

                texture.Apply();
                return texture;
            }

            private void Blend(int x, int y, Color color)
            {
                var destination = _pixels[y * _width + x];
                float alpha = color.a;
                _pixels[y * _width + x] = new Color(
                    Mathf.Lerp(destination.r, color.r, alpha),
                    Mathf.Lerp(destination.g, color.g, alpha),
                    Mathf.Lerp(destination.b, color.b, alpha),
                    Mathf.Max(destination.a, alpha));
            }

            private static float Cross(Vector2 p, Vector2 a, Vector2 b)
                => (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        }
    }
}
