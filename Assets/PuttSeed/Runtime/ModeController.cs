#nullable enable
using System;
using System.Collections;
using System.Threading.Tasks;
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
        private LoadingOverlay? _overlay;

        // The ACTIVE daily: today's, or a past day picked from the archive.
        private ulong _dailySeed;
        private int _activeDayNumber;
        private DateTime _activeDayDate;
        private bool _completionRecorded;

        /// <summary>True when the loaded daily is a past day from the archive.</summary>
        public bool IsArchiveDay { get; private set; }

        /// <summary>HUD label for daily mode ("Daily", or dated for archive days).</summary>
        public string DailyModeLabel => IsArchiveDay
            ? string.Format(Loc.Tr("Daily · {0}"), $"{_activeDayDate:MMM d}")
            : Loc.Tr("Daily");

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

        /// <summary>Raised once per newly unlocked achievement (toast hook).</summary>
        public event Action<AchievementDef>? AchievementUnlocked;

        /// <summary>Raised when a practice run sets a new personal best.</summary>
        public event Action<int>? PracticeBestImproved;

        /// <summary>True while a course is being generated behind the overlay.</summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// Aim-preview policy: teaching contexts only. The daily and harder
        /// practice buckets keep the GDD's read-the-green skill intact.
        /// </summary>
        public bool AimPreviewAllowed =>
            Mode == GameMode.Tutorial
            || (Mode == GameMode.Practice && _runner.Generation?.Difficulty == Difficulty.Easy);

        /// <summary>Wires dependencies; the store is shared with FeedbackController.</summary>
        public void Initialize(SimRunner runner, CourseRenderer courseRenderer, Camera cam,
            LoadingOverlay? overlay, StatsStore stats)
        {
            _runner = runner;
            _courseRenderer = courseRenderer;
            _camera = cam;
            _overlay = overlay;
            _stats = stats;
            runner.StateChanged += OnStateChanged;
            runner.RunReset += OnRunReset;
        }

        /// <summary>
        /// The share text for a finished run — the GDD format with the day
        /// number when today's daily is loaded, plain otherwise.
        /// </summary>
        public string BuildShareText(int strokes, int par, string code)
            => Mode == GameMode.Daily && _runner.Seed == _dailySeed && _dailySeed != 0
                ? $"PUTTSEED day {_activeDayNumber} — {strokes} strokes (par {par}). Watch: {code}"
                : $"PUTTSEED — {strokes} strokes (par {par}). Watch: {code}";

        /// <summary>
        /// Starts whatever the menu put into <see cref="GameSession"/> —
        /// called once by the game scene's bootstrap.
        /// </summary>
        public void StartFromSession()
        {
            PracticeDifficulty = GameSession.PracticeDifficulty;
            if (GameSession.UseFixedSeed)
            {
                StartFixedSeed(GameSession.FixedSeed);
                return;
            }

            switch (GameSession.Mode)
            {
                case GameMode.Practice:
                    StartPractice();
                    break;
                case GameMode.Tutorial:
                    StartTutorial(GameSession.TutorialIndex);
                    break;
                default:
                    if (GameSession.ArchiveDayNumber >= 0)
                    {
                        StartArchiveDay(GameSession.ArchiveDayNumber);
                        GameSession.ArchiveDayNumber = -1;
                    }
                    else
                    {
                        StartDaily();
                    }

                    break;
            }
        }

        /// <summary>Loads today's daily course.</summary>
        public void StartDaily()
        {
            Mode = GameMode.Daily;
            CurrentHint = "";
            IsArchiveDay = false;
            var utc = DateTime.UtcNow;
            _activeDayNumber = DayNumber(utc);
            _activeDayDate = utc.Date;
            _dailySeed = DailySeed.FromUtcDate(utc.Year, utc.Month, utc.Day);
            LoadAndShow(_dailySeed);
        }

        /// <summary>
        /// Loads a past day's course from the archive. The date alone
        /// regenerates it — no storage. Stats fill that day's record, but the
        /// completion never counts toward the streak.
        /// </summary>
        public void StartArchiveDay(int dayNumber)
        {
            Mode = GameMode.Daily;
            CurrentHint = "";
            IsArchiveDay = true;
            _activeDayNumber = dayNumber;
            _activeDayDate = DateOfDay(dayNumber);
            _dailySeed = DailySeed.FromUtcDate(
                _activeDayDate.Year, _activeDayDate.Month, _activeDayDate.Day);
            LoadAndShow(_dailySeed);
        }

        /// <summary>Starts a practice course in the current difficulty bucket.</summary>
        public void StartPractice()
        {
            Mode = GameMode.Practice;
            CurrentHint = "";
            IsArchiveDay = false;
            StartCoroutine(GeneratePracticeCourse());
        }

        /// <summary>Loads a tutorial stage.</summary>
        public void StartTutorial(int index)
        {
            Mode = GameMode.Tutorial;
            IsArchiveDay = false;
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
            IsArchiveDay = false;
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
                LoadAndShow(seed, ghostShots: shots); // ghost attaches after the load
            }
            else if (shots.Length > 0)
            {
                _runner.AddGhost(shots, "import");
            }

            return true;
        }

        private IEnumerator GeneratePracticeCourse()
        {
            if (IsLoading)
            {
                yield break;
            }

            IsLoading = true;
            _overlay?.Show(Loc.Tr("Generating course"));
            ModeChanged?.Invoke();
            yield return null; // let the overlay render first

            var entropy = new System.Random();
            var config = _runner.feel != null ? _runner.feel.BuildSimConfig() : SimConfig.Default;
            GenerationResult? best = null;
            ulong bestSeed = 0;
            int bestDistance = int.MaxValue;

            for (int t = 0; t < PracticeCandidateTries; t++)
            {
                var buffer = new byte[8];
                entropy.NextBytes(buffer);
                ulong seed = BitConverter.ToUInt64(buffer, 0);

                // Background generation: frames (and the putt vignette) keep
                // running while each candidate is solved.
                var task = Task.Run(() => CourseGenerator.Generate(
                    seed, GeneratorConfig.Default, config, SolverConfig.Default));
                while (!task.IsCompleted)
                {
                    yield return null;
                }

                if (task.Status != TaskStatus.RanToCompletion)
                {
                    _ = task.Exception; // observed; bounded-generation misses just retry
                    continue;
                }

                var candidate = task.Result;
                // Keep the candidate whose rated bucket is CLOSEST to the
                // requested one (exact match ends the search) — never an
                // arbitrary first course when the bucket is unlucky.
                int distance = Math.Abs((int)candidate.Difficulty - (int)PracticeDifficulty);
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestSeed = seed;
                    bestDistance = distance;
                }

                if (distance == 0)
                {
                    break;
                }
            }

            if (best != null)
            {
                _runner.AdoptGeneration(bestSeed, best, config);
                RebuildView();
                _stats.RecordPracticePlayed();
            }

            _overlay?.Hide();
            IsLoading = false;
            ModeChanged?.Invoke();
        }

        private void LoadAndShow(ulong seed, ShotInput[]? ghostShots = null)
        {
            StartCoroutine(LoadRoutine(seed, ghostShots));
        }

        /// <summary>
        /// Every course load goes through here: cover the screen, run the
        /// generation on a BACKGROUND thread (core is pure C#, no Unity API),
        /// and keep rendering frames while it works — the overlay's putt
        /// vignette genuinely rolls toward the cup during the load. The result
        /// is adopted on the main thread, then Hide plays the drop-in.
        /// </summary>
        private IEnumerator LoadRoutine(ulong seed, ShotInput[]? ghostShots)
        {
            if (IsLoading)
            {
                yield break;
            }

            IsLoading = true;
            _overlay?.Show(Loc.Tr("Generating course"));
            ModeChanged?.Invoke();

            // Read Unity-side state (the ScriptableObject) on the main thread;
            // the task only touches pure core types.
            var config = _runner.feel != null ? _runner.feel.BuildSimConfig() : SimConfig.Default;
            var task = Task.Run(() => CourseGenerator.Generate(
                seed, GeneratorConfig.Default, config, SolverConfig.Default));
            while (!task.IsCompleted)
            {
                yield return null; // frames render; the ball rolls while we wait
            }

            if (task.Status == TaskStatus.RanToCompletion)
            {
                _runner.AdoptGeneration(seed, task.Result, config);
                RebuildView();
                // Zero-shot codes are course invitations — no ghost to race.
                if (ghostShots != null && ghostShots.Length > 0)
                {
                    _runner.AddGhost(ghostShots, "import");
                }

                AttachBestGhostIfDaily();
            }
            else
            {
                Debug.LogError($"PuttSeed: generation failed for seed {seed}: " +
                    task.Exception?.GetBaseException().Message);
            }

            _overlay?.Hide();
            IsLoading = false;
            ModeChanged?.Invoke();
        }

        private void RebuildView()
        {
            _courseRenderer.Rebuild(_runner.Generation!.Course, _runner.Seed);
            CameraFramer.Frame(_camera, _runner.Generation.Course);
        }

        private void OnRunReset()
        {
            _completionRecorded = false;
            if (Mode == GameMode.Daily && _runner.Seed == _dailySeed && _dailySeed != 0)
            {
                _stats.RecordDailyAttempt(_activeDayNumber);
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
                    _activeDayNumber, sim.Strokes,
                    Scoring.Stars(sim.Strokes, _runner.Generation!.Course.Par),
                    ReplayCodec.Encode(_runner.Seed, shots),
                    countsForStreak: !IsArchiveDay);

                // The next retry races the (possibly new) best run.
                _runner.RemoveGhosts("best");
                AttachBestGhostIfDaily();
            }
            else if (Mode == GameMode.Practice && _runner.Generation != null
                && _stats.RecordPracticeBest((int)_runner.Generation.Difficulty, sim.Strokes))
            {
                PracticeBestImproved?.Invoke(sim.Strokes);
            }

            // Achievements see the post-record save, so streak/day counts
            // already include this run.
            var earned = Achievements.EvaluateRun(_stats.Data, Mode, IsArchiveDay,
                sim.Strokes, _runner.Generation!.Course.Par, sim.WallHitCount);
            for (int i = 0; i < earned.Count; i++)
            {
                var def = Achievements.Find(earned[i]);
                if (def != null && _stats.Unlock(def.Id))
                {
                    AchievementUnlocked?.Invoke(def);
                }
            }

            ModeChanged?.Invoke();
        }

        /// <summary>
        /// In daily mode, attaches today's stored best replay as a ghost — the
        /// GDD's "race your best" retention surface. No-op before the first
        /// completion or when the stored code belongs to another seed.
        /// </summary>
        private void AttachBestGhostIfDaily()
        {
            if (Mode != GameMode.Daily || _runner.Seed != _dailySeed || _dailySeed == 0)
            {
                return;
            }

            var record = _stats.GetOrCreateDay(_activeDayNumber);
            if (!record.completed || record.bestReplay.Length == 0)
            {
                return;
            }

            if (ReplayCodec.TryDecode(record.bestReplay, out var seed, out var shots) && seed == _dailySeed)
            {
                _runner.AddGhost(shots, "best");
            }
        }

        /// <summary>Days since 2020-01-01 UTC — the streak arithmetic unit.</summary>
        public static int DayNumber(DateTime utc)
            => (int)(utc.Date - new DateTime(2020, 1, 1)).TotalDays;

        /// <summary>The UTC date of a day number (inverse of <see cref="DayNumber"/>).</summary>
        public static DateTime DateOfDay(int dayNumber)
            => new DateTime(2020, 1, 1).AddDays(dayNumber);
    }
}
