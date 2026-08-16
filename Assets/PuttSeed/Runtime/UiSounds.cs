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
        public static void Click()
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
                _source.pitch = Random.Range(0.96f, 1.04f);
                _source.PlayOneShot(_source.clip, 0.55f);
            }
        }
    }

    /// <summary>
    /// Scene-serializable click hookup: UnityEvent listeners added while
    /// BAKING a scene do not survive serialization, so every generated button
    /// carries this component and wires the click sound at runtime instead.
    /// </summary>
    public sealed class UiClickSound : MonoBehaviour
    {
        private void Awake()
        {
            var button = GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.AddListener(UiSounds.Click);
            }
        }
    }
}
