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
        public AudioClip? sandClip;
        public AudioClip? iceClip;
        public AudioClip? readyClip;
        public AudioClip? starClip;
        public AudioClip? rimClip;
        public AudioClip? jingleClip;
        public AudioClip? rampClip;
        public AudioClip? gateClip;
        public AudioClip? millClip;

        [Header("Tuning")]
        [Range(0f, 1f)] public float volume = 0.9f;
        [Tooltip("Minimum seconds between bounce sounds (grazing-contact spam guard).")]
        public float bounceSoundCooldown = 0.06f;

        private SimRunner _runner = null!;
        private BallView _ballView = null!;
        private AudioSource _source = null!;
        private StatsStore? _settings;
        private PerfProbe? _probe;
        private readonly NearMissWatch _nearMiss = new NearMissWatch();

        /// <summary>
        /// Courses already celebrated this session. The first finish of a
        /// course earns the full show; the thirty-fourth retry earns the sound
        /// and a ring, because the loop is built on retries and a nine-tenths
        /// second zoom taxes every one of them. Session-scoped on purpose: a
        /// player returning tomorrow to yesterday's hole has earned the show
        /// again.
        /// </summary>
        private readonly System.Collections.Generic.HashSet<ulong> _celebratedSeeds =
            new System.Collections.Generic.HashSet<ulong>();

        private int _lastWallHits;
        private int _lastBumperHits;
        private int _lastWaterEntries;
        private int _lastGateHits;
        private int _lastMillHits;
        private int _lastPortalTransits;
        private bool _lastHoled;
        private bool _lastFailed;
        private float _lastBounceSoundTime;

        private ParticleSystem _burstPs = null!;
        private ParticleSystem _confettiPs = null!;
        private bool _wasInSand;
        private Vector2 _lastBallPos;

        private CameraJuice? _cameraJuice;
        private ShotLog? _shotLog;
        private LineRenderer _putterFace = null!;
        private Coroutine? _swingRoutine;
        private Coroutine? _slowMoRoutine;
        private GameObject? _slowMoBall;
        private GameObject? _letterbox;

        private bool _wasInIce;
        private bool _wasInRamp;
        private bool _wasReady;
        private Coroutine? _failWashRoutine;
        private GameObject? _failWashGo;
        private Mesh? _failWashMesh;
        private Coroutine? _starRoutine;
        private float _iceSparkleTimer;
        private float _sandDustTimer;

        private static readonly Color SandPuff = new Color(0.85f, 0.78f, 0.55f);
        private static readonly Color WaterSplash = new Color(0.42f, 0.62f, 0.88f);
        private static readonly Color RampRush = new Color(0.96f, 0.94f, 0.84f, 0.9f);
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
            if (sandClip == null) { sandClip = Resources.Load<AudioClip>("Sfx/sand"); }
            if (iceClip == null) { iceClip = Resources.Load<AudioClip>("Sfx/ice"); }
            if (readyClip == null) { readyClip = Resources.Load<AudioClip>("Sfx/ready"); }
            if (starClip == null) { starClip = Resources.Load<AudioClip>("Sfx/star"); }
            if (rimClip == null) { rimClip = Resources.Load<AudioClip>("Sfx/rim"); }
            if (jingleClip == null) { jingleClip = Resources.Load<AudioClip>("Sfx/jingle"); }
            if (rampClip == null) { rampClip = Resources.Load<AudioClip>("Sfx/ramp"); }
            if (gateClip == null) { gateClip = Resources.Load<AudioClip>("Sfx/gate"); }
            if (millClip == null) { millClip = Resources.Load<AudioClip>("Sfx/mill"); }
        }

        /// <summary>Plays the achievement arpeggio (wired by the bootstrap).</summary>
        public void PlayJingle() => Play(jingleClip, 0.9f);

        /// <summary>The settings source (menu toggles); null means everything on.</summary>
        public void SetSettings(StatsStore settings) => _settings = settings;

        /// <summary>Wires the frame-time probe (see <see cref="PerfProbe"/>).</summary>
        public void SetPerfProbe(PerfProbe probe) => _probe = probe;

        /// <summary>Whether a given effect may play for this player.</summary>
        private bool Allows(MotionEffect effect)
            => MotionSettings.Allows(effect, _settings != null && _settings.Data.reducedMotion);

        /// <summary>Camera effect target (shake, celebration zoom).</summary>
        public void SetCameraJuice(CameraJuice juice) => _cameraJuice = juice;

        /// <summary>
        /// The run's scorecard. This class already watches every event the
        /// card wants — it is the thing that turns them into sound — so it
        /// writes the marks rather than a second observer polling the same
        /// zones a second time.
        /// </summary>
        public void SetShotLog(ShotLog log) => _shotLog = log;

        /// <summary>Wires dependencies (called by the bootstrap).</summary>
        public void Initialize(SimRunner runner, BallView ballView)
        {
            _runner = runner;
            _ballView = ballView;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
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
            _lastGateHits = sim.GateHitCount;
            _lastMillHits = sim.WindmillHitCount;
            _lastPortalTransits = sim.PortalTransitCount;
            _lastHoled = sim.IsHoled;
            _lastFailed = sim.IsFailed;
            _shotLog?.Reset();
            _wasInSand = false;
            _wasInIce = false;
            _wasInRamp = false;
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

            if (_letterbox != null)
            {
                Destroy(_letterbox);
                _letterbox = null;
            }

            if (_failWashRoutine != null)
            {
                StopCoroutine(_failWashRoutine);
                ClearFailWash();
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

        /// <summary>Read-only ramp lookup — ramps carry their polygon inside.</summary>
        private bool BallIsOnRamp(PuttSeed.Core.Sim.RampZone[]? ramps)
        {
            var sim = _runner.Sim;
            if (ramps == null || sim == null)
            {
                return false;
            }

            for (int i = 0; i < ramps.Length; i++)
            {
                if (ramps[i].Area.Contains(sim.Ball.Position))
                {
                    return true;
                }
            }

            return false;
        }

        private void Update()
        {
            if (_runner == null)
            {
                return;
            }

            var sim = _runner.Sim;

            // Sparse sparkles trail the ball while it glides across ice.
            if (_wasInIce && sim != null && !sim.IsAtRest)
            {
                _iceSparkleTimer -= Time.deltaTime;
                if (_iceSparkleTimer <= 0f)
                {
                    _iceSparkleTimer = 0.07f;
                    EmitBurst(_burstPs, FixView.ToVector2(sim.Ball.Position),
                        new Color(0.85f, 0.96f, 1f, 0.8f), count: 1, speed: 0.2f, life: 0.5f);
                }
            }

            // Sand keeps kicking up while the ball grinds through it.
            if (_wasInSand && sim != null && !sim.IsAtRest)
            {
                _sandDustTimer -= Time.deltaTime;
                if (_sandDustTimer <= 0f)
                {
                    _sandDustTimer = 0.09f;
                    EmitBurst(_burstPs, FixView.ToVector2(sim.Ball.Position),
                        SandPuff, count: 1, speed: 0.35f, life: 0.3f);
                }
            }
        }

        private void OnShotFired()
        {
            _shotLog?.BeginShot();
            PlayShot();
            Tick(); // the stroke itself gets the lightest touch
            if (_swingRoutine != null)
            {
                StopCoroutine(_swingRoutine);
            }

            _swingRoutine = StartCoroutine(PutterSwing());

            // The heel mark: a quick ring where the stroke BEGAN. The ball
            // leaves immediately; this pins the origin for one readable beat,
            // which is what makes a bank shot's geometry legible afterwards.
            if (_runner.Sim != null)
            {
                StartCoroutine(CelebrationRing(FixView.ToVector2(_runner.Sim.Ball.Position),
                    start: 0.11f, growth: 0.26f, duration: 0.3f, width: 0.03f,
                    tint: new Color(0.97f, 0.96f, 0.90f)));
            }
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

            WatchForNearMiss(sim);

            if (sim.WallHitCount > _lastWallHits)
            {
                _shotLog?.Record(ShotLog.Mark.Wall);
                OnBounce(wallClip);
                // A few sparks at the contact point.
                EmitBurst(_burstPs, FixView.ToVector2(sim.Ball.Position),
                    new Color(1f, 1f, 1f, 0.85f), count: 4, speed: 1.3f, life: 0.22f);
            }

            if (sim.BumperHitCount > _lastBumperHits)
            {
                _shotLog?.Record(ShotLog.Mark.Bumper);
                OnBounce(bumperClip);
                Tap();
                if (Allows(MotionEffect.Shake))
                {
                    _cameraJuice?.Shake(0.05f, 0.18f);
                }
                FlashNearestBumper(FixView.ToVector2(sim.Ball.Position));
            }

            // Gate blocks and windmill slaps are wall-family hits, but each
            // gets its own voice — a gate that sounds like a wall teaches the
            // player nothing about what just refused them.
            if (sim.GateHitCount > _lastGateHits)
            {
                _shotLog?.Record(ShotLog.Mark.Gate);
                OnBounce(gateClip);
                EmitBurst(_burstPs, FixView.ToVector2(sim.Ball.Position),
                    new Color(0.99f, 0.80f, 0.38f, 0.9f), count: 5, speed: 1.3f, life: 0.25f);
            }

            if (sim.WindmillHitCount > _lastMillHits)
            {
                _shotLog?.Record(ShotLog.Mark.Windmill);
                OnBounce(millClip);
                Tap();
                if (Allows(MotionEffect.Shake))
                {
                    _cameraJuice?.Shake(0.04f, 0.15f);
                }
                EmitBurst(_burstPs, FixView.ToVector2(sim.Ball.Position),
                    new Color(1f, 1f, 1f, 0.85f), count: 5, speed: 1.4f, life: 0.22f);
            }

            if (sim.PortalTransitCount > _lastPortalTransits)
            {
                _shotLog?.Record(ShotLog.Mark.Portal);
                // Two violet puffs: where the ball vanished and where it is now.
                Play(readyClip, 0.9f);
                EmitBurst(_burstPs, _lastBallPos, new Color(0.62f, 0.40f, 0.92f, 0.9f),
                    count: 10, speed: 1.2f, life: 0.35f);
                EmitBurst(_burstPs, FixView.ToVector2(sim.Ball.Position),
                    new Color(0.62f, 0.40f, 0.92f, 0.9f), count: 10, speed: 1.2f, life: 0.35f);
                // And a quick violet ring where the ball REAPPEARED — the puffs
                // say something happened, the ring says where to look now.
                StartCoroutine(CelebrationRing(FixView.ToVector2(sim.Ball.Position),
                    start: 0.08f, growth: 0.4f, duration: 0.3f, width: 0.035f,
                    tint: PaletteMaterials.Portal));
            }

            if (sim.WaterEntryCount > _lastWaterEntries)
            {
                _shotLog?.Record(ShotLog.Mark.Water);
                Play(waterClip, 1f);
                Tap();
                // The sim already snapped the ball back to its last rest, so
                // splash where it was LAST frame — right at the water's edge.
                EmitBurst(_burstPs, _lastBallPos, WaterSplash, count: 14, speed: 1.6f, life: 0.5f);
                StartCoroutine(WaterSink(_lastBallPos));
                _ballView.PopIn(); // the real ball pops back in at the drop
            }

            var course = _runner.Generation?.Course;
            bool inSand = BallIsIn(course?.SandZones);
            if (inSand && !_wasInSand)
            {
                _shotLog?.Record(ShotLog.Mark.Sand);
                EmitBurst(_burstPs, FixView.ToVector2(sim.Ball.Position), SandPuff, count: 8, speed: 0.8f, life: 0.4f);
                Play(sandClip, 0.9f);
            }

            _wasInSand = inSand;

            bool inIce = BallIsIn(course?.IceZones);
            if (inIce && !_wasInIce)
            {
                _shotLog?.Record(ShotLog.Mark.Ice);
                Play(iceClip, 0.8f);
            }

            _wasInIce = inIce;

            // The ramp was the one element a ball could cross in total
            // silence: no sound, no particle, no haptic. It gets all three.
            bool onRamp = BallIsOnRamp(course?.Ramps);
            if (onRamp && !_wasInRamp)
            {
                _shotLog?.Record(ShotLog.Mark.Ramp);
                Play(rampClip, 0.85f);
                Tick();
                EmitBurst(_burstPs, FixView.ToVector2(sim.Ball.Position),
                    RampRush, count: 7, speed: 1f, life: 0.3f);
            }

            _wasInRamp = onRamp;

            if (sim.IsHoled && !_lastHoled)
            {
                _shotLog?.Record(ShotLog.Mark.Holed);
                _ballView.Sink();
                // The heaviest moment in the game starts here: zoom, replay,
                // letterbox, confetti and stars at once. Measure it.
                _probe?.WatchCelebration();
                Play(captureClip, 1f);
                Tap(strong: true);
                var hole = FixView.ToVector2(_runner.Generation!.Course.HolePosition);
                bool firstFinishHere = _celebratedSeeds.Add(_runner.Seed);
                StartCoroutine(firstFinishHere
                    ? CelebrationRing(hole)
                    : CelebrationRing(hole, start: 0.18f, growth: 0.8f, duration: 0.45f, width: 0.05f));
                StartCoroutine(CaptureFlash());
                if (firstFinishHere)
                {
                    _cameraJuice?.CelebrateZoom(hole);
                }
                else if (Allows(MotionEffect.CameraPush))
                {
                    _cameraJuice?.Tighten(hole);
                }

                if (firstFinishHere
                    && PuttSeed.Core.Sim.Scoring.Stars(sim.Strokes, _runner.Generation.Course.Par) == 3
                    && Allows(MotionEffect.Confetti))
                {
                    EmitConfetti(hole);
                }

                if (_slowMoRoutine != null)
                {
                    StopCoroutine(_slowMoRoutine);
                }

                // The letterbox is built inside the replay, so refusing the
                // replay refuses both — and a repeat finish refuses the whole
                // parade, keeping only the sound, the flash and a ring.
                if (firstFinishHere && Allows(MotionEffect.SlowMo))
                {
                    _slowMoRoutine = StartCoroutine(SlowMoWinningPutt(sim.Strokes));
                }

                if (_starRoutine != null)
                {
                    StopCoroutine(_starRoutine);
                }

                _starRoutine = StartCoroutine(StarNotes(
                    PuttSeed.Core.Sim.Scoring.Stars(sim.Strokes, _runner.Generation.Course.Par)));
            }

            if (sim.IsFailed && !_lastFailed)
            {
                // Running out of strokes used to be one quiet clip against a
                // hole-out that gets a ring, a flash, a zoom and confetti.
                // It now lands: a tone lower, a thump in the hand, the lights
                // dropping over the green.
                PlayPitched(failClip, 0.85f, 0.88f);
                Tap(strong: true);
                if (_failWashRoutine != null)
                {
                    StopCoroutine(_failWashRoutine);
                    ClearFailWash();
                }

                _failWashRoutine = StartCoroutine(FailWash());
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
            _lastGateHits = sim.GateHitCount;
            _lastMillHits = sim.WindmillHitCount;
            _lastPortalTransits = sim.PortalTransitCount;
            _lastHoled = sim.IsHoled;
            _lastFailed = sim.IsFailed;
            _lastBallPos = FixView.ToVector2(sim.Ball.Position);
        }

        /// <summary>A white pulse on the bumper the ball just punched.</summary>
        private void FlashNearestBumper(Vector2 ballPos)
        {
            var course = _runner.Generation?.Course;
            if (course == null || course.Bumpers.Length == 0)
            {
                return;
            }

            var nearest = FixView.ToVector2(course.Bumpers[0].Center);
            float radius = FixView.ToFloat(course.Bumpers[0].Radius);
            float bestSq = (nearest - ballPos).sqrMagnitude;
            for (int i = 1; i < course.Bumpers.Length; i++)
            {
                var center = FixView.ToVector2(course.Bumpers[i].Center);
                float dSq = (center - ballPos).sqrMagnitude;
                if (dSq < bestSq)
                {
                    bestSq = dSq;
                    nearest = center;
                    radius = FixView.ToFloat(course.Bumpers[i].Radius);
                }
            }

            StartCoroutine(BumperFlash(nearest, radius));
        }

        private IEnumerator BumperFlash(Vector2 center, float radius)
        {
            var mesh = MeshFactory.Disc(Vector2.zero, radius, Color.white);
            var go = new GameObject("BumperFlash");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = PaletteMaterials.Shared;
            go.transform.position = new Vector3(center.x, center.y, -0.042f);

            // Alpha animates through a property block — one mesh, no churn.
            var block = new MaterialPropertyBlock();
            const float duration = 0.2f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                go.transform.localScale = Vector3.one * (1f + 0.35f * k);
                block.SetColor("_Color", new Color(1f, 1f, 1f, 0.65f * (1f - k)));
                renderer.SetPropertyBlock(block);
                yield return null;
            }

            Destroy(mesh);
            Destroy(go);
        }

        /// <summary>The entry-point ball sinking into the water (the real ball
        /// has already been reset by the sim; this sells the dunk).</summary>
        private IEnumerator WaterSink(Vector2 entry)
        {
            var mesh = MeshFactory.Disc(Vector2.zero, 0.1f, PaletteMaterials.Ball);
            var go = new GameObject("WaterSink");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = PaletteMaterials.Shared;
            go.transform.position = new Vector3(entry.x, entry.y, -0.058f);

            const float duration = 0.3f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                go.transform.localScale = Vector3.one * (1f - t / duration);
                yield return null;
            }

            Destroy(mesh);
            Destroy(go);
        }

        /// <summary>A 0.12 s white pulse over the whole view on capture.</summary>
        private IEnumerator CaptureFlash()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                yield break;
            }

            var mesh = MeshFactory.Quad(new Vector2(-60f, -60f), new Vector2(60f, 60f), Color.white);
            var go = new GameObject("CaptureFlash");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = PaletteMaterials.Shared;

            var block = new MaterialPropertyBlock();
            const float duration = 0.12f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                var c = cam.transform.position;
                go.transform.position = new Vector3(c.x, c.y, -0.9f);
                block.SetColor("_Color", new Color(1f, 1f, 1f, 0.28f * (1f - t / duration)));
                renderer.SetPropertyBlock(block);
                yield return null;
            }

            Destroy(mesh);
            Destroy(go);
        }

        /// <summary>
        /// The lights going down over the green when the strokes run out: a
        /// slow dark wash, deliberately the mirror of the capture flash. A
        /// retry cancels it — see <see cref="SyncCounters"/>.
        /// </summary>
        private IEnumerator FailWash()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                yield break;
            }

            _failWashMesh = MeshFactory.Quad(new Vector2(-60f, -60f), new Vector2(60f, 60f), Color.white);
            _failWashGo = new GameObject("FailWash");
            _failWashGo.AddComponent<MeshFilter>().sharedMesh = _failWashMesh;
            var renderer = _failWashGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = PaletteMaterials.Shared;

            var block = new MaterialPropertyBlock();
            const float fadeIn = 0.14f;
            const float hold = 0.12f;
            const float fadeOut = 0.3f;
            const float peak = 0.34f;
            for (float t = 0f; t < fadeIn + hold + fadeOut; t += Time.deltaTime)
            {
                float alpha = t < fadeIn ? peak * (t / fadeIn)
                    : t < fadeIn + hold ? peak
                    : peak * (1f - (t - fadeIn - hold) / fadeOut);
                var c = cam.transform.position;
                _failWashGo.transform.position = new Vector3(c.x, c.y, -0.9f);
                block.SetColor("_Color", new Color(0.02f, 0.05f, 0.04f, alpha));
                renderer.SetPropertyBlock(block);
                yield return null;
            }

            ClearFailWash();
        }

        /// <summary>Destroys the wash quad and its mesh, whenever it ends.</summary>
        private void ClearFailWash()
        {
            if (_failWashGo != null)
            {
                Destroy(_failWashGo);
                _failWashGo = null;
            }

            if (_failWashMesh != null)
            {
                Destroy(_failWashMesh);
                _failWashMesh = null;
            }

            _failWashRoutine = null;
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

                Tick(); // each star lands in the hand too
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
                    rotation = Random.Range(0f, 360f),
                    angularVelocity = Random.Range(-260f, 260f), // tumbling squares
                }, 1);
            }
        }

        private void OnBounce(AudioClip? clip)
        {
            var velocity = _runner.Sim != null
                ? FixView.ToVector2(_runner.Sim.Ball.Velocity)
                : Vector2.right;
            _ballView.Squash(velocity);
            if (Time.unscaledTime - _lastBounceSoundTime >= bounceSoundCooldown)
            {
                _lastBounceSoundTime = Time.unscaledTime;

                // A hard hit sounds louder and deeper than a graze.
                var sim = _runner.Sim;
                float speed = sim != null ? FixView.ToVector2(sim.Ball.Velocity).magnitude : 3f;
                float k = Mathf.Clamp01(speed / 6f);
                if (clip != null && (_settings == null || _settings.Data.soundEnabled))
                {
                    _source.pitch = Mathf.Lerp(1.07f, 0.9f, k) * Random.Range(0.98f, 1.02f);
                    _source.PlayOneShot(clip, volume * Mathf.Lerp(0.35f, 1f, k));
                }
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

        /// <summary>Plays a clip around a deliberate pitch centre.</summary>
        private void PlayPitched(AudioClip? clip, float gain, float pitch)
        {
            if (clip != null && (_settings == null || _settings.Data.soundEnabled))
            {
                _source.pitch = pitch * Random.Range(0.98f, 1.02f);
                _source.PlayOneShot(clip, volume * gain);
            }
        }

        /// <summary>
        /// The putter voice rides the shot: a 10% tap is high and light, a
        /// full swing low and loud. One clip fired at a fixed pitch made every
        /// stroke in the game sound like the same stroke.
        /// </summary>
        private void PlayShot()
        {
            float power = Mathf.Clamp01(_runner.LastShot.PowerIndex / 255f);
            PlayPitched(shotClip, Mathf.Lerp(0.5f, 1f, power), Mathf.Lerp(1.16f, 0.88f, power));
        }

        private void Tick()
        {
            if (_settings == null || _settings.Data.hapticsEnabled)
            {
                HapticsPlayer.Tick();
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

            // Cinematic letterbox: two dark bars slide in for the replay.
            var cam = Camera.main;
            Transform? topBar = null, bottomBar = null;
            Mesh? barMesh = null;
            if (cam != null)
            {
                barMesh = MeshFactory.Quad(new Vector2(-50f, -0.5f), new Vector2(50f, 0.5f),
                    new Color(0.02f, 0.04f, 0.03f, 0.85f));

                // Under the camera, not in the world: cinema bars belong to the
                // top and bottom of the SCREEN, and the camera rolls 90° on
                // wide courses (see CameraFramer).
                _letterbox = new GameObject("Letterbox");
                _letterbox.transform.SetParent(cam.transform, false);
                topBar = CreateBar(barMesh, "Top");
                bottomBar = CreateBar(barMesh, "Bottom");
            }

            float slide = 0f;
            void PlaceBars()
            {
                if (cam == null || topBar == null || bottomBar == null)
                {
                    return;
                }

                float h = cam.orthographicSize;
                float barH = h * 0.16f;
                topBar.localScale = new Vector3(1f, barH, 1f);
                bottomBar.localScale = new Vector3(1f, barH, 1f);
                float inset = barH * (slide - 0.5f); // -0.5: fully off, +0.5: flush
                // Camera-local: z 9.13 sits where world -0.87 used to, just
                // behind the vignette.
                topBar.localPosition = new Vector3(0f, h - inset, 9.13f);
                bottomBar.localPosition = new Vector3(0f, -h + inset, 9.13f);
            }

            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                slide = Mathf.SmoothStep(0f, 1f, t / 0.2f);
                PlaceBars();
                yield return null;
            }

            slide = 1f;

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
                PlaceBars(); // follow any camera zoom
                yield return null;
            }

            // Sink: shrink into the cup.
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                _slowMoBall.transform.localScale = Vector3.one * (1f - t / 0.25f);
                PlaceBars();
                yield return null;
            }

            Destroy(_slowMoBall);
            _slowMoBall = null;

            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                slide = Mathf.SmoothStep(1f, 0f, t / 0.2f);
                PlaceBars();
                yield return null;
            }

            if (barMesh != null)
            {
                Destroy(barMesh);
            }

            if (_letterbox != null)
            {
                Destroy(_letterbox);
                _letterbox = null;
            }

            _slowMoRoutine = null;
        }

        private Transform CreateBar(Mesh barMesh, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_letterbox!.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = barMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = PaletteMaterials.Shared;
            return go.transform;
        }

        /// <summary>Flat expanding ring at the hole, fading out over ~0.8 s.</summary>
        /// <summary>
        /// The shot that almost dropped. Golf's best beat went unremarked here:
        /// a ball that grazed the cup sounded exactly like one that missed by a
        /// metre, and on Easy and Normal — where any touch captures — a rim-out
        /// cannot even happen, so this is the only place a near miss can exist.
        ///
        /// Reads the sim and changes nothing in it.
        /// </summary>
        private void WatchForNearMiss(PuttSeed.Core.Sim.GolfSim sim)
        {
            var generation = _runner.Generation;
            if (generation == null)
            {
                return;
            }

            var cup = FixView.ToVector2(generation.Course.HolePosition);
            var ball = FixView.ToVector2(sim.Ball.Position);
            float cupRadius = FixView.ToFloat(_runner.PlayConfig.HoleRadius);
            if (!_nearMiss.Observe(Vector2.Distance(ball, cup), cupRadius, sim.IsHoled, !sim.IsAtRest))
            {
                return;
            }

            Play(rimClip, 0.75f);
            Tap();
            StartCoroutine(CelebrationRing(cup, start: cupRadius * 1.2f, growth: 0.5f,
                duration: 0.34f, width: 0.035f));
            if (Allows(MotionEffect.CameraPush))
            {
                _cameraJuice?.Tighten(cup);
            }
        }

        /// <summary>
        /// An expanding ring at a point. The capture keeps the big slow one it
        /// always had; the near miss borrows it small and quick.
        /// </summary>
        private IEnumerator CelebrationRing(Vector2 center, float start = 0.2f, float growth = 1.6f,
            float duration = 0.8f, float width = 0.06f, Color? tint = null)
        {
            var ringColor = tint ?? Color.white;
            const int segments = 48;
            var go = new GameObject("CelebrationRing");
            var line = go.AddComponent<LineRenderer>();
            line.loop = true;
            line.positionCount = segments;
            line.widthMultiplier = width;
            line.material = PaletteMaterials.Shared;
            line.sortingOrder = 20;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float radius = start + k * growth;
                var color = new Color(ringColor.r, ringColor.g, ringColor.b, 1f - k);
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
        /// <summary>The lightest touch (shot release, star count).</summary>
        public static void Tick() => Vibrate(12, 70);

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
