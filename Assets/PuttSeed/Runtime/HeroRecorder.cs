#if UNITY_EDITOR
#nullable enable
using System.Collections;
using System.IO;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Records the Game view to a numbered PNG sequence, for building the
    /// README's hero animation. Editor-only by compilation: the whole file is
    /// behind UNITY_EDITOR, so nothing here can reach a player build.
    ///
    /// The frames are lossless and evenly spaced, which a screen recorder
    /// cannot promise. The trick is <c>Time.captureFramerate</c>: with it set,
    /// Unity stops asking the clock how much time passed and declares that
    /// exactly 1/fps did, so every frame advances the same amount however long
    /// the encode actually took. The game runs slower than real time while
    /// recording and the recording plays back at normal speed.
    ///
    /// That lands well on this sim in particular. SimRunner feeds
    /// Time.deltaTime to a 120 Hz FixedStepper, so a pinned 1/30 delta is
    /// exactly four ticks per frame, every frame — the recording is not just
    /// smooth, it is the same four ticks a 30 fps device would take.
    ///
    /// Started from PuttSeed → Record Hero Frames (F10), because the frames
    /// worth having start while a drag is HELD and a menu click would end it.
    /// </summary>
    public sealed class HeroRecorder : MonoBehaviour
    {
        /// <summary>Recorded frames per second of game time.</summary>
        public int fps = 30;

        /// <summary>Stops on its own after this many frames (5 s at 30).</summary>
        public int maxFrames = 150;

        private string _dir = "";
        private int _frame;

        /// <summary>True while frames are being written.</summary>
        public bool Recording { get; private set; }

        /// <summary>Starts if idle, stops if recording.</summary>
        public void Toggle()
        {
            if (Recording)
            {
                Stop("stopped by hand");
            }
            else
            {
                Begin();
            }
        }

        private void Begin()
        {
            // Every take gets its own folder, and nothing is ever deleted.
            //
            // This used to clear one shared folder on start, which sounded
            // tidy and was not: a good putt is maybe one attempt in four, so
            // the takes worth keeping were being overwritten by the ones that
            // followed them. Frames are cheap and a take you have to go back
            // into Play mode to recreate is not.
            var root = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "artifacts", "hero"));
            Directory.CreateDirectory(root);

            int take = 1;
            while (Directory.Exists(Path.Combine(root, $"take-{take:00}")))
            {
                take++;
            }

            _dir = Path.Combine(root, $"take-{take:00}");
            Directory.CreateDirectory(_dir);
            _frame = 0;
            Recording = true;
            Time.captureFramerate = fps;
            Debug.Log($"PuttSeed: recording {maxFrames} frames at {fps} fps -> {_dir}");
            StartCoroutine(Capture());
        }

        private IEnumerator Capture()
        {
            while (Recording && _frame < maxFrames)
            {
                // End of frame, or the capture reads a half-drawn one.
                yield return new WaitForEndOfFrame();

                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                var png = texture.EncodeToPNG();
                Destroy(texture);

                // Written synchronously and on purpose. The async
                // CaptureScreenshot can coalesce writes when called every
                // frame, which silently drops frames from the middle of a
                // sequence; the stall this costs is invisible in the result
                // because captureFramerate already decided what a frame is
                // worth in game time.
                File.WriteAllBytes(Path.Combine(_dir, $"frame-{_frame:0000}.png"), png);
                _frame++;
            }

            if (Recording)
            {
                Stop("reached maxFrames");
            }
        }

        private void Stop(string why)
        {
            Recording = false;
            Time.captureFramerate = 0;
            Debug.Log($"PuttSeed: recorded {_frame} frames ({why}) -> {_dir}\n" +
                "Build the GIF with: python tools/make-gif.py");
        }

        private void OnDisable()
        {
            // Leaving captureFramerate set would keep the editor running the
            // game at a pinned delta long after the recording ended, which
            // reads as "the game got slow" and is very hard to guess at.
            if (Recording)
            {
                Stop("play mode ended");
            }
        }
    }
}
#endif
