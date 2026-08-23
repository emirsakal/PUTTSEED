#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PuttSeed.Unity.Editor
{
    /// <summary>
    /// Captures the Game view into docs/media/ at its current resolution —
    /// used to produce the README screenshots at a consistent 1080x1920.
    /// Play mode only: every frame worth showing is a runtime frame.
    ///
    /// F9, because the best frames are the ones a menu cannot reach: the aim
    /// line only exists while a drag is HELD, and clicking a menu item means
    /// letting go of it first. The browser capture harness (tools/webshot.py)
    /// answers to the same key, so the muscle memory carries between them.
    /// </summary>
    public static class ScreenshotTool
    {
        [MenuItem("PuttSeed/Capture Screenshot _F9")]
        public static void Capture()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "PuttSeed: enter Play mode first — screenshots capture the running game.");
                return;
            }

            string dir = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "docs", "media"));
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, $"shot-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            ScreenCapture.CaptureScreenshot(file);
            Debug.Log($"PuttSeed: screenshot -> {file}");
        }

        /// <summary>
        /// Starts or stops the hero recording (see <see cref="HeroRecorder"/>).
        ///
        /// F10 for the same reason Capture is F9: the animation worth having
        /// begins with the aim line drawn, and reaching a menu means letting
        /// go of the drag that draws it.
        ///
        /// The recorder is created here rather than baked into the scene. It
        /// is a tool, not part of the game, and a scene that carries it would
        /// have to be rebuilt to stop carrying it.
        /// </summary>
        [MenuItem("PuttSeed/Record Hero Frames _F10")]
        public static void RecordHero()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "PuttSeed: enter Play mode first — the recording is of the running game.");
                return;
            }

            // Qualified: this file also uses System (for DateTime), so a bare
            // Object is ambiguous between UnityEngine.Object and System.Object.
            var recorder = UnityEngine.Object.FindFirstObjectByType<HeroRecorder>();
            if (recorder == null)
            {
                recorder = new GameObject("HeroRecorder").AddComponent<HeroRecorder>();
            }

            recorder.Toggle();
        }
    }
}
