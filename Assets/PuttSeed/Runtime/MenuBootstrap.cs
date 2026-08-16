#nullable enable
using System;
using System.IO;
using PuttSeed.Core.CourseGen;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        private bool _showCountdown;

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

            RefreshSettingsLabels(stats);
            soundButton?.onClick.AddListener(() =>
            {
                stats.SetSoundEnabled(!stats.Data.soundEnabled);
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
            if (!_showCountdown || countdownText == null)
            {
                return;
            }

            var remaining = DailyCountdown.UntilNextHole(DateTime.UtcNow);
            countdownText.text = remaining.TotalSeconds <= 0
                ? "New hole is ready — restart to play!"
                : $"next hole in {DailyCountdown.Format(remaining)}";
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
            GameSession.UseFixedSeed = false;
            SceneManager.LoadScene("Game");
        }

        /// <summary>The shared stats file path (same file the game scene writes).</summary>
        public static string StatsPath()
            => Path.Combine(Application.persistentDataPath, "puttseed-stats.json");
    }
}
