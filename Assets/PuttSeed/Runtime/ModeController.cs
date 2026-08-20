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

        /// <summary>The 50 curated fixed-seed levels, unlocked in order.</summary>
        Journey,

        /// <summary>Seven consecutive dailies as one round, strokes cumulative.</summary>
        Gauntlet,
    }

    /// <summary>
    /// Orchestrates modes, stats and streak on top of the runner. Reads the
    /// clock and device entropy HERE (never in core) and forwards only derived
    /// values across the boundary. Daily records apply only while today's
    /// daily seed is actually loaded.
    /// </summary>
    public sealed class ModeController : MonoBehaviour
    {
        /// <summary>
        /// The generator the curated campaign was picked from. Journey seeds
        /// mean nothing without it: a level is a seed AND the version that
        /// grows it, and the ramp was curated on v4's par distribution.
        /// </summary>
        public const int JourneyVersion = 4;

        /// <summary>The generator practice runs (see <see cref="PracticeCourses"/>).</summary>
        public const int PracticeVersion = PracticeCourses.Version;

        private SimRunner _runner = null!;
        private CourseRenderer _courseRenderer = null!;
        private Camera _camera = null!;
        private StatsStore _stats = null!;
        private ShotLog? _shotLog;
        private LoadingOverlay? _overlay;

        // The ACTIVE daily: today's, or a past day picked from the archive.
        private ulong _dailySeed;
        private int _activeDayNumber;
        private DateTime _activeDayDate;
        private bool _completionRecorded;

        /// <summary>
        /// True when the run just holed out was the day's FIRST finish — the
        /// one that counts. Retries stay unlimited because the loop is built on
        /// them, but a score taken on the thirty-fourth attempt is nobody
        /// else's score, so only this one fills the streak, the calendar and
        /// the share.
        /// </summary>
        public bool WasFirstFinish { get; private set; } = true;

        /// <summary>
        /// True while today's hole has already been answered and this run is
        /// therefore practice on it. The retry is still instant and still
        /// unlimited — it just no longer pretends to be the day's score.
        /// </summary>
        public bool DailyAlreadyAnswered =>
            Mode == GameMode.Daily && !IsArchiveDay && !_completionRecorded
            && (_stats.FindDay(_activeDayNumber)?.completed ?? false);

        // Generator config version of the loaded course. Daily derives it from
        // the day number, journey and tutorial pin v1 forever, practice runs
        // the newest; replay codes carry theirs.
        private int _activeConfigVersion = 1;

        // Timing for a ghost that arrives with the course it belongs to.
        private int[] _pendingGhostClocks = System.Array.Empty<int>();

        /// <summary>Generator config version of the loaded course (share codes carry it).</summary>
        public int ActiveConfigVersion => _activeConfigVersion;

        /// <summary>
        /// Wire version NEW codes are written at. Courses that can hold a
        /// windmill are shared at v3, which records the clock each shot was
        /// taken at; v1 courses have no mills and stay on the short layout.
        /// </summary>
        public int ShareVersion => _activeConfigVersion switch
        {
            1 => 1,  // no windmill can exist there, so no shot needs a clock
            2 => 3,  // v2 geometry, shared on the timed layout
            _ => _activeConfigVersion,
        };

        /// <summary>The clocks to encode alongside the played shots.</summary>
        public int[] ShareShotClocks()
        {
            var clocks = new int[_runner.PlayedShotClocks.Count];
            for (int i = 0; i < clocks.Length; i++)
            {
                clocks[i] = _runner.PlayedShotClocks[i];
            }

            return clocks;
        }

        /// <summary>True when the loaded daily is a past day from the archive.</summary>
        public bool IsArchiveDay { get; private set; }

        // The gauntlet in flight: which week, which of its seven holes, the
        // strokes banked from the holes already finished, and the shots each
        // took (a whole week shares one code).
        private int _gauntletWeek = -1;
        private int _gauntletHole;
        private int _gauntletBankedStrokes;
        private readonly ShotInput[][] _gauntletShots = new ShotInput[GauntletWeek.Length][];
        private readonly int[][] _gauntletClocks = new int[GauntletWeek.Length][];

        /// <summary>The gauntlet week in flight (-1 outside the mode).</summary>
        public int GauntletWeekIndex => _gauntletWeek;

        /// <summary>Which hole of the gauntlet is loaded (0-based).</summary>
        public int GauntletHole => _gauntletHole;

        /// <summary>Strokes banked from finished holes, excluding this one.</summary>
        public int GauntletBankedStrokes => _gauntletBankedStrokes;

        /// <summary>Total strokes so far, this hole included.</summary>
        public int GauntletTotalStrokes =>
            _gauntletBankedStrokes + (_runner.Sim?.Strokes ?? 0);

        /// <summary>True when the loaded hole is not the last of the week.</summary>
        public bool HasNextGauntletHole =>
            Mode == GameMode.Gauntlet && _gauntletHole + 1 < GauntletWeek.Length;

        /// <summary>Raised when a gauntlet finishes, with its stroke total.</summary>
        public event Action<int>? GauntletFinished;

        /// <summary>The themed twist the loaded course plays under.</summary>
        public DailyMutator ActiveMutator { get; private set; } = DailyMutator.None;

        /// <summary>Localized name of the active twist, or "" on a plain day.</summary>
        public string MutatorLabel => ActiveMutator switch
        {
            DailyMutator.Icy => Loc.Tr("Icy day"),
            DailyMutator.Bouncy => Loc.Tr("Bouncy day"),
            DailyMutator.Windy => Loc.Tr("Windy day") + " "
                + WindVane.SpeedLabel(FixView.ToVector2(_runner.PlayConfig.Wind)),
            _ => "",
        };

        /// <summary>HUD label for daily mode ("Daily", or dated for archive days).</summary>
        public string DailyModeLabel => IsArchiveDay
            ? string.Format(Loc.Tr("Daily · {0}"), Loc.ShortDate(_activeDayDate))
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
        {
            bool isDaily = Mode == GameMode.Daily && _runner.Seed == _dailySeed && _dailySeed != 0;
            // A run after the day's first finish is practice on today's hole:
            // shareable, but never dressed up as the day's answer.
            string tail = isDaily && !WasFirstFinish ? " · practice run" : "";
            var text = new System.Text.StringBuilder(isDaily
                ? $"PUTTSEED day {_activeDayNumber} — {strokes} strokes (par {par}){tail}"
                : $"PUTTSEED — {strokes} strokes (par {par})");

            // The scorecard. A replay code proves the run and reads as noise;
            // this row is the part a stranger can actually see.
            string glyphs = _shotLog != null ? _shotLog.Glyphs() : "";
            if (glyphs.Length > 0)
            {
                text.Append('\n').Append(glyphs);
            }

            if (isDaily && _stats.Data.streak > 0)
            {
                text.Append('\n').Append($"🔥 {_stats.Data.streak}-day streak");
            }

            // The code goes last: it is the proof, not the pitch.
            return text.Append($"\nWatch: {code}").ToString();
        }

        /// <summary>The run's scorecard, filled by the feedback observer.</summary>
        public void SetShotLog(ShotLog log) => _shotLog = log;

        /// <summary>The day being played (0 outside daily and archive runs).</summary>
        public int ActiveDayNumber => _activeDayNumber;

        /// <summary>
        /// Starts whatever the menu put into <see cref="GameSession"/> —
        /// called once by the game scene's bootstrap.
        /// </summary>
        public void StartFromSession()
        {
            PracticeDifficulty = GameSession.PracticeDifficulty;
            if (GameSession.UseFixedSeed)
            {
                StartFixedSeed(GameSession.FixedSeed, GameSession.FixedSeedConfigVersion);
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
                case GameMode.Journey:
                    StartJourney(GameSession.JourneyLevel);
                    break;
                case GameMode.Gauntlet:
                    StartGauntlet(GameSession.GauntletWeekIndex);
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

        /// <summary>
        /// Starts a week's gauntlet at its first hole. The seven courses are
        /// the dailies that week already shipped — no new content, and every
        /// player runs the same seven.
        /// </summary>
        public void StartGauntlet(int weekIndex)
        {
            Mode = GameMode.Gauntlet;
            CurrentHint = "";
            IsArchiveDay = false;
            JourneyLevel = -1;
            _gauntletWeek = weekIndex;
            _gauntletHole = 0;
            _gauntletBankedStrokes = 0;
            for (int h = 0; h < GauntletWeek.Length; h++)
            {
                _gauntletShots[h] = Array.Empty<ShotInput>();
                _gauntletClocks[h] = Array.Empty<int>();
            }

            LoadGauntletHole();
        }

        /// <summary>Banks the finished hole and loads the next one.</summary>
        public void NextGauntletHole()
        {
            if (!HasNextGauntletHole)
            {
                return;
            }

            BankGauntletHole();
            _gauntletHole++;
            LoadGauntletHole();
        }

        private void LoadGauntletHole()
        {
            int day = GauntletWeek.DayOfHole(_gauntletWeek, _gauntletHole);
            LoadAndShow(DailyCalendar.SeedForDay(day),
                configVersion: GeneratorSchedule.VersionForDay(day));
        }

        /// <summary>
        /// Moves this hole's strokes and shots into the run's totals. A failed
        /// hole banks the limit it spent and the week carries on — one bad
        /// hole should cost a week, not end it.
        /// </summary>
        private void BankGauntletHole()
        {
            var sim = _runner.Sim;
            if (sim == null || _gauntletHole >= GauntletWeek.Length)
            {
                return;
            }

            _gauntletBankedStrokes += sim.Strokes;
            var shots = new ShotInput[_runner.PlayedShots.Count];
            var clocks = new int[_runner.PlayedShotClocks.Count];
            for (int i = 0; i < shots.Length; i++)
            {
                shots[i] = _runner.PlayedShots[i];
            }

            for (int i = 0; i < clocks.Length; i++)
            {
                clocks[i] = _runner.PlayedShotClocks[i];
            }

            _gauntletShots[_gauntletHole] = shots;
            _gauntletClocks[_gauntletHole] = clocks;
        }

        /// <summary>
        /// Ends the week once the seventh hole is done. Earlier holes wait for
        /// the player to press on, so a finished hole can still be admired.
        /// </summary>
        private void FinishGauntletHoleIfLast()
        {
            if (HasNextGauntletHole)
            {
                return;
            }

            BankGauntletHole();
            _stats.RecordGauntlet(_gauntletWeek, _gauntletBankedStrokes);
            GauntletFinished?.Invoke(_gauntletBankedStrokes);
        }

        /// <summary>The whole week as one shareable code.</summary>
        public string BuildGauntletCode() =>
            GauntletCodec.Encode(_gauntletWeek, _gauntletShots, _gauntletClocks);

        /// <summary>Loads today's daily course.</summary>
        public void StartDaily()
        {
            Mode = GameMode.Daily;
            CurrentHint = "";
            IsArchiveDay = false;
            JourneyLevel = -1;
            _gauntletWeek = -1;
            var utc = DateTime.UtcNow;
            _activeDayNumber = DayNumber(utc);
            _activeDayDate = utc.Date;
            _dailySeed = DailySeed.FromUtcDate(utc.Year, utc.Month, utc.Day);
            LoadAndShow(_dailySeed, configVersion: GeneratorSchedule.VersionForDay(_activeDayNumber));
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
            JourneyLevel = -1;
            _activeDayNumber = dayNumber;
            _activeDayDate = DateOfDay(dayNumber);
            _dailySeed = DailySeed.FromUtcDate(
                _activeDayDate.Year, _activeDayDate.Month, _activeDayDate.Day);
            // Past days regenerate with the config they shipped under, forever.
            LoadAndShow(_dailySeed, configVersion: GeneratorSchedule.VersionForDay(dayNumber));
        }

        /// <summary>Starts a practice course in the current difficulty bucket.</summary>
        public void StartPractice()
        {
            Mode = GameMode.Practice;
            CurrentHint = "";
            IsArchiveDay = false;
            JourneyLevel = -1;
            StartCoroutine(GeneratePracticeCourse());
        }

        /// <summary>Loads a tutorial stage.</summary>
        public void StartTutorial(int index)
        {
            Mode = GameMode.Tutorial;
            IsArchiveDay = false;
            JourneyLevel = -1;
            TutorialIndex = ((index % TutorialConfig.Stages.Length) + TutorialConfig.Stages.Length)
                % TutorialConfig.Stages.Length;
            var stage = TutorialConfig.Stages[TutorialIndex];
            CurrentHint = stage.Hint;
            // Curated layouts, frozen to the generator they were picked from:
            // the first four are v1 forever, the element wave needs v2.
            LoadAndShow(stage.Seed, configVersion: stage.ConfigVersion);
        }

        /// <summary>True while a lesson after this one is still waiting.</summary>
        public bool HasNextTutorialStage =>
            Mode == GameMode.Tutorial && TutorialIndex + 1 < TutorialConfig.Stages.Length;

        /// <summary>Advances to the next tutorial stage (wraps).</summary>
        public void NextTutorial() => StartTutorial(TutorialIndex + 1);

        /// <summary>The active journey level (0-based; -1 outside the mode).</summary>
        public int JourneyLevel { get; private set; } = -1;

        /// <summary>Loads a journey level (clamped to the unlocked range).</summary>
        public void StartJourney(int level)
        {
            Mode = GameMode.Journey;
            CurrentHint = "";
            IsArchiveDay = false;
            JourneyLevel = Mathf.Clamp(level, 0, _stats.UnlockedJourneyLevels(JourneyConfig.Seeds.Length) - 1);
            // The campaign was curated under v1; its layouts are frozen.
            LoadAndShow(JourneyConfig.Seeds[JourneyLevel], configVersion: JourneyVersion);
        }

        /// <summary>True when a next journey level exists and is unlocked.</summary>
        public bool HasNextJourneyLevel =>
            Mode == GameMode.Journey
            && JourneyLevel + 1 < _stats.UnlockedJourneyLevels(JourneyConfig.Seeds.Length);

        /// <summary>Advances to the next unlocked journey level.</summary>
        public void NextJourneyLevel()
        {
            if (HasNextJourneyLevel)
            {
                StartJourney(JourneyLevel + 1);
            }
        }

        /// <summary>Bootstrap testing hook: load a specific seed, practice-style.</summary>
        public void StartFixedSeed(ulong seed, int configVersion = 1)
        {
            Mode = GameMode.Practice;
            CurrentHint = "";
            IsArchiveDay = false;
            JourneyLevel = -1;
            LoadAndShow(seed, configVersion: configVersion);
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

            if (!ReplayCodec.TryDecode(text.Substring(at, end - at), out var seed, out var shots,
                out var configVersion, out var shotClocks))
            {
                return false;
            }

            // Same seed under a different config is a DIFFERENT course — the
            // ghost would desync. Compare the resolved CONFIG, not the wire
            // number: v2 and v3 codes describe the same courses.
            if (seed != _runner.Seed
                || GeneratorConfig.ForVersion(configVersion)
                   != GeneratorConfig.ForVersion(_activeConfigVersion))
            {
                Mode = seed == _dailySeed && configVersion == GeneratorSchedule.VersionForDay(_activeDayNumber)
                    ? GameMode.Daily
                    : GameMode.Practice;
                CurrentHint = "";
                _pendingGhostClocks = shotClocks;
                LoadAndShow(seed, ghostShots: shots, configVersion: configVersion);
            }
            else if (shots.Length > 0)
            {
                _runner.AddGhost(shots, "import", shotClocks);
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

            // The next course may already be growing (see PrewarmPractice).
            // A candidate search is up to eight generations, and a v4
            // generation is not free: without this, every "new course" tap
            // would sit on the loading cover for as long as the search took.
            // Three places it can come from, cheapest first: the menu grew one
            // while the player was still deciding, this scene grew one while
            // the last course was being played, or nobody did and we pay now.
            var handed = GameSession.TakePreparedPractice(PracticeDifficulty);
            var search = handed != null
                ? Task.FromResult(handed.Value)
                : _nextPractice != null && _nextPracticeBucket == PracticeDifficulty
                    ? _nextPractice
                    : Task.Run(SearchArguments());
            _nextPractice = null;

            while (!search.IsCompleted)
            {
                yield return null; // frames render; the loading putt rolls on
            }

            var found = search.Status == TaskStatus.RanToCompletion
                ? search.Result
                : default;
            if (found.Result != null)
            {
                _activeConfigVersion = PracticeVersion;
                ActiveMutator = DailyMutators.ForSeed(found.Seed, PracticeVersion);
                _runner.AdoptGeneration(found.Seed, found.Result, found.Config);
                RebuildView();
                _stats.RecordPracticePlayed();
            }

            PrewarmPractice();

            _overlay?.Hide();
            IsLoading = false;
            ModeChanged?.Invoke();
        }

        // The next practice course, grown in the background while the player
        // is still on the current one, and the bucket it was grown for.
        private Task<PracticeCourses.Candidate>? _nextPractice;
        private Difficulty _nextPracticeBucket;

        /// <summary>Builds the background search for the current bucket.</summary>
        private Func<PracticeCourses.Candidate> SearchArguments()
        {
            var seeds = PracticeCourses.DrawSeeds();
            var baseConfig = _runner.feel != null ? _runner.feel.BuildSimConfig() : SimConfig.Default;
            var want = PracticeDifficulty;
            return () => PracticeCourses.Search(seeds, want, baseConfig);
        }

        /// <summary>Starts growing the next practice course, if one is wanted.</summary>
        private void PrewarmPractice()
        {
            if (Mode != GameMode.Practice)
            {
                return;
            }

            _nextPracticeBucket = PracticeDifficulty;
            _nextPractice = Task.Run(SearchArguments());
        }

        private void LoadAndShow(ulong seed, ShotInput[]? ghostShots = null, int configVersion = 1)
        {
            StartCoroutine(LoadRoutine(seed, ghostShots, configVersion));
        }

        /// <summary>
        /// Every course load goes through here: cover the screen, run the
        /// generation on a BACKGROUND thread (core is pure C#, no Unity API),
        /// and keep rendering frames while it works — the overlay's putt
        /// vignette genuinely rolls toward the cup during the load. The result
        /// is adopted on the main thread, then Hide plays the drop-in.
        /// </summary>
        private IEnumerator LoadRoutine(ulong seed, ShotInput[]? ghostShots, int configVersion)
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
            // The day's twist is part of the physics BEFORE generation runs:
            // the solver must prove the course under the same wind or ice the
            // player will meet, or a themed day could be an unfair one.
            var config = DailyMutators.Apply(
                _runner.feel != null ? _runner.feel.BuildSimConfig() : SimConfig.Default,
                seed, configVersion);
            var genConfig = GeneratorConfig.ForVersion(configVersion);

            // The budget a version was PROVEN under: a v4 hole solved on the v2
            // budget would be discarded before its three-stroke solution was
            // found, and the generator would quietly hand back a par 2.
            var solverConfig = SolverConfig.ForVersion(configVersion);

            // The menu may already have grown this exact hole for its thumbnail.
            var prepared = GameSession.TakePrepared(seed, configVersion);
            var task = prepared != null
                ? Task.FromResult(prepared)
                : Task.Run(() => CourseGenerator.Generate(seed, genConfig, config, solverConfig));
            while (!task.IsCompleted)
            {
                yield return null; // frames render; the ball rolls while we wait
            }

            if (task.Status == TaskStatus.RanToCompletion)
            {
                _activeConfigVersion = configVersion;
                ActiveMutator = DailyMutators.ForSeed(seed, configVersion);
                _runner.AdoptGeneration(seed, task.Result, config);
                RebuildView();
                // Zero-shot codes are course invitations — no ghost to race.
                if (ghostShots != null && ghostShots.Length > 0)
                {
                    _runner.AddGhost(ghostShots, "import", _pendingGhostClocks);
                }

                _pendingGhostClocks = System.Array.Empty<int>();

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

            // A hint chip rides under the top bar in teaching modes, so the
            // course gets less room on exactly the holes that show one.
            CameraFramer.Frame(_camera, _runner.Generation.Course,
                CurrentHint.Length > 0 ? CameraFramer.TopChromeWithHint : CameraFramer.TopChrome);
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
            if (Mode == GameMode.Gauntlet)
            {
                FinishGauntletHoleIfLast();
            }
            else if (Mode == GameMode.Daily && _runner.Seed == _dailySeed && _dailySeed != 0)
            {
                var shots = new ShotInput[_runner.PlayedShots.Count];
                for (int i = 0; i < shots.Length; i++)
                {
                    shots[i] = _runner.PlayedShots[i];
                }

                // Asked BEFORE recording: afterwards every day looks finished.
                WasFirstFinish = !(_stats.FindDay(_activeDayNumber)?.completed ?? false);
                _stats.RecordDailyCompletion(
                    _activeDayNumber, sim.Strokes,
                    Scoring.Stars(sim.Strokes, _runner.Generation!.Course.Par),
                    ReplayCodec.Encode(_runner.Seed, shots, ShareShotClocks(), ShareVersion),
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
            else if (Mode == GameMode.Journey && JourneyLevel >= 0 && _runner.Generation != null)
            {
                _stats.RecordJourneyResult(JourneyLevel,
                    Scoring.Stars(sim.Strokes, _runner.Generation.Course.Par));
            }

            // Achievements see the post-record save, so streak/day counts
            // already include this run.
            var course = _runner.Generation!.Course;
            var facts = new Achievements.RunFacts(
                Mode, IsArchiveDay,
                sim.Strokes, course.Par, sim.StrokeLimit,
                sim.WallHitCount, sim.WallHitsThisShot, sim.TouchedHazard,
                course.Windmills.Length > 0, sim.WindmillHitCount);
            var earned = Achievements.EvaluateRun(_stats.Data, facts);
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

            // A best recorded under another generator version is a different
            // course — never race a desyncing ghost.
            if (ReplayCodec.TryDecode(record.bestReplay, out var seed, out var shots, out var version,
                    out var clocks)
                && seed == _dailySeed
                && GeneratorConfig.ForVersion(version)
                   == GeneratorConfig.ForVersion(_activeConfigVersion))
            {
                _runner.AddGhost(shots, "best", clocks);
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
