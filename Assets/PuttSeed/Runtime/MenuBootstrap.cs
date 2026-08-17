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
        public Button[] archiveRowButtons = new Button[0];
        public Text[] archiveRowLabels = new Text[0];
        public Button? archiveOlderButton;
        public Button? archiveNewerButton;
        public Button? archiveCloseButton;
        public Text? archivePageLabel;
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
        public Image[] archiveRowStars = new Image[0];
        public Button? archiveRandomButton;
        public Text? histogramBlock;
        public Button? settingsButton;
        public GameObject? settingsPanel;
        public Button? settingsCloseButton;
        public Button? collectionButton;
        public GameObject? collectionPanel;
        public Button? collectionCloseButton;
        public Button[] collectionRowButtons = new Button[0];
        public Text[] collectionRowLabels = new Text[0];
        public Image[] collectionRowSwatches = new Image[0];
        public InputField? saveField;
        public Button? importSaveButton;
        public Text? importSaveLabel;
        public Button? exportSaveButton;
        public Text? exportSaveLabel;

        private bool _importArmed;

        private bool _showCountdown;
        private StatsStore _stats = null!;
        private int _archivePage;

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
                    ? string.Format(Loc.Tr("Daily {0} — done in {1}"), $"{utc:MMM d}", todayRecord.bestStrokes)
                    : string.Format(Loc.Tr("Play today's hole · {0}"), $"{utc:MMM d}");
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

            if (footerText != null)
            {
                footerText.text = BuildStatsLine(stats, todayRecord);
            }

            dailyButton?.onClick.AddListener(() => Launch(GameMode.Daily));
            practiceButton?.onClick.AddListener(() => Launch(GameMode.Practice));
            difficultyButton?.onClick.AddListener(CycleDifficulty);
            tutorialButton?.onClick.AddListener(() => Launch(GameMode.Tutorial));

            archiveButton?.onClick.AddListener(OpenArchive);
            archiveRandomButton?.onClick.AddListener(PlayRandomUnplayedDay);
            archiveCloseButton?.onClick.AddListener(() => archivePanel?.SetActive(false));
            archiveOlderButton?.onClick.AddListener(() => { _archivePage++; RefreshArchive(); });
            archiveNewerButton?.onClick.AddListener(() =>
            {
                _archivePage = Mathf.Max(0, _archivePage - 1);
                RefreshArchive();
            });
            for (int i = 0; i < archiveRowButtons.Length; i++)
            {
                int row = i; // capture per row, not the loop variable
                archiveRowButtons[i]?.onClick.AddListener(() => LaunchArchiveRow(row));
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
            for (int i = 0; i < collectionRowButtons.Length; i++)
            {
                int row = i; // capture per row
                collectionRowButtons[i]?.onClick.AddListener(() => EquipSkin(row));
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
            int s3 = 0, s2 = 0, s1 = 0, attempts = 0;
            for (int i = 0; i < data.days.Count; i++)
            {
                var day = data.days[i];
                attempts += day.attempts;
                if (day.completed)
                {
                    if (day.bestStars >= 3) { s3++; }
                    else if (day.bestStars == 2) { s2++; }
                    else { s1++; }
                }
            }

            if (statsBlock != null)
            {
                string Pb(int best) => best == 0 ? "—" : best.ToString();
                statsBlock.text =
                    string.Format(Loc.Tr("Streak {0}  (best {1})"), data.streak, data.bestStreak) + "\n"
                    + string.Format(Loc.Tr("Dailies completed  {0}"), Achievements.CompletedDailyCount(data)) + "\n"
                    + string.Format(Loc.Tr("3-star {0}  ·  2-star {1}  ·  1-star {2}"), s3, s2, s1) + "\n"
                    + string.Format(Loc.Tr("Daily attempts  {0}  ·  Practice  {1}"), attempts, data.practicePlayed) + "\n"
                    + string.Format(Loc.Tr("Practice best   E {0}  ·  N {1}  ·  H {2}"),
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
            RefreshCollection();
            if (collectionPanel != null)
            {
                UiFx.PopIn(this, collectionPanel);
            }
        }

        private void RefreshCollection()
        {
            var data = _stats.Data;
            for (int i = 0; i < collectionRowLabels.Length && i < BallSkins.All.Length; i++)
            {
                var skin = BallSkins.All[i];
                bool unlocked = BallSkins.IsUnlocked(skin, data);
                bool equipped = data.ballSkin == skin.Id;

                if (collectionRowSwatches[i] != null)
                {
                    collectionRowSwatches[i].color = unlocked
                        ? skin.Color
                        : new Color(skin.Color.r, skin.Color.g, skin.Color.b, 0.22f);
                }

                if (collectionRowLabels[i] != null)
                {
                    string hint = skin.RequiredAchievement != null
                        ? Loc.Tr(Achievements.Find(skin.RequiredAchievement)?.Detail ?? "")
                        : "";
                    string name = Loc.Tr(skin.Name);
                    collectionRowLabels[i].text = equipped
                        ? string.Format(Loc.Tr("{0}  —  equipped"), name)
                        : unlocked
                            ? string.Format(Loc.Tr("{0}  —  tap to equip"), name)
                            : string.Format(Loc.Tr("{0}  —  locked: {1}"), name, hint);
                    collectionRowLabels[i].color = equipped
                        ? UIStyle.Accent
                        : unlocked ? UIStyle.Cream : UIStyle.CreamDim;
                }

                if (collectionRowButtons[i] != null)
                {
                    collectionRowButtons[i].interactable = unlocked;
                }
            }
        }

        private void EquipSkin(int row)
        {
            if (row >= BallSkins.All.Length)
            {
                return;
            }

            var skin = BallSkins.All[row];
            if (BallSkins.IsUnlocked(skin, _stats.Data))
            {
                _stats.SetBallSkin(skin.Id);
                RefreshCollection();
            }
        }

        private void OpenArchive()
        {
            _archivePage = 0;
            RefreshArchive();
            if (archivePanel != null)
            {
                UiFx.PopIn(this, archivePanel);
            }
        }

        /// <summary>Day number shown on a row: yesterday backward, 7 per page.</summary>
        private int RowDayNumber(int row)
            => ModeController.DayNumber(DateTime.UtcNow) - 1 - _archivePage * 7 - row;

        private void RefreshArchive()
        {
            for (int i = 0; i < archiveRowLabels.Length; i++)
            {
                int day = RowDayNumber(i);
                bool valid = day >= 1;
                archiveRowButtons[i]?.gameObject.SetActive(valid);
                if (!valid || archiveRowLabels[i] == null)
                {
                    continue;
                }

                var date = ModeController.DateOfDay(day);
                var record = _stats.FindDay(day);
                bool played = record != null && record.completed;
                archiveRowLabels[i].text = played
                    ? string.Format(Loc.Tr("{0}  ·  best {1}"), $"{date:MMM d}", record!.bestStrokes)
                    : string.Format(Loc.Tr("{0}  ·  not played"), $"{date:MMM d}");
                archiveRowLabels[i].color = played ? UIStyle.Cream : UIStyle.CreamDim;

                // Star icons on the row's right: earned amber, the rest dim.
                for (int s = 0; s < 3; s++)
                {
                    var star = archiveRowStars[i * 3 + s];
                    if (star == null)
                    {
                        continue;
                    }

                    star.gameObject.SetActive(played);
                    if (played)
                    {
                        star.color = s < record!.bestStars
                            ? UIStyle.Accent
                            : new Color(1f, 1f, 1f, 0.14f);
                    }
                }
            }

            if (archivePageLabel != null)
            {
                int first = _archivePage * 7 + 1;
                archivePageLabel.text = string.Format(Loc.Tr("{0}–{1} days ago"), first, first + 6);
            }

            if (archiveOlderButton != null)
            {
                archiveOlderButton.interactable = RowDayNumber(7) >= 1;
            }

            if (archiveNewerButton != null)
            {
                archiveNewerButton.interactable = _archivePage > 0;
            }
        }

        /// <summary>
        /// Jumps into a random past day the player has not completed yet —
        /// sampled, with a fair fallback when history is (nearly) exhausted.
        /// </summary>
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

        private void LaunchArchiveRow(int row)
        {
            int day = RowDayNumber(row);
            if (day < 1)
            {
                return;
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
