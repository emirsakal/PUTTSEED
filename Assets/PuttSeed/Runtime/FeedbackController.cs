#nullable enable
using System.Collections;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// All game feel feedback in one watcher: audio (synthesized default
    /// clips from Resources/Sfx, overridable per slot — empty slots are
    /// silently skipped), haptics, ball squash on impacts and the hole-in
    /// celebration ring. Observes the sim's deterministic event counters;
    /// never influences the simulation.
    /// </summary>
    public sealed class FeedbackController : MonoBehaviour
    {
        [Header("Audio (empty slots fall back to the synthesized Resources/Sfx clips)")]
        public AudioClip? shotClip;
        public AudioClip? wallClip;
        public AudioClip? bumperClip;
        public AudioClip? captureClip;
        public AudioClip? waterClip;
        public AudioClip? failClip;

        [Header("Tuning")]
        [Range(0f, 1f)] public float volume = 0.9f;
        [Tooltip("Minimum seconds between bounce sounds (grazing-contact spam guard).")]
        public float bounceSoundCooldown = 0.06f;

        private SimRunner _runner = null!;
        private BallView _ballView = null!;
        private AudioSource _source = null!;
        private StatsStore? _settings;

        private int _lastWallHits;
        private int _lastBumperHits;
        private int _lastWaterEntries;
        private bool _lastHoled;
        private bool _lastFailed;
        private float _lastBounceSoundTime;

        /// <summary>
        /// Fills any empty clip slot with the synthesized defaults generated
        /// by the SfxSynth editor tool (PuttSeed → Generate SFX).
        /// </summary>
        public void LoadDefaultClips()
        {
            if (shotClip == null) { shotClip = Resources.Load<AudioClip>("Sfx/shot"); }
            if (wallClip == null) { wallClip = Resources.Load<AudioClip>("Sfx/wall"); }
            if (bumperClip == null) { bumperClip = Resources.Load<AudioClip>("Sfx/bumper"); }
            if (captureClip == null) { captureClip = Resources.Load<AudioClip>("Sfx/capture"); }
            if (waterClip == null) { waterClip = Resources.Load<AudioClip>("Sfx/water"); }
            if (failClip == null) { failClip = Resources.Load<AudioClip>("Sfx/fail"); }
        }

        /// <summary>The settings source (menu toggles); null means everything on.</summary>
        public void SetSettings(StatsStore settings) => _settings = settings;

        /// <summary>Wires dependencies (called by the bootstrap).</summary>
        public void Initialize(SimRunner runner, BallView ballView)
        {
            _runner = runner;
            _ballView = ballView;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            runner.ShotFired += OnShotFired;
            runner.RunReset += SyncCounters;
            runner.StateChanged += OnStateChanged;
            SyncCounters();
        }

        private void SyncCounters()
        {
            var sim = _runner.Sim;
            if (sim == null)
            {
                return;
            }

            _lastWallHits = sim.WallHitCount;
            _lastBumperHits = sim.BumperHitCount;
            _lastWaterEntries = sim.WaterEntryCount;
            _lastHoled = sim.IsHoled;
            _lastFailed = sim.IsFailed;
        }

        private void OnShotFired()
        {
            Play(shotClip, 1f);
        }

        private void OnStateChanged()
        {
            var sim = _runner.Sim;
            if (sim == null)
            {
                return;
            }

            if (sim.WallHitCount > _lastWallHits)
            {
                OnBounce(wallClip);
            }

            if (sim.BumperHitCount > _lastBumperHits)
            {
                OnBounce(bumperClip);
                Tap();
            }

            if (sim.WaterEntryCount > _lastWaterEntries)
            {
                Play(waterClip, 1f);
                Tap();
            }

            if (sim.IsHoled && !_lastHoled)
            {
                Play(captureClip, 1f);
                Tap();
                StartCoroutine(CelebrationRing(FixView.ToVector2(_runner.Generation!.Course.HolePosition)));
            }

            if (sim.IsFailed && !_lastFailed)
            {
                Play(failClip, 0.8f);
            }

            _lastWallHits = sim.WallHitCount;
            _lastBumperHits = sim.BumperHitCount;
            _lastWaterEntries = sim.WaterEntryCount;
            _lastHoled = sim.IsHoled;
            _lastFailed = sim.IsFailed;
        }

        private void OnBounce(AudioClip? clip)
        {
            _ballView.Squash();
            if (Time.unscaledTime - _lastBounceSoundTime >= bounceSoundCooldown)
            {
                _lastBounceSoundTime = Time.unscaledTime;
                Play(clip, Random.Range(0.85f, 1f));
            }
        }

        private void Play(AudioClip? clip, float gain)
        {
            if (clip != null && (_settings == null || _settings.Data.soundEnabled))
            {
                _source.pitch = Random.Range(0.96f, 1.04f);
                _source.PlayOneShot(clip, volume * gain);
            }
        }

        private void Tap()
        {
            if (_settings == null || _settings.Data.hapticsEnabled)
            {
                HapticsPlayer.Tap();
            }
        }

        /// <summary>Flat expanding ring at the hole, fading out over ~0.8 s.</summary>
        private IEnumerator CelebrationRing(Vector2 center)
        {
            const int segments = 48;
            var go = new GameObject("CelebrationRing");
            var line = go.AddComponent<LineRenderer>();
            line.loop = true;
            line.positionCount = segments;
            line.widthMultiplier = 0.06f;
            line.material = PaletteMaterials.Shared;
            line.sortingOrder = 20;

            const float duration = 0.8f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float radius = 0.2f + k * 1.6f;
                var color = new Color(1f, 1f, 1f, 1f - k);
                line.startColor = color;
                line.endColor = color;
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * 2f * Mathf.PI / segments;
                    line.SetPosition(i, new Vector3(
                        center.x + Mathf.Cos(angle) * radius,
                        center.y + Mathf.Sin(angle) * radius,
                        -0.5f));
                }

                yield return null;
            }

            Destroy(go);
        }
    }

    /// <summary>Coarse device haptics; no-op in the editor and on non-Android.</summary>
    public static class HapticsPlayer
    {
        /// <summary>A short tap on impact/capture events.</summary>
        public static void Tap()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
