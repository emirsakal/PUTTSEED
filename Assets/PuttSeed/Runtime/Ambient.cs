#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// The ambient pad: one persistent looping source, faded toward silence
    /// whenever the sound toggle is off (it follows UiSounds.Enabled live).
    /// Deliberately self-contained — removing the pad is deleting this file,
    /// its clip, and the two EnsurePlaying calls in the bootstraps.
    /// </summary>
    public static class Ambient
    {
        private const float Volume = 0.16f;

        private static Host? _host;

        /// <summary>Starts the pad once; safe to call from every scene load.</summary>
        public static void EnsurePlaying()
        {
            if (_host == null)
            {
                var go = new GameObject("Ambient");
                Object.DontDestroyOnLoad(go);
                _host = go.AddComponent<Host>();
            }
        }

        private sealed class Host : MonoBehaviour
        {
            private AudioSource _source = null!;

            private void Awake()
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.clip = Resources.Load<AudioClip>("Sfx/ambient");
                _source.loop = true;
                _source.playOnAwake = false;
                _source.volume = 0f;
                if (_source.clip != null)
                {
                    _source.Play();
                }
            }

            private void Update()
            {
                float target = UiSounds.Enabled ? Volume : 0f;
                _source.volume = Mathf.MoveTowards(_source.volume, target, Time.unscaledDeltaTime * 0.25f);
            }
        }
    }
}
