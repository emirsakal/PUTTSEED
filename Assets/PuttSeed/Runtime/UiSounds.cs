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
    /// The no-silent-buttons guarantee: bootstraps sweep the scene (inactive
    /// panels included) and fit every Button that slipped through generation
    /// with the click sound and press scale. Idempotent.
    /// </summary>
    public static class UiPolish
    {
        public static void EnsureButtonFeedback()
        {
            var buttons = Object.FindObjectsByType<UnityEngine.UI.Button>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var button in buttons)
            {
                if (button.GetComponent<UiClickSound>() == null)
                {
                    button.gameObject.AddComponent<UiClickSound>();
                }

                if (button.GetComponent<ButtonPressScale>() == null)
                {
                    button.gameObject.AddComponent<ButtonPressScale>();
                }
            }
        }
    }
}
