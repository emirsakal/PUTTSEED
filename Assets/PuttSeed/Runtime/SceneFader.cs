#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Cross-scene transition: a felt-green sweep slides across, the next
    /// scene loads underneath, and the sweep carries on out the other side —
    /// the same green the game lives on, with a cream leading edge, so even
    /// the cut between scenes speaks the house language. Input is blocked
    /// while covered.
    ///
    /// Under reduced motion the sweep becomes the old plain fade: a
    /// full-screen slide is exactly the kind of motion that setting removes.
    /// </summary>
    public static class SceneFader
    {
        private const float FadeOutSeconds = 0.18f;
        private const float FadeInSeconds = 0.22f;
        private const float SweepInSeconds = 0.24f;
        private const float SweepOutSeconds = 0.28f;

        /// <summary>Set by the bootstraps from the save — statics cannot read it.</summary>
        public static bool ReducedMotion;

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
            private RectTransform _coverRect = null!;
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
                _coverRect = rect;

                // The sweep's leading edge: a thin cream line on the cover's
                // left side, visible only while sliding.
                var edgeGo = new GameObject("SweepEdge");
                edgeGo.transform.SetParent(coverGo.transform, false);
                var edge = edgeGo.AddComponent<RectTransform>();
                edge.anchorMin = new Vector2(0f, 0f);
                edge.anchorMax = new Vector2(0f, 1f);
                edge.pivot = new Vector2(1f, 0.5f);
                edge.sizeDelta = new Vector2(6f, 0f);
                edge.anchoredPosition = Vector2.zero;
                var edgeImage = edgeGo.AddComponent<Image>();
                edgeImage.color = new Color(0.97f, 0.96f, 0.90f, 0.85f);
                edgeImage.raycastTarget = false;
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
                if (ReducedMotion)
                {
                    yield return FadeTo(1f, FadeOutSeconds);
                    SceneManager.LoadScene(sceneName);
                    yield return null; // let the new scene render its first frame
                    yield return FadeTo(0f, FadeInSeconds);
                }
                else
                {
                    SetAlpha(0f);
                    _cover.color = PaletteMaterials.Felt; // sweep wears the game's green
                    yield return Sweep(fromX: 1f, toX: 0f, SweepInSeconds);
                    SceneManager.LoadScene(sceneName);
                    yield return null;
                    yield return Sweep(fromX: 0f, toX: -1f, SweepOutSeconds);
                    _cover.color = new Color(0.05f, 0.11f, 0.07f, 0f); // back to fade duty
                    _coverRect.anchoredPosition = Vector2.zero;
                }

                _cover.raycastTarget = false;
                _busy = false;
            }

            /// <summary>Slides the cover across by screen widths (1 = offscreen right).</summary>
            private IEnumerator Sweep(float fromX, float toX, float seconds)
            {
                float width = _coverRect.rect.width;
                for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
                {
                    float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                    _coverRect.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, k) * width, 0f);
                    yield return null;
                }

                _coverRect.anchoredPosition = new Vector2(toX * width, 0f);
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
