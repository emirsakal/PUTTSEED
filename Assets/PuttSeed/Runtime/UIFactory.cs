#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Shared code-built uGUI helpers used by the menu and the in-game UI.
    /// The whole UI is constructed at runtime — no prefabs, no TMP — so
    /// scenes stay one-GameObject small and diffs stay reviewable.
    /// </summary>
    public static class UIFactory
    {
        /// <summary>The built-in legacy font.</summary>
        public static Font Font() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Creates a screen-space canvas scaled for 1080x1920 portrait.</summary>
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
            return canvasGo;
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

        /// <summary>Creates an anchored text element.</summary>
        public static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int size, TextAnchor align)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font();
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Creates a flat button; returns its label for later relabeling.</summary>
        public static Text CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick, int fontSize = 34)
        {
            var rect = CreateRect(parent, $"Button{label}", anchorMin, anchorMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.45f);
            var button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            var text = CreateText(rect, "Label", Vector2.zero, Vector2.one, fontSize, TextAnchor.MiddleCenter);
            text.text = label;
            return text;
        }

        /// <summary>Creates a single-line input field with a placeholder.</summary>
        public static InputField CreateInputField(Transform parent, Vector2 anchorMin, Vector2 anchorMax, string placeholderText)
        {
            var rect = CreateRect(parent, "InputField", anchorMin, anchorMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.12f);
            var field = rect.gameObject.AddComponent<InputField>();

            var placeholder = CreateText(rect, "Placeholder", Vector2.zero, Vector2.one, 30, TextAnchor.MiddleLeft);
            placeholder.text = placeholderText;
            placeholder.color = new Color(1f, 1f, 1f, 0.4f);

            var text = CreateText(rect, "Text", Vector2.zero, Vector2.one, 30, TextAnchor.MiddleLeft);
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
    }
}
