#nullable enable
using System;
using System.IO;
using PuttSeed.Core.CourseGen;
using UnityEngine;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// The menu scene's entry point. The UI hierarchy is scene-authored
    /// (baked by PuttSeed → Rebuild Scenes, editable in the Inspector and
    /// reskinnable with art assets); this component only binds behavior:
    /// fills the dynamic labels from stats and wires the button actions.
    /// </summary>
    public sealed class MenuBootstrap : MonoBehaviour
    {
        [Header("Scene-authored UI (assigned by PuttSeed → Rebuild Scenes)")]
        public Button? dailyButton;
        public Text? dailyLabel;
        public Button? practiceButton;
        public Button? difficultyButton;
        public Text? difficultyLabel;
        public Button? tutorialButton;
        public Text? tutorialLabel;
        public Text? footerText;
        public SegmentedToggle? soundToggle;
        public SegmentedToggle? hapticsToggle;
        public SegmentedToggle? aimToggle;
        public SegmentedToggle? colorblindToggle;
        public SegmentedToggle? batteryToggle;
        public SegmentedToggle? languageToggle;
        public Text? countdownText;
        public Button? archiveButton;
        public GameObject? archivePanel;
        public Button[] archiveCellButtons = new Button[0];
        public Text[] archiveCellLabels = new Text[0];
        public Text[] archiveWeekdayLabels = new Text[0];
        public Button? archivePrevMonthButton;
        public Button? archiveNextMonthButton;
        public Button? archiveCloseButton;
        public Text? archiveMonthLabel;
        public Button? statsButton;
        public GameObject? statsPanel;
        public Text? statsBlock;
        public Text? achievementsBlock;
        public Button? statsCloseButton;
        public Button? shareBestButton;
        public Text? shareBestLabel;
        public RectTransform? emblemBall;
        public RectTransform? deco1;
        public RectTransform? deco2;
        public Text? taglineText;
        public Image[] archiveCellStars = new Image[0];
        public Button? archiveRandomButton;
        public Text? histogramBlock;
        public Button? journeyButton;
        public Text? journeyLabel;
        public GameObject? journeyPanel;
        public Button? journeyCloseButton;
        public Button? journeyPrevButton;
        public Button? journeyNextButton;
        public Text? journeyPageLabel;
        public Button[] journeyCellButtons = new Button[0];
        public Text[] journeyCellLabels = new Text[0];
        public Image[] journeyCellStars = new Image[0];

        private int _journeyPage;
        public Button? settingsButton;
        public GameObject? settingsPanel;
        public Button? settingsCloseButton;
        public Button? collectionButton;
        public GameObject? collectionPanel;
        public Button? collectionCloseButton;
        public Button[] collectionCellButtons = new Button[0];
        public Text[] collectionCellLabels = new Text[0];
        public Image[] collectionCellSwatches = new Image[0];
        public Image[] collectionCellRings = new Image[0];
        public Text? collectionHintText;
        public SegmentedToggle? collectionTab;

        /// <summary>False = the grid shows ball skins, true = trails.</summary>
        private bool _collectionShowsTrails;

        // Calendar day cells: a finished day sits lit, a day still to come
        // is nearly invisible. Today takes the accent and needs no constant.
        private static readonly Color PlayedDayCell = new Color(0.10f, 0.22f, 0.13f, 0.95f);
        private static readonly Color FutureDayCell = new Color(0.03f, 0.06f, 0.04f, 0.55f);

        // Collection cell backgrounds carry the state: a locked cell sinks
        // almost to black, an equipped one lifts toward the felt. The swatch
        // and label are left alone — the tile behind them does the talking.
        private static readonly Color LockedCell = new Color(0.01f, 0.03f, 0.02f, 0.94f);
        private static readonly Color EquippedCell = new Color(0.12f, 0.24f, 0.14f, 0.96f);
        public InputField? saveField;
        public Button? importSaveButton;
        public Text? importSaveLabel;
        public Button? exportSaveButton;
        public Text? exportSaveLabel;

        private bool _importArmed;

        private bool _showCountdown;
        private StatsStore _stats = null!;
        // First day of the month the calendar is showing.
        private DateTime _archiveMonth;

        private static Color DifficultyColor(Difficulty difficulty) => difficulty switch
        {
            Difficulty.Easy => new Color(0.45f, 0.85f, 0.45f),
            Difficulty.Hard => new Color(0.95f, 0.36f, 0.3f),
            _ => new Color(0.99f, 0.85f, 0.35f),
        };

        private void Start()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = PaletteMaterials.Felt;
                cam.orthographic = true;
            }

            var stats = new StatsStore(StatsPath());
            _stats = stats;
            UiSounds.Enabled = stats.Data.soundEnabled;
            PaletteMaterials.ColorblindMode = stats.Data.colorblindMode;
            Application.targetFrameRate = stats.Data.batterySaver ? 60 : 120;
            UiPolish.EnsureButtonFeedback();
            bool firstLaunch = stats.Data.lastCompletedDay == 0
                && stats.Data.practicePlayed == 0
                && stats.Data.days.Count == 0;

            // FTUE (GDD): the very first launch drops straight into Tutorial 1.
            // One-shot — quitting the tutorial returns to a normal menu.
            if (firstLaunch && !stats.Data.tutorialSeen)
            {
                stats.MarkTutorialSeen();
                Launch(GameMode.Tutorial);
                return;
            }

            var utc = DateTime.UtcNow;
            int today = ModeController.DayNumber(utc);
            var todayRecord = stats.GetOrCreateDay(today);

            if (dailyLabel != null)
            {
                dailyLabel.text = todayRecord.completed
                    ? string.Format(Loc.Tr("Daily {0} — done in {1}"), Loc.ShortDate(utc), todayRecord.bestStrokes)
                    : string.Format(Loc.Tr("Play today's hole · {0}"), Loc.ShortDate(utc));
            }

            // Today's hole is done — the loop's next beat is the countdown.
            _showCountdown = todayRecord.completed;
            countdownText?.gameObject.SetActive(_showCountdown);

            if (tutorialLabel != null && firstLaunch)
            {
                tutorialLabel.text = Loc.Tr("Tutorial  ·  start here");
            }

            if (difficultyLabel != null)
            {
                difficultyLabel.text = Loc.Tr(GameSession.PracticeDifficulty.ToString());
                difficultyLabel.color = DifficultyColor(GameSession.PracticeDifficulty);
            }

            if (journeyLabel != null)
            {
                int done = stats.Data.journeyStars.Count;
                journeyLabel.text = done > 0
                    ? string.Format(Loc.Tr("Journey · {0}/{1}"), done, JourneyConfig.Seeds.Length)
                    : Loc.Tr("Journey");
            }

            if (footerText != null)
            {
                footerText.text = BuildStatsLine(stats, todayRecord);
            }

            dailyButton?.onClick.AddListener(() => Launch(GameMode.Daily));
            practiceButton?.onClick.AddListener(() => Launch(GameMode.Practice));
            difficultyButton?.onClick.AddListener(CycleDifficulty);
            tutorialButton?.onClick.AddListener(() => Launch(GameMode.Tutorial));

            journeyButton?.onClick.AddListener(OpenJourney);
            journeyCloseButton?.onClick.AddListener(() => journeyPanel?.SetActive(false));
            journeyPrevButton?.onClick.AddListener(() =>
            {
                _journeyPage = Mathf.Max(0, _journeyPage - 1);
                RefreshJourney();
            });
            journeyNextButton?.onClick.AddListener(() =>
            {
                _journeyPage = Mathf.Min(JourneyPageCount - 1, _journeyPage + 1);
                RefreshJourney();
            });
            for (int i = 0; i < journeyCellButtons.Length; i++)
            {
                int cell = i; // capture per cell
                journeyCellButtons[i]?.onClick.AddListener(() => LaunchJourneyCell(cell));
            }

            archiveButton?.onClick.AddListener(OpenArchive);
            archiveRandomButton?.onClick.AddListener(PlayRandomUnplayedDay);
            archiveCloseButton?.onClick.AddListener(() => archivePanel?.SetActive(false));
            archivePrevMonthButton?.onClick.AddListener(() => StepArchiveMonth(-1));
            archiveNextMonthButton?.onClick.AddListener(() => StepArchiveMonth(1));
            for (int i = 0; i < archiveCellButtons.Length; i++)
            {
                int cell = i; // capture per cell, not the loop variable
                archiveCellButtons[i]?.onClick.AddListener(() => LaunchArchiveCell(cell));
            }

            StartCoroutine(EmblemIdle());

            statsButton?.onClick.AddListener(OpenStats);
            statsCloseButton?.onClick.AddListener(() => statsPanel?.SetActive(false));
            shareBestButton?.onClick.AddListener(ShareTodaysBest);

            RefreshSettings();
            WireToggle(soundToggle, selected =>
            {
                stats.SetSoundEnabled(selected);
                UiSounds.Enabled = selected;
            });
            WireToggle(hapticsToggle, selected => stats.SetHapticsEnabled(selected));
            WireToggle(aimToggle, selected => stats.SetAimDirect(!selected)); // A = Sling
            WireToggle(colorblindToggle, selected =>
            {
                stats.SetColorblindMode(!selected); // A = Std
                PaletteMaterials.ColorblindMode = stats.Data.colorblindMode;
            });
            WireToggle(batteryToggle, selected =>
            {
                stats.SetBatterySaver(!selected); // A = 120 fps
                Application.targetFrameRate = stats.Data.batterySaver ? 60 : 120;
            });
            WireToggle(languageToggle, selected =>
            {
                stats.SetLanguage(selected ? "en" : "tr"); // A = EN
                Loc.Apply(stats.Data.language);
                SceneFader.LoadScene("Menu"); // every baked label re-localizes
            });
            settingsButton?.onClick.AddListener(() =>
            {
                if (settingsPanel != null)
                {
                    UiFx.PopIn(this, settingsPanel);
                }
            });
            settingsCloseButton?.onClick.AddListener(() => settingsPanel?.SetActive(false));
            collectionButton?.onClick.AddListener(OpenCollection);
            collectionCloseButton?.onClick.AddListener(() => collectionPanel?.SetActive(false));
            for (int i = 0; i < collectionCellButtons.Length; i++)
            {
                int cell = i; // capture per cell
                collectionCellButtons[i]?.onClick.AddListener(() => OnCollectionCell(cell));
            }

            if (collectionTab != null)
            {
                collectionTab.optionAButton?.onClick.AddListener(() => ShowCollectionTab(false));
                collectionTab.optionBButton?.onClick.AddListener(() => ShowCollectionTab(true));
            }
            exportSaveButton?.onClick.AddListener(ExportSave);
            importSaveButton?.onClick.AddListener(ImportSave);
        }

        private void ExportSave()
        {
            GUIUtility.systemCopyBuffer = SaveCodec.Export(_stats.Data);
            if (exportSaveLabel != null)
            {
                exportSaveLabel.text = Loc.Tr("Copied!");
            }
        }

        private void ImportSave()
        {
            string text = saveField != null ? saveField.text.Trim() : "";
            if (!SaveCodec.TryImport(text, out var imported))
            {
                if (importSaveLabel != null)
                {
                    importSaveLabel.text = Loc.Tr("Invalid code");
                    _importArmed = false;
                }

                return;
            }

            // Importing OVERWRITES this device's save — ask for a second tap.
            if (!_importArmed)
            {
                _importArmed = true;
                if (importSaveLabel != null)
                {
                    importSaveLabel.text = Loc.Tr("Tap to confirm");
                }

                return;
            }

            _stats.ReplaceData(imported);
            SceneFader.LoadScene("Menu"); // rebuild every label from the new save
        }

        /// <summary>Wires both segments: A selects true, B selects false.</summary>
        private void WireToggle(SegmentedToggle? toggle, System.Action<bool> selectA)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.optionAButton?.onClick.AddListener(() =>
            {
                selectA(true);
                RefreshSettings();
            });
            toggle.optionBButton?.onClick.AddListener(() =>
            {
                selectA(false);
                RefreshSettings();
            });
        }

        private void RefreshSettings()
        {
            var data = _stats.Data;
            soundToggle?.SetSelected(data.soundEnabled);
            hapticsToggle?.SetSelected(data.hapticsEnabled);
            aimToggle?.SetSelected(!data.aimDirect);        // A = Sling
            colorblindToggle?.SetSelected(!data.colorblindMode); // A = Std
            batteryToggle?.SetSelected(!data.batterySaver); // A = 120 fps
            languageToggle?.SetSelected(Loc.Current == Loc.Language.English); // A = EN
        }

        private void Update()
        {
            // Gentle life: the deco circles drift, the tagline breathes.
            float time = Time.time;
            if (deco1 != null)
            {
                deco1.anchoredPosition = new Vector2(
                    Mathf.Sin(time * 0.11f) * 16f, Mathf.Cos(time * 0.08f) * 11f);
            }

            if (deco2 != null)
            {
                deco2.anchoredPosition = new Vector2(
                    Mathf.Cos(time * 0.09f) * 13f, Mathf.Sin(time * 0.12f) * 15f);
            }

            if (taglineText != null)
            {
                var c = taglineText.color;
                taglineText.color = new Color(c.r, c.g, c.b, 0.5f + 0.14f * Mathf.Sin(time * 0.9f));
            }

            // Android back button (Escape): close an open panel, else quit.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (archivePanel != null && archivePanel.activeSelf)
                {
                    UiSounds.ClickDown();
                    archivePanel.SetActive(false);
                }
                else if (statsPanel != null && statsPanel.activeSelf)
                {
                    UiSounds.ClickDown();
                    statsPanel.SetActive(false);
                }
                else if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    UiSounds.ClickDown();
                    settingsPanel.SetActive(false);
                }
                else if (collectionPanel != null && collectionPanel.activeSelf)
                {
                    UiSounds.ClickDown();
                    collectionPanel.SetActive(false);
                }
                else if (journeyPanel != null && journeyPanel.activeSelf)
                {
                    UiSounds.ClickDown();
                    journeyPanel.SetActive(false);
                }
                else
                {
                    Application.Quit();
                }
            }

            if (!_showCountdown || countdownText == null)
            {
                return;
            }

            var remaining = DailyCountdown.UntilNextHole(DateTime.UtcNow);
            countdownText.text = remaining.TotalSeconds <= 0
                ? Loc.Tr("New hole is ready — restart to play!")
                : string.Format(Loc.Tr("next hole in {0}"), DailyCountdown.Format(remaining));
        }

        /// <summary>
        /// Menu idle: every few seconds the emblem ball rolls into the emblem
        /// cup, sinks, and pops back at the tee — the menu never sits frozen.
        /// Anchor fractions mirror the emblem layout in UiConstruction.
        /// </summary>
        private System.Collections.IEnumerator EmblemIdle()
        {
            if (emblemBall == null)
            {
                yield break;
            }

            var start = emblemBall.anchoredPosition;
            while (true)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(5f, 9f));
                var parentRect = (RectTransform)emblemBall.parent;
                var target = start + new Vector2(
                    (0.5f - 0.425f) * parentRect.rect.width,
                    (0.87f - 0.8765f) * parentRect.rect.height);

                for (float t = 0f; t < 0.8f; t += Time.deltaTime)
                {
                    float k = t / 0.8f;
                    emblemBall.anchoredPosition = Vector2.Lerp(start, target, k * k);
                    yield return null;
                }

                for (float t = 0f; t < 0.18f; t += Time.deltaTime)
                {
                    emblemBall.localScale = Vector3.one * (1f - t / 0.18f);
                    yield return null;
                }

                emblemBall.localScale = Vector3.zero;
                yield return new WaitForSeconds(0.7f);

                emblemBall.anchoredPosition = start;
                for (float t = 0f; t < 0.2f; t += Time.deltaTime)
                {
                    emblemBall.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t / 0.2f);
                    yield return null;
                }

                emblemBall.localScale = Vector3.one;
            }
        }

        private void OpenStats()
        {
            var data = _stats.Data;
            int attempts = 0;
            for (int i = 0; i < data.days.Count; i++)
            {
                attempts += data.days[i].attempts;
            }

            if (statsBlock != null)
            {
                // "E 3 · N 2 · H —" told nobody anything. Every number now
                // arrives with the word for what it counts, and the star
                // tally is gone: the stroke histogram beside it says the same
                // thing, since par is what turns strokes into stars.
                string Pb(int best) => best == 0 ? Loc.Tr("not yet") : best.ToString();
                statsBlock.text =
                    string.Format(Loc.Tr("Streak {0}  (best {1})"), data.streak, data.bestStreak) + "\n"
                    + string.Format(Loc.Tr("Dailies finished  {0}"), Achievements.CompletedDailyCount(data)) + "\n"
                    + string.Format(Loc.Tr("Daily attempts  {0}"), attempts) + "\n"
                    + string.Format(Loc.Tr("Practice rounds  {0}"), data.practicePlayed) + "\n\n"
                    + Loc.Tr("Fewest strokes in practice") + "\n"
                    + string.Format(Loc.Tr("  Easy {0}   ·   Normal {1}   ·   Hard {2}"),
                        Pb(data.bestPracticeEasy), Pb(data.bestPracticeNormal), Pb(data.bestPracticeHard));
            }

            if (achievementsBlock != null)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var def in Achievements.All)
                {
                    bool unlocked = data.achievements.Contains(def.Id);
                    sb.AppendLine(unlocked
                        ? $"<color=#F8F4E6>{def.Title}</color> <color=#F8F4E699>— {def.Detail}</color>"
                        : $"<color=#F8F4E64D>{def.Title} — {def.Detail}</color>");
                }

                achievementsBlock.text = sb.ToString();
            }

            // Stroke distribution: bullet bars over the completed days' bests.
            if (histogramBlock != null)
            {
                var buckets = new int[6]; // 1..5 strokes, then 6+
                foreach (var day in data.days)
                {
                    if (day.completed)
                    {
                        buckets[Mathf.Clamp(day.bestStrokes, 1, 6) - 1]++;
                    }
                }

                int max = 1;
                foreach (int b in buckets)
                {
                    max = Mathf.Max(max, b);
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine(Loc.Tr("Strokes taken")).AppendLine();
                for (int i = 0; i < buckets.Length; i++)
                {
                    string bucketLabel = i < 5 ? (i + 1).ToString() : "6+";
                    int length = buckets[i] == 0
                        ? 0
                        : Mathf.Max(1, Mathf.RoundToInt(buckets[i] * 8f / max));
                    sb.Append(bucketLabel.PadLeft(2)).Append(' ')
                        .Append("<color=#FCC24A>").Append(new string('•', length)).Append("</color>");
                    if (buckets[i] > 0)
                    {
                        sb.Append(' ').Append(buckets[i]);
                    }

                    sb.AppendLine();
                }

                histogramBlock.text = sb.ToString();
            }

            // Share is only offered once today's best actually exists.
            var todayBest = _stats.FindDay(ModeController.DayNumber(DateTime.UtcNow));
            shareBestButton?.gameObject.SetActive(
                todayBest != null && todayBest.completed && todayBest.bestReplay.Length > 0);
            if (shareBestLabel != null)
            {
                shareBestLabel.text = Loc.Tr("Share best");
            }

            if (exportSaveLabel != null)
            {
                exportSaveLabel.text = Loc.Tr("Export save");
            }

            if (importSaveLabel != null)
            {
                importSaveLabel.text = Loc.Tr("Import");
            }

            _importArmed = false;

            if (statsPanel != null)
            {
                UiFx.PopIn(this, statsPanel);
            }
        }

        private void ShareTodaysBest()
        {
            int today = ModeController.DayNumber(DateTime.UtcNow);
            var record = _stats.FindDay(today);
            if (record == null || !record.completed || record.bestReplay.Length == 0)
            {
                return;
            }

            // Share payloads stay English on purpose — they travel to anyone.
            string text = $"PUTTSEED day {today} — {record.bestStrokes} strokes. Watch: {record.bestReplay}";
            GUIUtility.systemCopyBuffer = text;
            bool sheet = NativeShare.Share(text);
            if (shareBestLabel != null)
            {
                shareBestLabel.text = Loc.Tr(sheet ? "Sharing…" : "Copied!");
            }
        }

        private void OpenCollection()
        {
            ShowCollectionTab(false);
            if (collectionPanel != null)
            {
                UiFx.PopIn(this, collectionPanel);
            }
        }

        /// <summary>Switches the shared grid between ball skins and trails.</summary>
        private void ShowCollectionTab(bool trails)
        {
            _collectionShowsTrails = trails;
            collectionTab?.SetSelected(!trails);
            RefreshCollection();
            SetCollectionHint(string.Format(Loc.Tr("{0}  —  equipped"),
                Loc.Tr(trails
                    ? BallTrails.Resolve(_stats.Data.ballTrail).Name
                    : BallSkins.Resolve(_stats.Data.ballSkin).Name)));
        }

        /// <summary>How many cells the active tab fills.</summary>
        private int CollectionCount =>
            _collectionShowsTrails ? BallTrails.All.Length : BallSkins.All.Length;

        private void RefreshCollection()
        {
            var data = _stats.Data;
            for (int i = 0; i < collectionCellButtons.Length; i++)
            {
                // The grid is sized for the LARGER catalog, so the shorter tab
                // hides its spare cells rather than showing dead squares.
                bool exists = i < CollectionCount;
                collectionCellButtons[i]?.gameObject.SetActive(exists);
                if (!exists)
                {
                    continue;
                }

                string name;
                Color color;
                bool unlocked;
                bool equipped;
                if (_collectionShowsTrails)
                {
                    var trail = BallTrails.All[i];
                    name = trail.Name;
                    color = trail.Color;
                    unlocked = BallTrails.IsUnlocked(trail, data);
                    equipped = data.ballTrail == trail.Id;
                }
                else
                {
                    var skin = BallSkins.All[i];
                    name = skin.Name;
                    color = skin.Color;
                    unlocked = BallSkins.IsUnlocked(skin, data);
                    equipped = data.ballSkin == skin.Id;
                }

                if (collectionCellSwatches[i] != null)
                {
                    // Trail tints carry their own alpha; the swatch shows the
                    // color at full strength so a locked cell is the only dim one.
                    collectionCellSwatches[i].color = unlocked
                        ? new Color(color.r, color.g, color.b, 1f)
                        : new Color(color.r, color.g, color.b, 0.22f);
                }

                if (collectionCellButtons[i] != null)
                {
                    collectionCellButtons[i].image.color = !unlocked ? LockedCell
                        : equipped ? EquippedCell
                        : UIStyle.PanelDark;
                }

                if (collectionCellRings[i] != null)
                {
                    collectionCellRings[i].gameObject.SetActive(equipped);
                }

                if (collectionCellLabels[i] != null)
                {
                    collectionCellLabels[i].text = Loc.Tr(name);
                    collectionCellLabels[i].color = equipped
                        ? UIStyle.Accent
                        : unlocked ? UIStyle.Cream : UIStyle.CreamDim;
                }
            }
        }

        private void SetCollectionHint(string text)
        {
            if (collectionHintText != null)
            {
                collectionHintText.text = text;
            }
        }

        /// <summary>
        /// A collection cell tap: equips an unlocked skin; on a locked one the
        /// hint line explains what still stands in the way.
        /// </summary>
        private void OnCollectionCell(int cell)
        {
            if (cell >= CollectionCount)
            {
                return;
            }

            string name;
            bool unlocked;
            string hint;
            if (_collectionShowsTrails)
            {
                var trail = BallTrails.All[cell];
                name = Loc.Tr(trail.Name);
                unlocked = BallTrails.IsUnlocked(trail, _stats.Data);
                hint = BallTrails.UnlockHint(trail);
                if (unlocked)
                {
                    _stats.SetBallTrail(trail.Id);
                }
            }
            else
            {
                var skin = BallSkins.All[cell];
                name = Loc.Tr(skin.Name);
                unlocked = BallSkins.IsUnlocked(skin, _stats.Data);
                hint = BallSkins.UnlockHint(skin);
                if (unlocked)
                {
                    _stats.SetBallSkin(skin.Id);
                }
            }

            if (unlocked)
            {
                RefreshCollection();
                SetCollectionHint(string.Format(Loc.Tr("{0}  —  equipped"), name));
            }
            else
            {
                SetCollectionHint(string.Format(Loc.Tr("{0}  —  locked: {1}"), name, hint));
            }
        }

        /// <summary>Levels shown per journey page (a 5x5 grid).</summary>
        private const int JourneyPageSize = 25;

        private static int JourneyPageCount
            => (JourneyConfig.Seeds.Length + JourneyPageSize - 1) / JourneyPageSize;

        private void OpenJourney()
        {
            // Land on the page holding the next level to play.
            int frontier = _stats.UnlockedJourneyLevels(JourneyConfig.Seeds.Length) - 1;
            _journeyPage = Mathf.Clamp(frontier / JourneyPageSize, 0, JourneyPageCount - 1);
            RefreshJourney();
            if (journeyPanel != null)
            {
                UiFx.PopIn(this, journeyPanel);
            }
        }

        private void RefreshJourney()
        {
            var stars = _stats.Data.journeyStars;
            int unlocked = _stats.UnlockedJourneyLevels(JourneyConfig.Seeds.Length);
            for (int i = 0; i < journeyCellButtons.Length; i++)
            {
                int level = _journeyPage * JourneyPageSize + i;
                bool exists = level < JourneyConfig.Seeds.Length;
                journeyCellButtons[i]?.gameObject.SetActive(exists);
                if (!exists)
                {
                    continue;
                }

                bool isUnlocked = level < unlocked;
                int earned = level < stars.Count ? stars[level] : 0;
                if (journeyCellLabels[i] != null)
                {
                    journeyCellLabels[i].text = (level + 1).ToString();
                    journeyCellLabels[i].color = isUnlocked ? UIStyle.Cream : new Color(1f, 1f, 1f, 0.25f);
                }

                if (journeyCellButtons[i] != null)
                {
                    journeyCellButtons[i].interactable = isUnlocked;
                }

                for (int s = 0; s < 3; s++)
                {
                    var star = journeyCellStars[i * 3 + s];
                    if (star == null)
                    {
                        continue;
                    }

                    star.gameObject.SetActive(isUnlocked);
                    if (isUnlocked)
                    {
                        star.color = s < earned ? UIStyle.Accent : new Color(1f, 1f, 1f, 0.14f);
                    }
                }
            }

            if (journeyPageLabel != null)
            {
                int total = 0;
                foreach (int s in stars)
                {
                    total += s;
                }

                int first = _journeyPage * JourneyPageSize + 1;
                int last = Mathf.Min(first + JourneyPageSize - 1, JourneyConfig.Seeds.Length);
                journeyPageLabel.text = $"{first}–{last}  ·  "
                    + string.Format(Loc.Tr("{0} stars"), total);
            }

            if (journeyPrevButton != null)
            {
                journeyPrevButton.interactable = _journeyPage > 0;
            }

            if (journeyNextButton != null)
            {
                journeyNextButton.interactable = _journeyPage < JourneyPageCount - 1;
            }
        }

        private void LaunchJourneyCell(int cell)
        {
            int level = _journeyPage * JourneyPageSize + cell;
            if (level >= _stats.UnlockedJourneyLevels(JourneyConfig.Seeds.Length))
            {
                return;
            }

            GameSession.Mode = GameMode.Journey;
            GameSession.JourneyLevel = level;
            GameSession.ArchiveDayNumber = -1;
            GameSession.UseFixedSeed = false;
            SceneFader.LoadScene("Game");
        }

        private void OpenArchive()
        {
            var utc = DateTime.UtcNow.Date;
            _archiveMonth = new DateTime(utc.Year, utc.Month, 1);
            RefreshArchive();
            if (archivePanel != null)
            {
                UiFx.PopIn(this, archivePanel);
            }
        }

        /// <summary>The first month the archive can show (the daily epoch).</summary>
        private static DateTime EpochMonth => new DateTime(2020, 1, 1);

        private void StepArchiveMonth(int delta)
        {
            var target = _archiveMonth.AddMonths(delta);
            var utc = DateTime.UtcNow.Date;
            var newest = new DateTime(utc.Year, utc.Month, 1);
            if (target < EpochMonth || target > newest)
            {
                return; // no months exist outside the daily's lifetime
            }

            _archiveMonth = target;
            RefreshArchive();
        }

        /// <summary>The day number a calendar cell stands for (-1 outside the month).</summary>
        private int CellDayNumber(int cell)
        {
            int lead = ((int)_archiveMonth.DayOfWeek - (int)Loc.FirstDayOfWeek + 7) % 7;
            int dayOfMonth = cell - lead + 1;
            int daysInMonth = DateTime.DaysInMonth(_archiveMonth.Year, _archiveMonth.Month);
            if (dayOfMonth < 1 || dayOfMonth > daysInMonth)
            {
                return -1;
            }

            return ModeController.DayNumber(_archiveMonth.AddDays(dayOfMonth - 1));
        }

        private void RefreshArchive()
        {
            if (archiveMonthLabel != null)
            {
                archiveMonthLabel.text = Loc.MonthLabel(_archiveMonth);
            }

            var initials = Loc.WeekdayInitials();
            for (int i = 0; i < archiveWeekdayLabels.Length && i < initials.Length; i++)
            {
                if (archiveWeekdayLabels[i] != null)
                {
                    archiveWeekdayLabels[i].text = initials[i];
                }
            }

            int today = ModeController.DayNumber(DateTime.UtcNow);
            int daysInMonth = DateTime.DaysInMonth(_archiveMonth.Year, _archiveMonth.Month);
            int lead = ((int)_archiveMonth.DayOfWeek - (int)Loc.FirstDayOfWeek + 7) % 7;

            for (int i = 0; i < archiveCellButtons.Length; i++)
            {
                int dayOfMonth = i - lead + 1;
                bool inMonth = dayOfMonth >= 1 && dayOfMonth <= daysInMonth;
                archiveCellButtons[i]?.gameObject.SetActive(inMonth);
                if (!inMonth)
                {
                    continue;
                }

                int day = CellDayNumber(i);
                bool playable = day >= 1 && day <= today;
                bool isToday = day == today;
                var record = _stats.FindDay(day);
                bool played = record != null && record.completed;

                if (archiveCellLabels[i] != null)
                {
                    archiveCellLabels[i].text = dayOfMonth.ToString();
                    archiveCellLabels[i].color = isToday ? UIStyle.AccentInk
                        : playable ? UIStyle.Cream
                        : UIStyle.CreamDim;
                }

                if (archiveCellButtons[i] != null)
                {
                    archiveCellButtons[i].interactable = playable;

                    // Today shouts, a finished day sits lit, an unplayed one
                    // waits at the normal tone, and the future is nearly gone.
                    archiveCellButtons[i].image.color = isToday ? UIStyle.Accent
                        : played ? PlayedDayCell
                        : playable ? UIStyle.PanelDark
                        : FutureDayCell;
                }

                for (int st = 0; st < 3; st++)
                {
                    var star = archiveCellStars[i * 3 + st];
                    if (star == null)
                    {
                        continue;
                    }

                    star.gameObject.SetActive(played);
                    if (played)
                    {
                        star.color = st < record!.bestStars
                            ? (isToday ? UIStyle.AccentInk : UIStyle.Accent)
                            : new Color(1f, 1f, 1f, 0.16f);
                    }
                }
            }

            if (archivePrevMonthButton != null)
            {
                archivePrevMonthButton.interactable = _archiveMonth > EpochMonth;
            }

            if (archiveNextMonthButton != null)
            {
                var utc = DateTime.UtcNow.Date;
                archiveNextMonthButton.interactable =
                    _archiveMonth < new DateTime(utc.Year, utc.Month, 1);
            }
        }

        private void LaunchArchiveCell(int cell)
        {
            int day = CellDayNumber(cell);
            int today = ModeController.DayNumber(DateTime.UtcNow);
            if (day < 1 || day > today)
            {
                return;
            }

            // Today from the calendar is still today: it must run as the daily
            // so the streak and the day's record are earned normally.
            GameSession.ArchiveDayNumber = day == today ? -1 : day;
            GameSession.Mode = GameMode.Daily;
            GameSession.UseFixedSeed = false;
            SceneFader.LoadScene("Game");
        }

        private void PlayRandomUnplayedDay()
        {
            int today = ModeController.DayNumber(DateTime.UtcNow);
            if (today <= 1)
            {
                return; // no past days exist yet
            }

            int day = UnityEngine.Random.Range(1, today);
            for (int tries = 0; tries < 40; tries++)
            {
                int candidate = UnityEngine.Random.Range(1, today);
                var record = _stats.FindDay(candidate);
                if (record == null || !record.completed)
                {
                    day = candidate;
                    break;
                }
            }

            GameSession.Mode = GameMode.Daily;
            GameSession.ArchiveDayNumber = day;
            GameSession.UseFixedSeed = false;
            SceneFader.LoadScene("Game");
        }

        private static string BuildStatsLine(StatsStore stats, DayRecord today)
        {
            string streak = stats.Data.streak > 0
                ? string.Format(Loc.Tr("Streak {0}"), stats.Data.streak)
                : Loc.Tr("No streak yet");
            string attempts = today.attempts > 0
                ? string.Format(Loc.Tr(" · Today: {0} attempt(s)"), today.attempts)
                : "";
            string practice = stats.Data.practicePlayed > 0
                ? string.Format(Loc.Tr(" · Practice: {0}"), stats.Data.practicePlayed)
                : "";
            return streak + attempts + practice;
        }

        private void CycleDifficulty()
        {
            GameSession.PracticeDifficulty = (Difficulty)(((int)GameSession.PracticeDifficulty + 1) % 3);
            if (difficultyLabel != null)
            {
                difficultyLabel.text = Loc.Tr(GameSession.PracticeDifficulty.ToString());
                difficultyLabel.color = DifficultyColor(GameSession.PracticeDifficulty);
            }
        }

        private static void Launch(GameMode mode)
        {
            GameSession.Mode = mode;
            GameSession.TutorialIndex = 0;
            GameSession.ArchiveDayNumber = -1;
            GameSession.UseFixedSeed = false;
            SceneFader.LoadScene("Game");
        }

        /// <summary>The shared stats file path (same file the game scene writes).</summary>
        public static string StatsPath()
            => Path.Combine(Application.persistentDataPath, "puttseed-stats.json");
    }
}
