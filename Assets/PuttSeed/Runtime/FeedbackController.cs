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
        public AudioClip? rollClip;
        public AudioClip? sandClip;
        public AudioClip? iceClip;
        public AudioClip? readyClip;
        public AudioClip? starClip;
        public AudioClip? jingleClip;

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

        private CameraJuice? _cameraJuice;
        private LineRenderer _putterFace = null!;
        private Coroutine? _swingRoutine;
        private Coroutine? _slowMoRoutine;
        private GameObject? _slowMoBall;

        private AudioSource _rollSource = null!;
        private float _rollLevel;
        private bool _wasInIce;
        private bool _wasReady;
        private Coroutine? _starRoutine;

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
            if (rollClip == null) { rollClip = Resources.Load<AudioClip>("Sfx/roll"); }
            if (sandClip == null) { sandClip = Resources.Load<AudioClip>("Sfx/sand"); }
            if (iceClip == null) { iceClip = Resources.Load<AudioClip>("Sfx/ice"); }
            if (readyClip == null) { readyClip = Resources.Load<AudioClip>("Sfx/ready"); }
            if (starClip == null) { starClip = Resources.Load<AudioClip>("Sfx/star"); }
            if (jingleClip == null) { jingleClip = Resources.Load<AudioClip>("Sfx/jingle"); }
        }

        /// <summary>Plays the achievement arpeggio (wired by the bootstrap).</summary>
        public void PlayJingle() => Play(jingleClip, 0.9f);

        /// <summary>The settings source (menu toggles); null means everything on.</summary>
        public void SetSettings(StatsStore settings) => _settings = settings;

        /// <summary>Camera effect target (shake, celebration zoom).</summary>
        public void SetCameraJuice(CameraJuice juice) => _cameraJuice = juice;

        /// <summary>Wires dependencies (called by the bootstrap).</summary>
        public void Initialize(SimRunner runner, BallView ballView)
        {
            _runner = runner;
            _ballView = ballView;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            // A dedicated looping source hums while the ball rolls; volume and
            // pitch follow ball speed in Update.
            _rollSource = gameObject.AddComponent<AudioSource>();
            _rollSource.playOnAwake = false;
            _rollSource.loop = true;
            _rollSource.clip = rollClip;
            _burstPs = CreateParticleSystem("Bursts", gravity: 0f);
            _confettiPs = CreateParticleSystem("Confetti", gravity: 0.7f);

            // The putter face: a short flat bar that slides into the ball on
            // every accepted shot (the swing read, no physics involvement).
            var putterGo = new GameObject("PutterFace");
            putterGo.transform.SetParent(transform, false);
            _putterFace = putterGo.AddComponent<LineRenderer>();
            _putterFace.positionCount = 2;
            _putterFace.startWidth = 0.09f;
            _putterFace.endWidth = 0.09f;
            _putterFace.material = PaletteMaterials.Shared;
            _putterFace.sortingOrder = 15;
            _putterFace.enabled = false;

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
            _wasInIce = false;
            _wasReady = true; // no settle-pluck for the freshly placed ball
            _lastBallPos = FixView.ToVector2(sim.Ball.Position);

            if (_starRoutine != null)
            {
                StopCoroutine(_starRoutine);
                _starRoutine = null;
            }

            // A reset interrupts any in-flight presentation.
            _cameraJuice?.CancelEffects();
            if (_swingRoutine != null)
            {
                StopCoroutine(_swingRoutine);
                _swingRoutine = null;
            }

            if (_putterFace != null)
            {
                _putterFace.enabled = false;
            }

            if (_slowMoRoutine != null)
            {
                StopCoroutine(_slowMoRoutine);
                _slowMoRoutine = null;
            }

            if (_slowMoBall != null)
            {
                Destroy(_slowMoBall);
                _slowMoBall = null;
            }
        }

        /// <summary>Read-only zone lookup for presentation (core's own test).</summary>
        private bool BallIsIn(PuttSeed.Core.Sim.ZonePolygon[]? zones)
        {
            var sim = _runner.Sim;
            if (zones == null || sim == null)
            {
                return false;
            }

            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i].Contains(sim.Ball.Position))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The rolling loop: speed-driven volume and pitch.</summary>
        private void Update()
        {
            if (_runner == null || _rollSource == null)
            {
                return;
            }

            var sim = _runner.Sim;
            bool soundOn = _settings == null || _settings.Data.soundEnabled;
            float target = 0f;
            if (soundOn && sim != null && !sim.IsAtRest && !sim.IsHoled)
            {
                float speed = FixView.ToVector2(sim.Ball.Velocity).magnitude;
                target = Mathf.Clamp01(speed / 6f);
            }

            _rollLevel = Mathf.MoveTowards(_rollLevel, target, Time.deltaTime * 5f);
            _rollSource.volume = _rollLevel * 0.45f * volume;
            _rollSource.pitch = 0.75f + _rollLevel * 0.55f;
            if (_rollLevel > 0.01f && !_rollSource.isPlaying && _rollSource.clip != null)
            {
                _rollSource.Play();
            }
            else if (_rollLevel <= 0.01f && _rollSource.isPlaying)
            {
                _rollSource.Pause();
            }
        }

        private void OnShotFired()
        {
            Play(shotClip, 1f);
            if (_swingRoutine != null)
            {
                StopCoroutine(_swingRoutine);
            }

            _swingRoutine = StartCoroutine(PutterSwing());
        }

        /// <summary>The putter face slides into the contact point, then fades.</summary>
        private IEnumerator PutterSwing()
        {
            var sim = _runner.Sim;
            if (sim == null)
            {
                yield break;
            }

            float angle = _runner.LastShot.AngleIndex * 2f * Mathf.PI / 1024f;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var perp = new Vector2(-dir.y, dir.x) * 0.16f;
            var origin = FixView.ToVector2(_runner.LastShotOrigin);
            var faceColor = new Color(0.16f, 0.15f, 0.18f);

            _putterFace.enabled = true;
            const float slideTime = 0.09f;
            const float fadeTime = 0.16f;
            for (float t = 0f; t < slideTime + fadeTime; t += Time.deltaTime)
            {
                float slide = Mathf.Clamp01(t / slideTime);
                float alpha = t < slideTime ? 1f : 1f - (t - slideTime) / fadeTime;
                var center = origin - dir * Mathf.Lerp(0.5f, 0.16f, Mathf.SmoothStep(0f, 1f, slide));
                _putterFace.SetPosition(0, new Vector3(center.x - perp.x, center.y - perp.y, -0.055f));
                _putterFace.SetPosition(1, new Vector3(center.x + perp.x, center.y + perp.y, -0.055f));
                var c = new Color(faceColor.r, faceColor.g, faceColor.b, alpha);
                _putterFace.startColor = c;
                _putterFace.endColor = c;
                yield return null;
            }

            _putterFace.enabled = false;
            _swingRoutine = null;
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
                _cameraJuice?.Shake(0.05f, 0.18f);
            }

            if (sim.WaterEntryCount > _lastWaterEntries)
            {
                Play(waterClip, 1f);
                Tap();
                // The sim already snapped the ball back to its last rest, so
                // splash where it was LAST frame — right at the water's edge.
                EmitBurst(_burstPs, _lastBallPos, WaterSplash, count: 14, speed: 1.6f, life: 0.5f);
            }

            var course = _runner.Generation?.Course;
            bool inSand = BallIsIn(course?.SandZones);
            if (inSand && !_wasInSand)
            {
                EmitBurst(_burstPs, FixView.ToVector2(sim.Ball.Position), SandPuff, count: 8, speed: 0.8f, life: 0.4f);
                Play(sandClip, 0.9f);
            }

            _wasInSand = inSand;

            bool inIce = BallIsIn(course?.IceZones);
            if (inIce && !_wasInIce)
            {
                Play(iceClip, 0.8f);
            }

            _wasInIce = inIce;

            if (sim.IsHoled && !_lastHoled)
            {
                Play(captureClip, 1f);
                Tap(strong: true);
                var hole = FixView.ToVector2(_runner.Generation!.Course.HolePosition);
                StartCoroutine(CelebrationRing(hole));
                _cameraJuice?.CelebrateZoom(hole);
                if (PuttSeed.Core.Sim.Scoring.Stars(sim.Strokes, _runner.Generation.Course.Par) == 3)
                {
                    EmitConfetti(hole);
                }

                if (_slowMoRoutine != null)
                {
                    StopCoroutine(_slowMoRoutine);
                }

                _slowMoRoutine = StartCoroutine(SlowMoWinningPutt(sim.Strokes));

                if (_starRoutine != null)
                {
                    StopCoroutine(_starRoutine);
                }

                _starRoutine = StartCoroutine(StarNotes(
                    PuttSeed.Core.Sim.Scoring.Stars(sim.Strokes, _runner.Generation.Course.Par)));
            }

            if (sim.IsFailed && !_lastFailed)
            {
                Play(failClip, 0.8f);
            }

            // The settle moment: shot resolved, aiming open again — pluck.
            bool ready = sim.IsAtRest && !sim.IsHoled && !sim.IsFailed;
            if (ready && !_wasReady)
            {
                Play(readyClip, 0.5f);
            }

            _wasReady = ready;

            _lastWallHits = sim.WallHitCount;
            _lastBumperHits = sim.BumperHitCount;
            _lastWaterEntries = sim.WaterEntryCount;
            _lastHoled = sim.IsHoled;
            _lastFailed = sim.IsFailed;
            _lastBallPos = FixView.ToVector2(sim.Ball.Position);
        }

        /// <summary>A rising note per earned star (major-triad steps).</summary>
        private IEnumerator StarNotes(int stars)
        {
            yield return new WaitForSeconds(0.5f);
            float[] pitches = { 1f, 1.26f, 1.5f };
            for (int i = 0; i < stars && i < pitches.Length; i++)
            {
                if (starClip != null && (_settings == null || _settings.Data.soundEnabled))
                {
                    _source.pitch = pitches[i];
                    _source.PlayOneShot(starClip, volume * 0.7f);
                }

                yield return new WaitForSeconds(0.16f);
            }

            _starRoutine = null;
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

        private void Tap(bool strong = false)
        {
            if (_settings == null || _settings.Data.hapticsEnabled)
            {
                if (strong)
                {
                    HapticsPlayer.Thump();
                }
                else
                {
                    HapticsPlayer.Tap();
                }
            }
        }

        /// <summary>
        /// The winning putt again at 0.35x: a translucent ball re-simulates
        /// the exact final shot on a throwaway sim (RestoreRest + LastShot),
        /// rolls to the cup in slow motion and sinks. Interrupted by resets.
        /// </summary>
        private IEnumerator SlowMoWinningPutt(int finalStrokes)
        {
            yield return new WaitForSeconds(0.7f);
            var gen = _runner.Generation;
            if (gen == null)
            {
                yield break;
            }

            var replay = new PuttSeed.Core.Sim.GolfSim(gen.Course, _runner.PlayConfig);
            replay.RestoreRest(_runner.LastShotOrigin, Mathf.Max(0, finalStrokes - 1));
            replay.Shoot(_runner.LastShot);

            _slowMoBall = new GameObject("SlowMoBall");
            _slowMoBall.AddComponent<MeshFilter>().sharedMesh =
                MeshFactory.Disc(Vector2.zero, 0.1f, new Color(0.97f, 0.97f, 0.95f, 0.6f));
            _slowMoBall.AddComponent<MeshRenderer>().sharedMaterial = PaletteMaterials.Shared;
            var trail = _slowMoBall.AddComponent<TrailRenderer>();
            trail.time = 0.9f;
            trail.startWidth = 0.07f;
            trail.endWidth = 0.01f;
            trail.material = PaletteMaterials.Shared;
            trail.startColor = new Color(1f, 1f, 1f, 0.3f);
            trail.endColor = new Color(1f, 1f, 1f, 0f);

            float ticks = 0f;
            int safety = 2000;
            while (!replay.IsHoled && safety-- > 0)
            {
                ticks += Time.deltaTime * 120f * 0.35f;
                while (ticks >= 1f && !replay.IsHoled)
                {
                    replay.Tick();
                    ticks -= 1f;
                }

                var p = FixView.ToVector2(replay.Ball.Position);
                _slowMoBall.transform.position = new Vector3(p.x, p.y, -0.058f);
                yield return null;
            }

            // Sink: shrink into the cup.
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                _slowMoBall.transform.localScale = Vector3.one * (1f - t / 0.25f);
                yield return null;
            }

            Destroy(_slowMoBall);
            _slowMoBall = null;
            _slowMoRoutine = null;
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

    /// <summary>
    /// Device haptics with amplitude control: VibrationEffect over JNI on
    /// API 26+, legacy vibrate below, Handheld.Vibrate as the last resort.
    /// No-op in the editor and on non-Android platforms.
    /// </summary>
    public static class HapticsPlayer
    {
        /// <summary>A light tick (bumper hit, water entry).</summary>
        public static void Tap() => Vibrate(20, 110);

        /// <summary>A firm thump (hole capture).</summary>
        public static void Thump() => Vibrate(40, 220);

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject? _vibrator;

        private static void Vibrate(long milliseconds, int amplitude)
        {
            try
            {
                if (_vibrator == null)
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    }
                }

                if (_vibrator == null)
                {
                    Handheld.Vibrate();
                    return;
                }

                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    if (version.GetStatic<int>("SDK_INT") >= 26)
                    {
                        using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                        using (var effect = effectClass.CallStatic<AndroidJavaObject>(
                            "createOneShot", milliseconds, amplitude))
                        {
                            _vibrator.Call("vibrate", effect);
                        }
                    }
                    else
                    {
                        _vibrator.Call("vibrate", milliseconds);
                    }
                }
            }
            catch (System.Exception)
            {
                Handheld.Vibrate();
            }
        }
#else
        private static void Vibrate(long milliseconds, int amplitude)
        {
            _ = milliseconds;
            _ = amplitude;
        }
#endif
    }
}
