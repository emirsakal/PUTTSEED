#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// One persistent click player for all UI buttons. Bootstraps sync
    /// <see cref="Enabled"/> from the saved sound toggle.
    /// </summary>
    public static class UiSounds
    {
        /// <summary>Follows the settings' sound toggle.</summary>
        public static bool Enabled = true;

        private static AudioSource? _source;

        /// <summary>Plays the synthesized UI tick (no-op while muted).</summary>
        public static void Click() => Play(1f);

        /// <summary>The closing counterpart: the same tick, pitched down.</summary>
        public static void ClickDown() => Play(0.76f);

        private static void Play(float pitch)
        {
            if (!Enabled)
            {
                return;
            }

            if (_source == null)
            {
                var go = new GameObject("UiSounds");
                Object.DontDestroyOnLoad(go);
                _source = go.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.clip = Resources.Load<AudioClip>("Sfx/click");
            }

            if (_source.clip != null)
            {
                _source.pitch = pitch * Random.Range(0.97f, 1.03f);
                _source.PlayOneShot(_source.clip, 0.55f);
            }
        }
    }

    /// <summary>
    /// Scene-serializable click hookup: UnityEvent listeners added while
    /// BAKING a scene do not survive serialization, so every generated button
    /// carries this component and wires the click sound at runtime instead.
    /// Close/back buttons mark <see cref="downTone"/> for the descending tick.
    /// </summary>
    public sealed class UiClickSound : MonoBehaviour
    {
        /// <summary>True on closing controls — plays the pitched-down tick.</summary>
        public bool downTone;

        private void Awake()
        {
            var button = GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    if (downTone)
                    {
                        UiSounds.ClickDown();
                    }
                    else
                    {
                        UiSounds.Click();
                    }
                });
            }
        }
    }

    /// <summary>
    /// Scene-serializable press feedback: the button dips to 96% while held.
    /// </summary>
    public sealed class ButtonPressScale : MonoBehaviour,
        UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
            => transform.localScale = Vector3.one * 0.96f;

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
            => transform.localScale = Vector3.one;
    }
}
