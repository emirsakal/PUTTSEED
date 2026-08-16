#nullable enable
using System;
using System.Collections;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.Daily;
using PuttSeed.Core.Replay;
using PuttSeed.Core.Sim;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>The GDD's play modes.</summary>
    public enum GameMode
    {
        /// <summary>Today's UTC-date-seeded course, stats and streak tracked.</summary>
        Daily,

        /// <summary>Random courses filtered by difficulty bucket; unlimited.</summary>
        Practice,

        /// <summary>Three fixed-seed teaching courses with hint lines.</summary>
        Tutorial,
    }

    /// <summary>
    /// Orchestrates modes, stats and streak on top of the runner. Reads the
    /// clock and device entropy HERE (never in core) and forwards only derived
    /// values across the boundary. Daily records apply only while today's
    /// daily seed is actually loaded.
    /// </summary>
    public sealed class ModeController : MonoBehaviour
    {
        private const int PracticeCandidateTries = 8;

        private SimRunner _runner = null!;
        private CourseRenderer _courseRenderer = null!;
        private Camera _camera = null!;
        private StatsStore _stats = null!;

        private ulong _dailySeed;
        private int _todayDayNumber;
        private bool _completionRecorded;

        /// <summary>The active mode.</summary>
        public GameMode Mode { get; private set; } = GameMode.Daily;

        /// <summary>Practice difficulty bucket filter.</summary>
        public Difficulty PracticeDifficulty { get; private set; } = Difficulty.Normal;

        /// <summary>Current tutorial stage index (0-based).</summary>
        public int TutorialIndex { get; private set; }

        /// <summary>Hint line for the current course ("" outside tutorials).</summary>
        public string CurrentHint { get; private set; } = "";

        /// <summary>Local stats (streak, per-day records, practice count).</summary>
        public StatsStore Stats => _stats;

        /// <summary>Raised when mode, hint or stats-visible state changes.</summary>
        public event Action? ModeChanged;

        /// <summary>Wires dependencies; statsPath is persistentDataPath-based in the app.</summary>
        public void Initialize(SimRunner runner, CourseRenderer courseRenderer, Camera cam, string statsPath)
        {
            _runner = runner;
            _courseRenderer = courseRenderer;
            _camera = cam;
            _stats = new StatsStore(statsPath);
            runner.StateChanged += OnStateChanged;
            runner.RunReset += OnRunReset;
        }

        /// <summary>True when the player has never completed a daily nor practiced (FTUE gate).</summary>
        public bool IsFirstLaunch =>
            _stats.Data.lastCompletedDay == 0 && _stats.Data.practicePlayed == 0 && _stats.Data.days.Count == 0;

        /// <summary>Loads today's daily course.</summary>
        public void StartDaily()
        {
            Mode = GameMode.Daily;
            CurrentHint = "";
            var utc = DateTime.UtcNow;
            _todayDayNumber = DayNumber(utc);
            _dailySeed = DailySeed.FromUtcDate(utc.Year, utc.Month, utc.Day);
            LoadAndShow(_dailySeed);
        }

        /// <summary>Starts a practice course in the current difficulty bucket.</summary>
        public void StartPractice()
        {
            Mode = GameMode.Practice;
            CurrentHint = "";
            StartCoroutine(GeneratePracticeCourse());
        }

        /// <summary>Cycles Easy → Normal → Hard and starts a matching course.</summary>
        public void CyclePracticeDifficulty()
        {
            PracticeDifficulty = (Difficulty)(((int)PracticeDifficulty + 1) % 3);
            StartPractice();
        }

        /// <summary>Loads a tutorial stage.</summary>
        public void StartTutorial(int index)
        {
            Mode = GameMode.Tutorial;
            TutorialIndex = ((index % TutorialConfig.Stages.Length) + TutorialConfig.Stages.Length)
                % TutorialConfig.Stages.Length;
            var stage = TutorialConfig.Stages[TutorialIndex];
            CurrentHint = stage.Hint;
            LoadAndShow(stage.Seed);
        }

        /// <summary>Advances to the next tutorial stage (wraps).</summary>
        public void NextTutorial() => StartTutorial(TutorialIndex + 1);

        /// <summary>Bootstrap testing hook: load a specific seed, practice-style.</summary>
        public void StartFixedSeed(ulong seed)
        {
            Mode = GameMode.Practice;
            CurrentHint = "";
            LoadAndShow(seed);
        }

        /// <summary>
        /// Plays a pasted PUTT- code as a ghost, loading its course first when
        /// it belongs to a different seed. Returns false for invalid codes.
        /// </summary>
        public bool ImportReplay(string text)
        {
            int at = text.IndexOf("PUTT-", StringComparison.Ordinal);
            if (at < 0)
            {
                return false;
            }

            int end = at;
            while (end < text.Length && !char.IsWhiteSpace(text[end]))
            {
                end++;
            }

            if (!ReplayCodec.TryDecode(text.Substring(at, end - at), out var seed, out var shots))
            {
                return false;
            }

            if (seed != _runner.Seed)
            {
                Mode = seed == _dailySeed ? GameMode.Daily : GameMode.Practice;
                CurrentHint = "";
                LoadAndShow(seed);
            }

            _runner.AddGhost(shots, "import");
            return true;
        }

        private IEnumerator GeneratePracticeCourse()
        {
            ModeChanged?.Invoke();
            yield return null; // let the "generating" frame render first

            var entropy = new System.Random();
            var config = _runner.feel != null ? _runner.feel.BuildSimConfig() : SimConfig.Default;
            GenerationResult? best = null;
            ulong bestSeed = 0;

            for (int t = 0; t < PracticeCandidateTries; t++)
            {
                var buffer = new byte[8];
                entropy.NextBytes(buffer);
                ulong seed = BitConverter.ToUInt64(buffer, 0);
                GenerationResult candidate;
                try
                {
                    candidate = CourseGenerator.Generate(
                        seed, GeneratorConfig.Default, config, SolverConfig.Default);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                if (best == null || candidate.Difficulty == PracticeDifficulty)
                {
                    best = candidate;
                    bestSeed = seed;
                }

                if (candidate.Difficulty == PracticeDifficulty)
                {
                    break;
                }

                yield return null; // keep the app responsive between tries
            }

            if (best != null)
            {
                _runner.AdoptGeneration(bestSeed, best, config);
                RebuildView();
                _stats.RecordPracticePlayed();
                ModeChanged?.Invoke();
            }
        }

        private void LoadAndShow(ulong seed)
        {
            _runner.LoadSeed(seed);
            RebuildView();
            ModeChanged?.Invoke();
        }

        private void RebuildView()
        {
            _courseRenderer.Rebuild(_runner.Generation!.Course);
            CameraFramer.Frame(_camera, _runner.Generation.Course);
        }

        private void OnRunReset()
        {
            _completionRecorded = false;
            if (Mode == GameMode.Daily && _runner.Seed == _dailySeed && _dailySeed != 0)
            {
                _stats.RecordDailyAttempt(_todayDayNumber);
            }
        }

        private void OnStateChanged()
        {
            var sim = _runner.Sim;
            if (sim == null || !sim.IsHoled || _completionRecorded)
            {
                return;
            }

            _completionRecorded = true;
            if (Mode == GameMode.Daily && _runner.Seed == _dailySeed && _dailySeed != 0)
            {
                var shots = new ShotInput[_runner.PlayedShots.Count];
                for (int i = 0; i < shots.Length; i++)
                {
                    shots[i] = _runner.PlayedShots[i];
                }

                _stats.RecordDailyCompletion(
                    _todayDayNumber, sim.Strokes, ReplayCodec.Encode(_runner.Seed, shots));
            }

            ModeChanged?.Invoke();
        }

        /// <summary>Days since 2020-01-01 UTC — the streak arithmetic unit.</summary>
        public static int DayNumber(DateTime utc)
            => (int)(utc.Date - new DateTime(2020, 1, 1)).TotalDays;
    }
}
