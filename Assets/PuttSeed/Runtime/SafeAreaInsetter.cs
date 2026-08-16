#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Insets its RectTransform to <see cref="Screen.safeArea"/> so notches
    /// and punch-holes never cover UI. Re-applies only when the area changes
    /// (rotation, foldables).
    /// </summary>
    public sealed class SafeAreaInsetter : MonoBehaviour
    {
        private Rect _applied;

        private void LateUpdate()
        {
            var safeArea = Screen.safeArea;
            if (safeArea == _applied || Screen.width == 0 || Screen.height == 0)
            {
                return;
            }

            _applied = safeArea;
            var rect = (RectTransform)transform;
            var screen = new Vector2(Screen.width, Screen.height);
            rect.anchorMin = safeArea.position / screen;
            rect.anchorMax = (safeArea.position + safeArea.size) / screen;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
