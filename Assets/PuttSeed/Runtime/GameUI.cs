#nullable enable
using PuttSeed.Core.Replay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// The in-game HUD (mode selection lives in the Menu scene): stroke/par/
    /// streak counter, tutorial hint line, status, retry, share, ghost toggle,
    /// replay import, back-to-menu, and a next-lesson button in tutorials.
    /// Pure presentation + boundary calls.
    /// </summary>
    public sealed class GameUI : MonoBehaviour
    {
        private SimRunner _runner = null!;
        private ModeController _modes = null!;

        private Text _counter = null!;
        private Text _hint = null!;
        private Text _status = null!;
        private Text _toast = null!;
        private GameObject _nextLessonButton = null!;
        private InputField _importField = null!;
        private float _toastUntil;

        /// <summary>Builds the canvas hierarchy and wires events.</summary>
        public void Initialize(SimRunner runner, ModeController modes)
        {
            _runner = runner;
            _modes = modes;

            var canvas = UIFactory.CreateCanvas(transform);

            _counter = UIFactory.CreateText(canvas.transform, "Counter", new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f), 42, TextAnchor.UpperLeft);
            _hint = UIFactory.CreateText(canvas.transform, "Hint", new Vector2(0.03f, 0.88f), new Vector2(0.97f, 0.925f), 32, TextAnchor.UpperCenter);
            _hint.color = new Color(1f, 1f, 0.75f);
            _status = UIFactory.CreateText(canvas.transform, "Status", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.72f), 72, TextAnchor.MiddleCenter);
            _toast = UIFactory.CreateText(canvas.transform, "Toast", new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.29f), 34, TextAnchor.MiddleCenter);

            // Navigation row.
            UIFactory.CreateButton(canvas.transform, "Menu", new Vector2(0.02f, 0.165f), new Vector2(0.24f, 0.235f), OnMenu);
            var next = UIFactory.CreateButton(canvas.transform, "Next lesson", new Vector2(0.26f, 0.165f), new Vector2(0.6f, 0.235f), () => _modes.NextTutorial());
            _nextLessonButton = next.transform.parent.gameObject;

            // Import row.
            _importField = UIFactory.CreateInputField(canvas.transform, new Vector2(0.02f, 0.1f), new Vector2(0.72f, 0.155f), "  paste PUTT- code…");
            UIFactory.CreateButton(canvas.transform, "Watch", new Vector2(0.74f, 0.1f), new Vector2(0.98f, 0.155f), OnImport);

            // Action row.
            UIFactory.CreateButton(canvas.transform, "Retry", new Vector2(0.02f, 0.02f), new Vector2(0.24f, 0.09f), () => _runner.Retry());
            UIFactory.CreateButton(canvas.transform, "Share", new Vector2(0.26f, 0.02f), new Vector2(0.48f, 0.09f), OnShare);
            UIFactory.CreateButton(canvas.transform, "Ghost", new Vector2(0.5f, 0.02f), new Vector2(0.72f, 0.09f), OnToggleAuthorGhost);

            runner.StateChanged += Refresh;
            modes.ModeChanged += Refresh;
            Refresh();
        }

        private void Update()
        {
            if (_toast.text.Length > 0 && Time.unscaledTime > _toastUntil)
            {
                _toast.text = "";
            }
        }

        private void Refresh()
        {
            var sim = _runner.Sim;
            var gen = _runner.Generation;
            _hint.text = _modes.CurrentHint;
            _nextLessonButton.SetActive(_modes.Mode == GameMode.Tutorial);

            if (sim == null || gen == null)
            {
                _counter.text = "generating…";
                return;
            }

            string modeLabel = _modes.Mode switch
            {
                GameMode.Daily => "Daily",
                GameMode.Practice => $"Practice · {gen.Difficulty}",
                _ => $"Tutorial {_modes.TutorialIndex + 1}/{TutorialConfig.Stages.Length}",
            };

            int streak = _modes.Stats.Data.streak;
            string streakLabel = streak > 0 ? $"   Streak {streak}" : "";
            _counter.text = $"{modeLabel}   Strokes {sim.Strokes}/{sim.StrokeLimit}   Par {gen.Course.Par}{streakLabel}";
            _status.text = sim.IsHoled
                ? SuccessLine(sim.Strokes, gen.Course.Par)
                : sim.IsFailed ? "Out of strokes — retry!" : "";
        }

        private static string SuccessLine(int strokes, int par)
            => strokes <= par - 1 ? "Under par — brilliant!"
             : strokes == par ? "Par — well played!"
             : "Holed!";

        private static void OnMenu()
        {
            SceneManager.LoadScene("Menu");
        }

        private void OnShare()
        {
            var sim = _runner.Sim;
            if (sim == null || !sim.IsHoled)
            {
                ShowToast("Finish the hole to share your run.");
                return;
            }

            var shots = new PuttSeed.Core.Sim.ShotInput[_runner.PlayedShots.Count];
            for (int i = 0; i < shots.Length; i++)
            {
                shots[i] = _runner.PlayedShots[i];
            }

            var code = ReplayCodec.Encode(_runner.Seed, shots);
            GUIUtility.systemCopyBuffer =
                $"PUTTSEED — {sim.Strokes} strokes (par {_runner.Generation!.Course.Par}). Watch: {code}";
            ShowToast("Copied to clipboard!");
        }

        private void OnToggleAuthorGhost()
        {
            if (_runner.Generation == null)
            {
                return;
            }

            if (_runner.Ghosts.Count > 0)
            {
                _runner.ClearGhosts();
                ShowToast("Ghost off.");
            }
            else
            {
                _runner.AddAuthorGhost();
                ShowToast("Author ghost on.");
            }
        }

        private void OnImport()
        {
            if (_modes.ImportReplay(_importField.text.Trim()))
            {
                ShowToast("Ghost playing.");
            }
            else
            {
                ShowToast("Not a valid PUTT- code.");
            }
        }

        private void ShowToast(string message)
        {
            _toast.text = message;
            _toastUntil = Time.unscaledTime + 2.5f;
        }
    }
}
