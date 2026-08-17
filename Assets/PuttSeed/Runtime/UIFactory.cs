#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>The UI palette and metrics, shared by menu, HUD and overlay.</summary>
    public static class UIStyle
    {
        /// <summary>Primary text color (warm off-white).</summary>
        public static readonly Color Cream = new Color(0.97f, 0.96f, 0.90f);

        /// <summary>Secondary text color.</summary>
        public static readonly Color CreamDim = new Color(0.97f, 0.96f, 0.90f, 0.62f);

        /// <summary>Card/panel background (deep green-black, translucent).</summary>
        public static readonly Color PanelDark = new Color(0.05f, 0.11f, 0.07f, 0.88f);

        /// <summary>Softer panel for chips and secondary surfaces.</summary>
        public static readonly Color PanelSoft = new Color(0.05f, 0.11f, 0.07f, 0.55f);

        /// <summary>Accent (warm amber) for the primary action.</summary>
        public static readonly Color Accent = new Color(0.99f, 0.76f, 0.29f);

        /// <summary>Text on accent surfaces.</summary>
        public static readonly Color AccentInk = new Color(0.16f, 0.10f, 0.02f);

        /// <summary>Hint/emphasis tint (soft yellow).</summary>
        public static readonly Color Hint = new Color(1f, 0.95f, 0.7f);
    }

    /// <summary>
    /// Shared code-built uGUI helpers used by the menu, the HUD and the
    /// loading overlay. The whole UI is constructed at runtime — no prefabs,
    /// no TMP, no image assets: even the rounded-corner and circle sprites are
    /// generated once and cached.
    /// </summary>
    public static class UIFactory
    {
        private static Sprite? _roundedSprite;
        private static Sprite? _circleSprite;
        private static Sprite? _starSprite;

        /// <summary>
        /// Uses imported sprite ASSETS instead of transient generated sprites.
        /// The editor scene builder calls this before baking UI into a scene,
        /// so saved scenes reference real assets the user can swap or repaint.
        /// </summary>
        public static void UseSpriteAssets(Sprite rounded, Sprite circle, Sprite star)
        {
            _roundedSprite = rounded;
            _circleSprite = circle;
            _starSprite = star;
        }

        /// <summary>The built-in legacy font.</summary>
        public static Font Font() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>A 9-sliced rounded-rectangle sprite (asset when set, else generated).</summary>
        public static Sprite RoundedSprite()
        {
            if (_roundedSprite == null)
            {
                _roundedSprite = BuildRoundedSprite(size: 64, radius: 20);
            }

            return _roundedSprite;
        }

        /// <summary>An anti-aliased filled circle sprite (asset when set, else generated).</summary>
        public static Sprite CircleSprite()
        {
            if (_circleSprite == null)
            {
                _circleSprite = BuildCircleSprite(size: 64);
            }

            return _circleSprite;
        }

        /// <summary>Generates the raw PNG bytes for the rounded-rect sprite asset.</summary>
        public static byte[] RoundedSpritePng()
        {
            var tex = BuildRoundedTexture(64, 20);
            var png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }

        /// <summary>An anti-aliased five-point star sprite (asset when set, else generated).</summary>
        public static Sprite StarSprite()
        {
            if (_starSprite == null)
            {
                _starSprite = BuildStarSprite(size: 64);
            }

            return _starSprite;
        }

        /// <summary>Generates the raw PNG bytes for the circle sprite asset.</summary>
        public static byte[] CircleSpritePng()
        {
            var tex = BuildCircleTexture(64);
            var png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }

        /// <summary>Generates the raw PNG bytes for the star sprite asset.</summary>
        public static byte[] StarSpritePng()
        {
            var tex = BuildStarTexture(64);
            var png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }

        /// <summary>
        /// Creates a screen-space canvas scaled for 1080x1920 portrait and
        /// returns its safe-area root — build all UI under that, so notches
        /// and punch-holes never cover controls.
        /// </summary>
        public static GameObject CreateCanvas(Transform parent)
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            var safeRoot = CreateRect(canvasGo.transform, "SafeArea", Vector2.zero, Vector2.one);
            safeRoot.gameObject.AddComponent<SafeAreaInsetter>();
            return safeRoot.gameObject;
        }

        /// <summary>Creates an anchored empty rect.</summary>
        public static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>Creates a rounded panel (card, bar, chip background).</summary>
        public static Image CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// A pop-up card that visibly separates from the felt: a light outline
        /// frame around a cool near-black body — clearly not the green board
        /// behind it. Returns the inner body image.
        /// </summary>
        public static Image CreateFramedCard(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var frame = CreatePanel(parent, name + "Frame", anchorMin, anchorMax,
                new Color(0.97f, 0.96f, 0.90f, 0.32f));
            var inner = CreateRect(frame.transform, name, Vector2.zero, Vector2.one);
            inner.offsetMin = new Vector2(4f, 4f);
            inner.offsetMax = new Vector2(-4f, -4f);
            var image = inner.gameObject.AddComponent<Image>();
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.035f, 0.055f, 0.095f, 0.985f);
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Creates a filled circle image (decorative shapes).</summary>
        public static Image CreateCircle(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = CircleSprite();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Creates an anchored text element (cream, optional soft shadow).</summary>
        public static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int size, TextAnchor align, bool shadow = false)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax);
            var text = rect.gameObject.AddComponent<Text>();
            rect.gameObject.AddComponent<LocalizedText>(); // baked English self-localizes at Awake
            text.font = Font();
            text.fontSize = size;
            text.alignment = align;
            text.color = UIStyle.Cream;
            text.raycastTarget = false;
            if (shadow)
            {
                var s = rect.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.45f);
                s.effectDistance = new Vector2(0f, -3f);
            }

            return text;
        }

        /// <summary>
        /// Creates a rounded flat button; <paramref name="primary"/> uses the
        /// amber accent for the main action. Returns the label for relabeling.
        /// </summary>
        public static Text CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
            UnityEngine.Events.UnityAction onClick, int fontSize = 34, bool primary = false)
        {
            var rect = CreateRect(parent, $"Button{label}", anchorMin, anchorMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = primary ? UIStyle.Accent : UIStyle.PanelDark;

            var button = rect.gameObject.AddComponent<Button>();
            rect.gameObject.AddComponent<UiClickSound>();
            rect.gameObject.AddComponent<ButtonPressScale>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var text = CreateText(rect, "Label", Vector2.zero, Vector2.one, fontSize, TextAnchor.MiddleCenter);
            text.text = label;
            text.color = primary ? UIStyle.AccentInk : UIStyle.Cream;
            return text;
        }

        /// <summary>Creates a rounded single-line input field with a placeholder.</summary>
        public static InputField CreateInputField(Transform parent, Vector2 anchorMin, Vector2 anchorMax, string placeholderText)
        {
            var rect = CreateRect(parent, "InputField", anchorMin, anchorMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.10f);
            var field = rect.gameObject.AddComponent<InputField>();

            var placeholder = CreateText(rect, "Placeholder", new Vector2(0.04f, 0f), Vector2.one, 30, TextAnchor.MiddleLeft);
            placeholder.text = placeholderText;
            placeholder.color = UIStyle.CreamDim;

            var text = CreateText(rect, "Text", new Vector2(0.04f, 0f), Vector2.one, 30, TextAnchor.MiddleLeft);
            text.supportRichText = false;

            field.textComponent = text;
            field.placeholder = placeholder;
            return field;
        }

        /// <summary>Creates the EventSystem once per scene.</summary>
        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private static Texture2D BuildRoundedTexture(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = RoundedRectAlpha(x, y, size, radius);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        private static Sprite BuildRoundedSprite(int size, int radius)
        {
            int border = radius + 4;
            return Sprite.Create(BuildRoundedTexture(size, radius), new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        private static float RoundedRectAlpha(int x, int y, int size, int radius)
        {
            // Distance to the nearest corner circle center when inside a corner
            // square; 1-pixel smoothstep edge for anti-aliasing.
            float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
            float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
            float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
            return Mathf.Clamp01(radius - dist + 0.5f);
        }

        private static Texture2D BuildCircleTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.5f - 1f;
            var center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - dist + 0.5f)));
                }
            }

            tex.Apply();
            return tex;
        }

        private static Sprite BuildCircleSprite(int size)
            => Sprite.Create(BuildCircleTexture(size), new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));

        private static Texture2D BuildStarTexture(int size)
        {
            // Ten-vertex star polygon (point up), rasterized with 3x3
            // supersampling for anti-aliased edges.
            var verts = new Vector2[10];
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float outer = size * 0.5f - 1.5f;
            float inner = outer * 0.45f;
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float radius = i % 2 == 0 ? outer : inner;
                verts[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < 3; sy++)
                    {
                        for (int sx = 0; sx < 3; sx++)
                        {
                            var p = new Vector2(x + (sx + 0.5f) / 3f, y + (sy + 0.5f) / 3f);
                            if (InsidePolygon(p, verts))
                            {
                                hits++;
                            }
                        }
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, hits / 9f));
                }
            }

            tex.Apply();
            return tex;
        }

        private static bool InsidePolygon(Vector2 p, Vector2[] verts)
        {
            bool inside = false;
            for (int i = 0, j = verts.Length - 1; i < verts.Length; j = i++)
            {
                if (verts[i].y > p.y != verts[j].y > p.y &&
                    p.x < (verts[j].x - verts[i].x) * (p.y - verts[i].y) / (verts[j].y - verts[i].y) + verts[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static Sprite BuildStarSprite(int size)
            => Sprite.Create(BuildStarTexture(size), new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
