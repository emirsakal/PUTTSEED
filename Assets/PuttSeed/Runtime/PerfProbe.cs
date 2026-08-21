#nullable enable
using UnityEngine;
using UnityEngine.Profiling;

namespace PuttSeed.Unity
{
    /// <summary>
    /// The three numbers nobody had: what a frame costs at the heaviest moment
    /// of the game, what a course costs to grow, and what the app is holding
    /// after a session of them.
    ///
    /// Every feel pass so far — bumps, particles, an extruded rail, a wind vane
    /// — added draw calls against no baseline at all, on a game whose target is
    /// a phone. This measures rather than guesses, and it measures the moment
    /// that matters: the capture celebration runs a zoom, a slow-motion replay,
    /// a letterboxed overlay, confetti and a star reveal at once, so if
    /// anything drops a frame it is that.
    ///
    /// It writes one line to the log per celebration. On device that is
    /// <c>adb logcat -s Unity</c>; in the editor it is the console. Costs a
    /// float compare per frame and allocates nothing.
    /// </summary>
    public sealed class PerfProbe : MonoBehaviour
    {
        /// <summary>Frames sampled after a capture — about a second at 120 Hz.</summary>
        private const int Window = 120;

        private readonly float[] _frames = new float[Window];
        private int _sampled;
        private bool _watching;
        private int _coursesGrown;
        private float _lastGenerationMs;

        /// <summary>Records how long the last course took to generate.</summary>
        public void ReportGeneration(float milliseconds)
        {
            _lastGenerationMs = milliseconds;
            _coursesGrown++;
        }

        /// <summary>Starts sampling frames — called when the ball drops.</summary>
        public void WatchCelebration()
        {
            _sampled = 0;
            _watching = true;
        }

        private void Update()
        {
            if (!_watching)
            {
                return;
            }

            _frames[_sampled++] = Time.unscaledDeltaTime * 1000f;
            if (_sampled < Window)
            {
                return;
            }

            _watching = false;
            Report();
        }

        private void Report()
        {
            // Insertion sort over 120 floats, once per hole-out: the median is
            // what a player feels and the worst is what a reviewer screenshots,
            // and both need the samples in order.
            for (int i = 1; i < Window; i++)
            {
                float value = _frames[i];
                int j = i - 1;
                while (j >= 0 && _frames[j] > value)
                {
                    _frames[j + 1] = _frames[j];
                    j--;
                }

                _frames[j + 1] = value;
            }

            float median = _frames[Window / 2];
            float worst = _frames[Window - 1];
            float ninetyFifth = _frames[(Window * 95) / 100];
            long memoryMb = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);

            Debug.Log($"PuttSeed perf · capture frame: median {median:F1} ms, " +
                $"95th {ninetyFifth:F1} ms, worst {worst:F1} ms · " +
                $"generation {_lastGenerationMs:F0} ms · " +
                $"memory {memoryMb} MB after {_coursesGrown} courses");
        }
    }
}
