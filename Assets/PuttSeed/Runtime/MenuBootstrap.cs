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
    /// The menu scene's single entry point: title, today's status, streak, and
    /// the mode buttons. Selecting a mode writes <see cref="GameSession"/> and
    /// loads the Game scene. Stats are read-only here (the game scene owns
    /// writing). First launch highlights the tutorial (GDD FTUE).
    /// </summary>
    public sealed class MenuBootstrap : MonoBehaviour
    {
        private Text _difficultyLabel = null!;

        private void Start()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PaletteMaterials.Felt;
            cam.orthographic = true;

            var stats = new StatsStore(StatsPath());
            bool firstLaunch = stats.Data.lastCompletedDay == 0
                && stats.Data.practicePlayed == 0
                && stats.Data.days.Count == 0;

            var canvas = UIFactory.CreateCanvas(transform);

            var title = UIFactory.CreateText(canvas.transform, "Title",
                new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.92f), 120, TextAnchor.MiddleCenter);
            title.text = "PUTTSEED";

            var tagline = UIFactory.CreateText(canvas.transform, "Tagline",
                new Vector2(0.05f, 0.73f), new Vector2(0.95f, 0.78f), 34, TextAnchor.MiddleCenter);
            tagline.text = "one hole a day, same for everyone";
            tagline.color = new Color(1f, 1f, 1f, 0.7f);

            var utc = DateTime.UtcNow;
            int today = ModeController.DayNumber(utc);
            var todayRecord = stats.GetOrCreateDay(today);

            // Daily.
            string dailyLabel = todayRecord.completed
                ? $"Daily {utc:MMM d} — done in {todayRecord.bestStrokes}"
                : $"Daily {utc:MMM d}";
            UIFactory.CreateButton(canvas.transform, dailyLabel,
                new Vector2(0.1f, 0.56f), new Vector2(0.9f, 0.65f), () => Launch(GameMode.Daily), 44);

            // Practice + difficulty.
            UIFactory.CreateButton(canvas.transform, "Practice",
                new Vector2(0.1f, 0.44f), new Vector2(0.62f, 0.53f), () => Launch(GameMode.Practice), 44);
            _difficultyLabel = UIFactory.CreateButton(canvas.transform, GameSession.PracticeDifficulty.ToString(),
                new Vector2(0.64f, 0.44f), new Vector2(0.9f, 0.53f), CycleDifficulty, 38);

            // Tutorial.
            var tutorialLabel = firstLaunch ? "Tutorial  ←  start here" : "Tutorial";
            UIFactory.CreateButton(canvas.transform, tutorialLabel,
                new Vector2(0.1f, 0.32f), new Vector2(0.9f, 0.41f), () => Launch(GameMode.Tutorial), 44);

            // Stats footer.
            var footer = UIFactory.CreateText(canvas.transform, "Footer",
                new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.28f), 32, TextAnchor.MiddleCenter);
            footer.color = new Color(1f, 1f, 1f, 0.75f);
            footer.text = BuildStatsLine(stats, todayRecord);
        }

        private static string BuildStatsLine(StatsStore stats, DayRecord today)
        {
            string streak = stats.Data.streak > 0 ? $"Streak {stats.Data.streak}" : "No streak yet";
            string attempts = today.attempts > 0 ? $" · today: {today.attempts} attempt(s)" : "";
            string practice = stats.Data.practicePlayed > 0 ? $" · practice: {stats.Data.practicePlayed}" : "";
            return streak + attempts + practice;
        }

        private void CycleDifficulty()
        {
            GameSession.PracticeDifficulty = (Difficulty)(((int)GameSession.PracticeDifficulty + 1) % 3);
            _difficultyLabel.text = GameSession.PracticeDifficulty.ToString();
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
