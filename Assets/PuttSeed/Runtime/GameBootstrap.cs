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

            var runnerGo = new GameObject("SimRunner");
            var runner = runnerGo.AddComponent<SimRunner>();
            runner.feel = feel;

            var courseGo = new GameObject("CourseView");
            var courseRenderer = courseGo.AddComponent<CourseRenderer>();

            var ballGo = new GameObject("Ball");
            var ballView = ballGo.AddComponent<BallView>();
            ballView.Initialize(runner);

            var feedbackGo = new GameObject("Feedback");
            feedbackGo.AddComponent<FeedbackController>().Initialize(runner, ballView);

            var ghostsGo = new GameObject("Ghosts");
            ghostsGo.AddComponent<GhostViewManager>().Initialize(runner);

            var inputGo = new GameObject("DragInput");
            inputGo.AddComponent<DragAimController>().Initialize(runner, cam);

            var uiGo = new GameObject("UI");
            var ui = uiGo.AddComponent<GameUI>();

            var devGo = new GameObject("DevReload");
            var devReload = devGo.AddComponent<DevReloadController>();

            var overlayGo = new GameObject("LoadingOverlay");
            var overlay = overlayGo.AddComponent<LoadingOverlay>();

            var modesGo = new GameObject("Modes");
            var modes = modesGo.AddComponent<ModeController>();
            modes.Initialize(runner, courseRenderer, cam, overlay, MenuBootstrap.StatsPath());

            ui.Initialize(runner, modes);
            devReload.Initialize(runner, courseRenderer, cam);

            // The menu wrote the session; the inspector override wins in the
            // editor when the Game scene is played directly with a fixed seed.
            if (useFixedSeed)
            {
                GameSession.UseFixedSeed = true;
                GameSession.FixedSeed = fixedSeed;
            }

            modes.StartFromSession();

            Application.targetFrameRate = 120;
        }
    }
}
