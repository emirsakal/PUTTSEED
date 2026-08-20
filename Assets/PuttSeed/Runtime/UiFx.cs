#nullable enable
using System.Collections;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>Tiny UI motion helpers: panel pop-ins and toast slides.</summary>
    public static class UiFx
    {
        /// <summary>Activates a panel with a quick scale + fade pop.</summary>
        public static void PopIn(MonoBehaviour host, GameObject panel)
        {
            panel.SetActive(true);
            host.StartCoroutine(PopRoutine(panel));
        }

        private static IEnumerator PopRoutine(GameObject panel)
        {
            var group = panel.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = panel.AddComponent<CanvasGroup>();
            }

            const float duration = 0.15f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (panel == null || !panel.activeSelf)
                {
                    yield break; // closed mid-pop
                }

                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                group.alpha = k;
                panel.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, k);
                yield return null;
            }

            if (panel != null)
            {
                group.alpha = 1f;
                panel.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Activates a chip sliding up into place while fading in.
        ///
        /// The slide offsets localPosition, and it used to leave it offset
        /// whenever the chip was hidden mid-flight — which is the ordinary way
        /// this chip closes. The next slide then read that displaced spot as
        /// the rest position and settled there, so the import field walked 26
        /// pixels down the screen every time it was opened until it sat on top
        /// of the button row. The routine now restores the rest position on
        /// every exit, interruption included.
        /// </summary>
        public static Coroutine SlideUp(MonoBehaviour host, GameObject chip)
        {
            chip.SetActive(true);
            return host.StartCoroutine(SlideRoutine(chip));
        }

        private static IEnumerator SlideRoutine(GameObject chip)
        {
            var group = chip.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = chip.AddComponent<CanvasGroup>();
            }

            var basePos = chip.transform.localPosition;
            const float duration = 0.16f;
            const float rise = 26f;
            try
            {
                for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
                {
                    if (chip == null || !chip.activeSelf)
                    {
                        yield break;
                    }

                    float k = Mathf.SmoothStep(0f, 1f, t / duration);
                    group.alpha = k;
                    chip.transform.localPosition = basePos + Vector3.down * (rise * (1f - k));
                    yield return null;
                }
            }
            finally
            {
                // Every exit lands here: finished, hidden mid-slide, or the
                // coroutine stopped. Leaving the chip where the animation had
                // it is what made it creep down the screen.
                if (chip != null)
                {
                    group.alpha = 1f;
                    chip.transform.localPosition = basePos;
                }
            }
        }
    }
}
