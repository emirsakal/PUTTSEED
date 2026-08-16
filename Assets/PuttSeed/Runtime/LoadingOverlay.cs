#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Full-screen loading cover shown while a course is generated and its
    /// visuals are rebuilt, so no half-initialized frame (ball floating on an
    /// empty field) is ever visible. Blocks input while shown; animates
    /// trailing dots so multi-second generations read as alive.
    /// </summary>
    public sealed class LoadingOverlay : MonoBehaviour
    {
        private GameObject _root = null!;
        private Text _label = null!;
        private string _baseText = "";
        private float _dotTimer;

        /// <summary>True while the cover is visible.</summary>
        public bool IsShown => _root != null && _root.activeSelf;

        private void Awake()
        {
            var canvasGo = new GameObject("LoadingCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // above every other canvas
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var cover = UIFactory.CreateRect(canvasGo.transform, "Cover", Vector2.zero, Vector2.one);
            var image = cover.gameObject.AddComponent<Image>();
            image.color = PaletteMaterials.Felt;
            image.raycastTarget = true; // swallow taps while loading

            _label = UIFactory.CreateText(cover, "Label",
                new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.55f), 52, TextAnchor.MiddleCenter);

            _root = canvasGo;
            _root.SetActive(false);
        }

        /// <summary>Shows the cover with a message.</summary>
        public void Show(string text)
        {
            _baseText = text;
            _label.text = text;
            _dotTimer = 0f;
            _root.SetActive(true);
        }

        /// <summary>Hides the cover.</summary>
        public void Hide() => _root.SetActive(false);

        private void Update()
        {
            if (!IsShown)
            {
                return;
            }

            _dotTimer += Time.unscaledDeltaTime;
            int dots = 1 + (int)(_dotTimer * 2f) % 3;
            _label.text = _baseText + new string('.', dots);
        }
    }
}
