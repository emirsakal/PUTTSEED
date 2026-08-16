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
        private static readonly Color FlagRed = new Color(0.86f, 0.24f, 0.19f);

        private Text _difficultyLabel = null!;

        private static Color DifficultyColor(Difficulty difficulty) => difficulty switch
        {
            Difficulty.Easy => new Color(0.45f, 0.85f, 0.45f),
            Difficulty.Hard => new Color(0.95f, 0.36f, 0.3f),
            _ => new Color(0.99f, 0.85f, 0.35f),
        };

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

            // Decorative backdrop: two huge soft circles drifting off-canvas.
            UIFactory.CreateCircle(canvas.transform, "Deco1",
                new Vector2(-0.25f, 0.62f), new Vector2(0.35f, 0.96f), new Color(1f, 1f, 1f, 0.05f));
            UIFactory.CreateCircle(canvas.transform, "Deco2",
                new Vector2(0.7f, -0.12f), new Vector2(1.35f, 0.25f), new Color(1f, 1f, 1f, 0.05f));

            // Emblem above the title: cup + flag pole + amber pennant + ball.
            UIFactory.CreateCircle(canvas.transform, "EmblemHole",
                new Vector2(0.44f, 0.855f), new Vector2(0.56f, 0.885f), new Color(0.05f, 0.09f, 0.06f, 0.9f));
            var pole = UIFactory.CreateRect(canvas.transform, "EmblemPole",
                new Vector2(0.496f, 0.87f), new Vector2(0.504f, 0.955f));
            var poleImage = pole.gameObject.AddComponent<UnityEngine.UI.Image>();
            poleImage.color = UIStyle.Cream;
            poleImage.raycastTarget = false;
            UIFactory.CreatePanel(canvas.transform, "EmblemFlag",
                new Vector2(0.504f, 0.915f), new Vector2(0.63f, 0.952f), FlagRed);

            // Fixed square size + circle sprite = perfectly round on any aspect.
            var ballRect = UIFactory.CreateRect(canvas.transform, "EmblemBall",
                new Vector2(0.425f, 0.8765f), new Vector2(0.425f, 0.8765f));
            ballRect.sizeDelta = new Vector2(48f, 48f);
            var ballImage = ballRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            ballImage.sprite = UIFactory.CircleSprite();
            ballImage.color = UIStyle.Cream;
            ballImage.raycastTarget = false;

            var title = UIFactory.CreateText(canvas.transform, "Title",
                new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.85f), 124, TextAnchor.MiddleCenter, shadow: true);
            title.text = "PUTTSEED";

            var tagline = UIFactory.CreateText(canvas.transform, "Tagline",
                new Vector2(0.05f, 0.705f), new Vector2(0.95f, 0.745f), 33, TextAnchor.MiddleCenter);
            tagline.text = "one hole a day · same for everyone";
            tagline.color = UIStyle.CreamDim;

            var utc = DateTime.UtcNow;
            int today = ModeController.DayNumber(utc);
            var todayRecord = stats.GetOrCreateDay(today);

            // Mode card.
            UIFactory.CreatePanel(canvas.transform, "Card",
                new Vector2(0.06f, 0.27f), new Vector2(0.94f, 0.67f), UIStyle.PanelSoft);

            string dailyLabel = todayRecord.completed
                ? $"Daily {utc:MMM d} — done in {todayRecord.bestStrokes}"
                : $"Play today's hole · {utc:MMM d}";
            UIFactory.CreateButton(canvas.transform, dailyLabel,
                new Vector2(0.1f, 0.545f), new Vector2(0.9f, 0.635f), () => Launch(GameMode.Daily), 44,
                primary: !todayRecord.completed);

            UIFactory.CreateButton(canvas.transform, "Practice",
                new Vector2(0.1f, 0.43f), new Vector2(0.6f, 0.52f), () => Launch(GameMode.Practice), 44);
            _difficultyLabel = UIFactory.CreateButton(canvas.transform, GameSession.PracticeDifficulty.ToString(),
                new Vector2(0.62f, 0.43f), new Vector2(0.9f, 0.52f), CycleDifficulty, 36);
            _difficultyLabel.color = DifficultyColor(GameSession.PracticeDifficulty);

            var tutorialLabel = firstLaunch ? "Tutorial  ·  start here" : "Tutorial";
            UIFactory.CreateButton(canvas.transform, tutorialLabel,
                new Vector2(0.1f, 0.315f), new Vector2(0.9f, 0.405f), () => Launch(GameMode.Tutorial), 44,
                primary: firstLaunch);

            // Stats chip.
            UIFactory.CreatePanel(canvas.transform, "FooterChip",
                new Vector2(0.14f, 0.205f), new Vector2(0.86f, 0.25f), UIStyle.PanelSoft);
            var footer = UIFactory.CreateText(canvas.transform, "Footer",
                new Vector2(0.14f, 0.205f), new Vector2(0.86f, 0.25f), 30, TextAnchor.MiddleCenter);
            footer.color = UIStyle.CreamDim;
            footer.text = BuildStatsLine(stats, todayRecord);
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
            _difficultyLabel.text = GameSession.PracticeDifficulty.ToString();
            _difficultyLabel.color = DifficultyColor(GameSession.PracticeDifficulty);
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
