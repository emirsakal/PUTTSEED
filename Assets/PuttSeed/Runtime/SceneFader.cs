#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Cross-scene fade: a persistent cover dips to the felt-dark panel color,
    /// the next scene loads underneath, and the cover lifts — no more hard
    /// cuts between menu and game. Input is blocked while fading.
    /// </summary>
    public static class SceneFader
    {
        private const float FadeOutSeconds = 0.18f;
        private const float FadeInSeconds = 0.22f;

        private static FaderHost? _host;

        /// <summary>Loads a scene behind a quick fade (replaces LoadScene calls).</summary>
        public static void LoadScene(string sceneName)
        {
            if (_host == null)
            {
                var go = new GameObject("SceneFader");
                Object.DontDestroyOnLoad(go);
                _host = go.AddComponent<FaderHost>();
            }

            _host.Load(sceneName);
        }

        private sealed class FaderHost : MonoBehaviour
        {
            private Image _cover = null!;
            private bool _busy;

            private void Awake()
            {
                var canvasGo = new GameObject("FadeCanvas");
                canvasGo.transform.SetParent(transform, false);
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500; // above the loading overlay (100)

                var coverGo = new GameObject("Cover");
                coverGo.transform.SetParent(canvasGo.transform, false);
                var rect = coverGo.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                _cover = coverGo.AddComponent<Image>();
                _cover.color = new Color(0.05f, 0.11f, 0.07f, 0f);
                _cover.raycastTarget = false;
            }

            public void Load(string sceneName)
            {
                if (!_busy)
                {
                    StartCoroutine(Run(sceneName));
                }
            }

            private IEnumerator Run(string sceneName)
            {
                _busy = true;
                _cover.raycastTarget = true;
                yield return FadeTo(1f, FadeOutSeconds);
                SceneManager.LoadScene(sceneName);
                yield return null; // let the new scene render its first frame
                yield return FadeTo(0f, FadeInSeconds);
                _cover.raycastTarget = false;
                _busy = false;
            }

            private IEnumerator FadeTo(float target, float seconds)
            {
                float from = _cover.color.a;
                for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
                {
                    SetAlpha(Mathf.Lerp(from, target, t / seconds));
                    yield return null;
                }

                SetAlpha(target);
            }

            private void SetAlpha(float alpha)
            {
                var c = _cover.color;
                _cover.color = new Color(c.r, c.g, c.b, alpha);
            }
        }
    }
}
