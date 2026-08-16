#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Full-screen loading cover shown while a course is generated and its
    /// visuals are rebuilt. The cover hierarchy is scene-authored (baked by
    /// PuttSeed → Rebuild Scenes; reskinnable in the Inspector); this
    /// component only toggles it and animates the trailing dots.
    /// </summary>
    public sealed class LoadingOverlay : MonoBehaviour
    {
        [Header("Scene-authored UI (assigned by PuttSeed → Rebuild Scenes)")]
        public GameObject? root;
        public Text? label;

        private string _baseText = "";
        private float _dotTimer;

        /// <summary>True while the cover is visible.</summary>
        public bool IsShown => root != null && root.activeSelf;

        /// <summary>Shows the cover with a message.</summary>
        public void Show(string text)
        {
            _baseText = text;
            _dotTimer = 0f;
            if (label != null)
            {
                label.text = text;
            }

            root?.SetActive(true);
        }

        /// <summary>Hides the cover.</summary>
        public void Hide() => root?.SetActive(false);

        private void Update()
        {
            if (!IsShown || label == null)
            {
                return;
            }

            _dotTimer += Time.unscaledDeltaTime;
            int dots = 1 + (int)(_dotTimer * 2f) % 3;
            label.text = _baseText + new string('.', dots);
        }
    }
}
