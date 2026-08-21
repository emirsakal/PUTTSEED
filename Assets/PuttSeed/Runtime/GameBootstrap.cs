#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Single scene entry point: builds the whole runtime object graph in code
    /// (camera, sim runner, course view, ball, ghosts, input, UI) and loads
    /// today's daily course — or a fixed seed for testing. Reading the clock
    /// happens HERE, outside core, and only the derived date ints cross the
    /// boundary.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Feel asset; when empty, loads FeelConfig from Resources.")]
        public FeelConfig? feel;

        [Tooltip("Play this seed instead of today's daily course.")]
        public bool useFixedSeed;
        public ulong fixedSeed = 1;

        [Header("Scene-authored UI (assigned by PuttSeed → Rebuild Scenes)")]
        public GameUI? gameUi;
        public LoadingOverlay? loadingOverlay;

        private void Start()
        {
            if (feel == null)
            {
                feel = Resources.Load<FeelConfig>("FeelConfig");
            }

            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }

            var cameraJuice = cam.gameObject.AddComponent<CameraJuice>();
            new GameObject("Vignette").AddComponent<VignetteView>().Initialize(cam);

            // The loading cover is scene-authored; resolve it early so the
            // course/flag intro reveals can wait for it to lift.
            var overlay = loadingOverlay != null ? loadingOverlay : FindFirstObjectByType<LoadingOverlay>();

            var runnerGo = new GameObject("SimRunner");
            var runner = runnerGo.AddComponent<SimRunner>();
            runner.feel = feel;

            var courseGo = new GameObject("CourseView");
            var courseRenderer = courseGo.AddComponent<CourseRenderer>();
            courseRenderer.overlay = overlay;
            courseRenderer.runner = runner; // windmill views mirror the sim phase

            var flagGo = new GameObject("Flag");
            var flagView = flagGo.AddComponent<FlagView>();
            flagView.overlay = overlay;
            flagView.Initialize(runner);

            // One store instance serves gameplay stats and the settings toggles.
            var stats = new StatsStore(MenuBootstrap.StatsPath());
            UiSounds.Enabled = stats.Data.soundEnabled;
            PaletteMaterials.ColorblindMode = stats.Data.colorblindMode;

            var ballGo = new GameObject("Ball");
            var ballView = ballGo.AddComponent<BallView>();
            ballView.Initialize(runner,
                BallSkins.Resolve(stats.Data.ballSkin).Color,
                BallTrails.Resolve(stats.Data.ballTrail).Color);

            // One scorecard per session: the feedback watcher writes it as it
            // turns events into sound, the share text reads it.
            var shotLog = new ShotLog();

            // The measuring stick. Present in every build: it costs a float
            // compare per frame and a log line per hole-out, and a number that
            // only exists in a special build is a number nobody has.
            var probe = new GameObject("PerfProbe").AddComponent<PerfProbe>();

            var feedbackGo = new GameObject("Feedback");
            var feedback = feedbackGo.AddComponent<FeedbackController>();
            feedback.LoadDefaultClips();
            feedback.SetSettings(stats);
            feedback.SetCameraJuice(cameraJuice);
            feedback.SetShotLog(shotLog);
            feedback.SetPerfProbe(probe);
            feedback.Initialize(runner, ballView);

            var ghostsGo = new GameObject("Ghosts");
            ghostsGo.AddComponent<GhostViewManager>().Initialize(runner);

            var inputGo = new GameObject("DragInput");
            var dragInput = inputGo.AddComponent<DragAimController>();
            dragInput.Initialize(runner, cam);

            var readyGo = new GameObject("ReadyIndicator");
            readyGo.AddComponent<ReadyIndicator>().Initialize(runner, dragInput);

            // The HUD is scene-authored; bind, don't build.
            var ui = gameUi != null ? gameUi : FindFirstObjectByType<GameUI>();
            if (ui == null)
            {
                Debug.LogError("PuttSeed: no scene-authored GameUI found — run PuttSeed → Rebuild Scenes.");
                return;
            }

            var devGo = new GameObject("DevReload");
            var devReload = devGo.AddComponent<DevReloadController>();

            var modesGo = new GameObject("Modes");
            var modes = modesGo.AddComponent<ModeController>();
            modes.Initialize(runner, courseRenderer, cam, overlay, stats);
            modes.SetShotLog(shotLog);
            modes.SetPerfProbe(probe);

            ui.Initialize(runner, modes);
            devReload.Initialize(runner, courseRenderer, cam);
            dragInput.previewAllowed = () => modes.AimPreviewAllowed;
            dragInput.aimDirect = () => stats.Data.aimDirect;
            modes.AchievementUnlocked += _ => feedback.PlayJingle();

            // The menu wrote the session; the inspector override wins in the
            // editor when the Game scene is played directly with a fixed seed.
            if (useFixedSeed)
            {
                GameSession.UseFixedSeed = true;
                GameSession.FixedSeed = fixedSeed;
            }

            UiPolish.EnsureButtonFeedback();
            modes.StartFromSession();

            Application.targetFrameRate = stats.Data.batterySaver ? 60 : 120;
        }
    }
}
