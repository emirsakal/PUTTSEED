#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// The game's one transition: a felt-green sweep slides across, whatever
    /// is changing changes underneath, and the sweep carries on out the other
    /// side — the same green the game lives on, with a cream leading edge, so
    /// even a cut speaks the house language. Input is blocked while covered.
    ///
    /// It covers two kinds of change, because to a player they are the same
    /// kind. Loading a SCENE is the obvious one. The other is the next level,
    /// the next lesson, the next gauntlet hole, another practice course —
    /// which stay in the game scene and used to arrive as a hard cut, so
    /// advancing a level felt unlike every other move in the game.
    ///
    /// Under reduced motion the sweep becomes a plain fade: a full-screen
    /// slide is exactly the kind of motion that setting removes.
    /// </summary>
    public static class SceneFader
    {
        private const float FadeOutSeconds = 0.18f;
        private const float FadeInSeconds = 0.22f;
        private const float SweepInSeconds = 0.24f;
        private const float SweepOutSeconds = 0.28f;

        // An in-scene swap is quicker than a scene load because it has less to
        // hide: the course is already grown, so the sweep is covering a single
        // frame rather than a scene coming up. A player advancing through
        // levels does this over and over, and the same 0.52 s that reads as
        // ceremony once reads as a toll the tenth time.
        private const float SwapInSeconds = 0.15f;
        private const float SwapOutSeconds = 0.19f;
        private const float SwapFadeSeconds = 0.12f;

        // How long the cover will wait for a swap that turns out not to be
        // instant (a practice course nobody pre-grew). Past this the sweep
        // leaves and LoadingOverlay takes the job — it shows after 150 ms and
        // has a putt vignette to fill the time, which a flat green cover does
        // not.
        private const float SwapBusyCapSeconds = 0.6f;

        /// <summary>Set by the bootstraps from the save — statics cannot read it.</summary>
        public static bool ReducedMotion;

        private static FaderHost? _host;

        /// <summary>Loads a scene behind the sweep (replaces LoadScene calls).</summary>
        public static void LoadScene(string sceneName)
            => Host().Load(sceneName);

        /// <summary>
        /// Runs <paramref name="swap"/> behind the same sweep without changing
        /// scene — the next level, lesson, hole or practice course.
        /// </summary>
        /// <param name="swap">The change, performed while the screen is covered.</param>
        /// <param name="busy">
        /// Optional "still working" probe. A swap that starts an async
        /// generation is not done when it returns, and revealing then would
        /// show the OLD course for a beat. Polled until it goes false, capped
        /// so a slow generation cannot strand the player behind a blank cover.
        /// </param>
        public static void Swap(System.Action swap, System.Func<bool>? busy = null)
            => Host().Swap(swap, busy);

        /// <summary>True while a sweep is running, so callers can refuse to stack them.</summary>
        public static bool IsBusy => _host != null && _host.Busy;

        private static FaderHost Host()
        {
            if (_host == null)
            {
                var go = new GameObject("SceneFader");
                Object.DontDestroyOnLoad(go);
                _host = go.AddComponent<FaderHost>();
            }

            return _host;
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

            /// <summary>True while a sweep is running.</summary>
            public bool Busy => _busy;

            public void Load(string sceneName)
            {
                if (!_busy)
                {
                    StartCoroutine(Run(() => SceneManager.LoadScene(sceneName), null,
                        SweepInSeconds, SweepOutSeconds, FadeOutSeconds, FadeInSeconds));
                }
            }

            public void Swap(System.Action swap, System.Func<bool>? busy)
            {
                if (!_busy)
                {
                    StartCoroutine(Run(swap, busy,
                        SwapInSeconds, SwapOutSeconds, SwapFadeSeconds, SwapFadeSeconds));
                }
            }

            private IEnumerator Run(System.Action change, System.Func<bool>? busy,
                float inSeconds, float outSeconds, float fadeOut, float fadeIn)
            {
                _busy = true;
                _cover.raycastTarget = true;
                if (ReducedMotion)
                {
                    yield return FadeTo(1f, fadeOut);
                    change();
                    yield return null; // let what changed render its first frame
                    yield return WaitWhileBusy(busy);
                    yield return FadeTo(0f, fadeIn);
                }
                else
                {
                    SetAlpha(0f);
                    _cover.color = PaletteMaterials.Felt; // sweep wears the game's green
                    yield return Sweep(fromX: 1f, toX: 0f, inSeconds);
                    change();
                    yield return null;
                    yield return WaitWhileBusy(busy);
                    yield return Sweep(fromX: 0f, toX: -1f, outSeconds);
                    _cover.color = new Color(0.05f, 0.11f, 0.07f, 0f); // back to fade duty
                    _coverRect.anchoredPosition = Vector2.zero;
                }

                _cover.raycastTarget = false;
                _busy = false;
            }

            /// <summary>
            /// Holds the cover while an async swap finishes, but only for so
            /// long: past the cap the sweep leaves and the loading overlay,
            /// which has something to look at, takes over.
            /// </summary>
            private IEnumerator WaitWhileBusy(System.Func<bool>? busy)
            {
                if (busy == null)
                {
                    yield break;
                }

                for (float t = 0f; t < SwapBusyCapSeconds && busy(); t += Time.unscaledDeltaTime)
                {
                    yield return null;
                }
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
