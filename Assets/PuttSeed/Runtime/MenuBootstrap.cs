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
        public Button? soundButton;
        public Text? soundLabel;
        public Button? hapticsButton;
        public Text? hapticsLabel;
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
            Ambient.EnsurePlaying();
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
                    ? $"Daily {utc:MMM d} — done in {todayRecord.bestStrokes}"
                    : $"Play today's hole · {utc:MMM d}";
            }

            // Today's hole is done — the loop's next beat is the countdown.
            _showCountdown = todayRecord.completed;
            countdownText?.gameObject.SetActive(_showCountdown);

            if (tutorialLabel != null && firstLaunch)
            {
                tutorialLabel.text = "Tutorial  ·  start here";
            }

            if (difficultyLabel != null)
            {
                difficultyLabel.text = GameSession.PracticeDifficulty.ToString();
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

            RefreshSettingsLabels(stats);
            soundButton?.onClick.AddListener(() =>
            {
                stats.SetSoundEnabled(!stats.Data.soundEnabled);
                UiSounds.Enabled = stats.Data.soundEnabled;
                RefreshSettingsLabels(stats);
            });
            hapticsButton?.onClick.AddListener(() =>
            {
                stats.SetHapticsEnabled(!stats.Data.hapticsEnabled);
                RefreshSettingsLabels(stats);
            });
        }

        private void RefreshSettingsLabels(StatsStore stats)
        {
            if (soundLabel != null)
            {
                soundLabel.text = stats.Data.soundEnabled ? "Sound: On" : "Sound: Off";
                soundLabel.color = stats.Data.soundEnabled ? UIStyle.Cream : UIStyle.CreamDim;
            }

            if (hapticsLabel != null)
            {
                hapticsLabel.text = stats.Data.hapticsEnabled ? "Haptics: On" : "Haptics: Off";
                hapticsLabel.color = stats.Data.hapticsEnabled ? UIStyle.Cream : UIStyle.CreamDim;
            }
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
                ? "New hole is ready — restart to play!"
                : $"next hole in {DailyCountdown.Format(remaining)}";
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
                statsBlock.text =
                    $"Streak {data.streak}  (best {data.bestStreak})\n" +
                    $"Dailies completed  {Achievements.CompletedDailyCount(data)}\n" +
                    $"3-star {s3}  ·  2-star {s2}  ·  1-star {s1}\n" +
                    $"Daily attempts  {attempts}\n" +
                    $"Practice courses  {data.practicePlayed}";
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

            // Share is only offered once today's best actually exists.
            var todayBest = _stats.FindDay(ModeController.DayNumber(DateTime.UtcNow));
            shareBestButton?.gameObject.SetActive(
                todayBest != null && todayBest.completed && todayBest.bestReplay.Length > 0);
            if (shareBestLabel != null)
            {
                shareBestLabel.text = "Share today's best";
            }

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

            string text = $"PUTTSEED day {today} — {record.bestStrokes} strokes. Watch: {record.bestReplay}";
            GUIUtility.systemCopyBuffer = text;
            bool sheet = NativeShare.Share(text);
            if (shareBestLabel != null)
            {
                shareBestLabel.text = sheet ? "Sharing…" : "Copied!";
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
                archiveRowLabels[i].text = record != null && record.completed
                    ? $"{date:MMM d}  ·  best {record.bestStrokes}"
                      + (record.bestStars > 0 ? $"  ·  {record.bestStars}-star" : "")
                    : $"{date:MMM d}  ·  not played";
                archiveRowLabels[i].color = record != null && record.completed
                    ? UIStyle.Cream
                    : UIStyle.CreamDim;
            }

            if (archivePageLabel != null)
            {
                int first = _archivePage * 7 + 1;
                archivePageLabel.text = $"{first}–{first + 6} days ago";
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
            string streak = stats.Data.streak > 0 ? $"Streak {stats.Data.streak}" : "No streak yet";
            string attempts = today.attempts > 0 ? $" · Today: {today.attempts} attempt(s)" : "";
            string practice = stats.Data.practicePlayed > 0 ? $" · Practice: {stats.Data.practicePlayed}" : "";
            return streak + attempts + practice;
        }

        private void CycleDifficulty()
        {
            GameSession.PracticeDifficulty = (Difficulty)(((int)GameSession.PracticeDifficulty + 1) % 3);
            if (difficultyLabel != null)
            {
                difficultyLabel.text = GameSession.PracticeDifficulty.ToString();
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
