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

        private ParticleSystem _burstPs = null!;
        private ParticleSystem _confettiPs = null!;
        private bool _wasInSand;
        private Vector2 _lastBallPos;

        private static readonly Color SandPuff = new Color(0.85f, 0.78f, 0.55f);
        private static readonly Color WaterSplash = new Color(0.42f, 0.62f, 0.88f);
        private static readonly Color[] ConfettiColors =
        {
            new Color(0.99f, 0.76f, 0.29f), // accent amber
            new Color(0.97f, 0.96f, 0.90f), // cream
            new Color(0.45f, 0.85f, 0.45f), // easy green
            new Color(0.95f, 0.36f, 0.30f), // hard red
        };

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
            _burstPs = CreateParticleSystem("Bursts", gravity: 0f);
            _confettiPs = CreateParticleSystem("Confetti", gravity: 0.7f);

            runner.ShotFired += OnShotFired;
            runner.RunReset += SyncCounters;
            runner.StateChanged += OnStateChanged;
            SyncCounters();
        }

        /// <summary>A world-space burst emitter drawn above the course meshes.</summary>
        private ParticleSystem CreateParticleSystem(string name, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.maxParticles = 128;
            var emission = ps.emission;
            emission.enabled = false; // bursts come from Emit() only
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = PaletteMaterials.Shared;
            renderer.sortingOrder = 25;
            return ps;
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
            _wasInSand = false;
            _lastBallPos = FixView.ToVector2(sim.Ball.Position);
        }

        /// <summary>Read-only zone lookup for presentation (core's own test).</summary>
        private bool BallIsInSand()
        {
            var course = _runner.Generation?.Course;
            var sim = _runner.Sim;
            if (course == null || sim == null)
            {
                return false;
            }

            for (int i = 0; i < course.SandZones.Length; i++)
            {
                if (course.SandZones[i].Contains(sim.Ball.Position))
                {
                    return true;
                }
            }

            return false;
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
                // The sim already snapped the ball back to its last rest, so
                // splash where it was LAST frame — right at the water's edge.
                EmitBurst(_burstPs, _lastBallPos, WaterSplash, count: 14, speed: 1.6f, life: 0.5f);
            }

            bool inSand = BallIsInSand();
            if (inSand && !_wasInSand)
            {
                EmitBurst(_burstPs, FixView.ToVector2(sim.Ball.Position), SandPuff, count: 8, speed: 0.8f, life: 0.4f);
            }

            _wasInSand = inSand;

            if (sim.IsHoled && !_lastHoled)
            {
                Play(captureClip, 1f);
                Tap();
                var hole = FixView.ToVector2(_runner.Generation!.Course.HolePosition);
                StartCoroutine(CelebrationRing(hole));
                if (PuttSeed.Core.Sim.Scoring.Stars(sim.Strokes, _runner.Generation.Course.Par) == 3)
                {
                    EmitConfetti(hole);
                }
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
            _lastBallPos = FixView.ToVector2(sim.Ball.Position);
        }

        /// <summary>Radial burst of flat-color particles at a world position.</summary>
        private static void EmitBurst(ParticleSystem ps, Vector2 center, Color color, int count, float speed, float life)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, 2f * Mathf.PI);
                float v = speed * Random.Range(0.5f, 1f);
                ps.Emit(new ParticleSystem.EmitParams
                {
                    position = new Vector3(center.x, center.y, -0.4f),
                    velocity = new Vector3(Mathf.Cos(angle) * v, Mathf.Sin(angle) * v, 0f),
                    startColor = color,
                    startSize = Random.Range(0.05f, 0.11f),
                    startLifetime = life * Random.Range(0.7f, 1f),
                }, 1);
            }
        }

        /// <summary>Three-star celebration: palette confetti raining off the cup.</summary>
        private void EmitConfetti(Vector2 hole)
        {
            for (int i = 0; i < 42; i++)
            {
                float angle = Random.Range(Mathf.PI * 0.15f, Mathf.PI * 0.85f); // upward fan
                float v = Random.Range(1.5f, 3.2f);
                _confettiPs.Emit(new ParticleSystem.EmitParams
                {
                    position = new Vector3(hole.x, hole.y, -0.4f),
                    velocity = new Vector3(Mathf.Cos(angle) * v, Mathf.Sin(angle) * v, 0f),
                    startColor = ConfettiColors[i % ConfettiColors.Length],
                    startSize = Random.Range(0.06f, 0.13f),
                    startLifetime = Random.Range(0.8f, 1.4f),
                }, 1);
            }
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
