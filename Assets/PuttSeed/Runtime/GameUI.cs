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
        private GameObject _hintChip = null!;
        private Text _status = null!;
        private Text _toast = null!;
        private GameObject _toastChip = null!;
        private GameObject _nextLessonButton = null!;
        private InputField _importField = null!;
        private float _toastUntil;

        /// <summary>Builds the canvas hierarchy and wires events.</summary>
        public void Initialize(SimRunner runner, ModeController modes)
        {
            _runner = runner;
            _modes = modes;

            var canvas = UIFactory.CreateCanvas(transform);

            // Top bar card with the counter.
            UIFactory.CreatePanel(canvas.transform, "TopBar",
                new Vector2(0.02f, 0.925f), new Vector2(0.98f, 0.985f), UIStyle.PanelSoft);
            _counter = UIFactory.CreateText(canvas.transform, "Counter",
                new Vector2(0.05f, 0.925f), new Vector2(0.95f, 0.985f), 40, TextAnchor.MiddleLeft);

            // Tutorial hint chip (hidden when there is no hint).
            var hintChip = UIFactory.CreatePanel(canvas.transform, "HintChip",
                new Vector2(0.06f, 0.865f), new Vector2(0.94f, 0.915f), UIStyle.PanelSoft);
            _hintChip = hintChip.gameObject;
            _hint = UIFactory.CreateText(_hintChip.transform, "Hint",
                new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), 30, TextAnchor.MiddleCenter);
            _hint.color = UIStyle.Hint;

            _status = UIFactory.CreateText(canvas.transform, "Status",
                new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.72f), 76, TextAnchor.MiddleCenter, shadow: true);

            // Toast chip (hidden when idle).
            var toastChip = UIFactory.CreatePanel(canvas.transform, "ToastChip",
                new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.3f), UIStyle.PanelDark);
            _toastChip = toastChip.gameObject;
            _toast = UIFactory.CreateText(_toastChip.transform, "Toast",
                new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), 32, TextAnchor.MiddleCenter);
            _toastChip.SetActive(false);

            // Bottom control card behind all three rows.
            UIFactory.CreatePanel(canvas.transform, "BottomBar",
                new Vector2(0.01f, 0.008f), new Vector2(0.99f, 0.245f), UIStyle.PanelSoft);

            // Navigation row.
            UIFactory.CreateButton(canvas.transform, "Menu", new Vector2(0.03f, 0.168f), new Vector2(0.25f, 0.232f), OnMenu);
            var next = UIFactory.CreateButton(canvas.transform, "Next lesson",
                new Vector2(0.27f, 0.168f), new Vector2(0.61f, 0.232f), () => _modes.NextTutorial(), 34, primary: true);
            _nextLessonButton = next.transform.parent.gameObject;

            // Import row.
            _importField = UIFactory.CreateInputField(canvas.transform,
                new Vector2(0.03f, 0.098f), new Vector2(0.71f, 0.158f), "paste PUTT- code…");
            UIFactory.CreateButton(canvas.transform, "Watch", new Vector2(0.73f, 0.098f), new Vector2(0.97f, 0.158f), OnImport);

            // Action row.
            UIFactory.CreateButton(canvas.transform, "Retry", new Vector2(0.03f, 0.018f), new Vector2(0.25f, 0.088f), () => _runner.Retry());
            UIFactory.CreateButton(canvas.transform, "Share", new Vector2(0.27f, 0.018f), new Vector2(0.49f, 0.088f), OnShare);
            UIFactory.CreateButton(canvas.transform, "Ghost", new Vector2(0.51f, 0.018f), new Vector2(0.73f, 0.088f), OnToggleAuthorGhost);

            runner.StateChanged += Refresh;
            modes.ModeChanged += Refresh;
            Refresh();
        }

        private void Update()
        {
            if (_toastChip.activeSelf && Time.unscaledTime > _toastUntil)
            {
                _toastChip.SetActive(false);
            }
        }

        private void Refresh()
        {
            var sim = _runner.Sim;
            var gen = _runner.Generation;
            _hint.text = _modes.CurrentHint;
            _hintChip.SetActive(_modes.CurrentHint.Length > 0);
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
            _toastChip.SetActive(true);
            _toastUntil = Time.unscaledTime + 2.5f;
        }
    }
}
