#nullable enable
using PuttSeed.Core.Replay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// The in-game HUD. The hierarchy is scene-authored (baked by PuttSeed →
    /// Rebuild Scenes; edit or reskin it freely in the Inspector) — this
    /// component only binds behavior to the serialized references: counter,
    /// hint/toast chips, status, retry/share/ghost, replay import, menu, and
    /// the tutorial next-lesson button.
    /// </summary>
    public sealed class GameUI : MonoBehaviour
    {
        [Header("Scene-authored UI (assigned by PuttSeed → Rebuild Scenes)")]
        public Text? counterText;
        public GameObject? hintChip;
        public Text? hintText;
        public Text? statusText;
        public GameObject? toastChip;
        public Text? toastText;
        public Button? menuButton;
        public Button? nextLessonButton;
        public InputField? importField;
        public Button? watchButton;
        public Button? retryButton;
        public Button? shareButton;
        public Button? ghostButton;

        private SimRunner _runner = null!;
        private ModeController _modes = null!;
        private float _toastUntil;

        /// <summary>Wires behavior onto the scene-authored controls.</summary>
        public void Initialize(SimRunner runner, ModeController modes)
        {
            _runner = runner;
            _modes = modes;

            menuButton?.onClick.AddListener(OnMenu);
            nextLessonButton?.onClick.AddListener(() => _modes.NextTutorial());
            watchButton?.onClick.AddListener(OnImport);
            retryButton?.onClick.AddListener(() => _runner.Retry());
            shareButton?.onClick.AddListener(OnShare);
            ghostButton?.onClick.AddListener(OnToggleAuthorGhost);

            runner.StateChanged += Refresh;
            modes.ModeChanged += Refresh;
            Refresh();
        }

        private void Update()
        {
            if (toastChip != null && toastChip.activeSelf && Time.unscaledTime > _toastUntil)
            {
                toastChip.SetActive(false);
            }
        }

        private void Refresh()
        {
            var sim = _runner.Sim;
            var gen = _runner.Generation;
            if (hintText != null)
            {
                hintText.text = _modes.CurrentHint;
            }

            hintChip?.SetActive(_modes.CurrentHint.Length > 0);
            nextLessonButton?.gameObject.SetActive(_modes.Mode == GameMode.Tutorial);

            if (counterText == null)
            {
                return;
            }

            if (sim == null || gen == null)
            {
                counterText.text = "generating…";
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
            counterText.text = $"{modeLabel}   Strokes {sim.Strokes}/{sim.StrokeLimit}   Par {gen.Course.Par}{streakLabel}";
            if (statusText != null)
            {
                statusText.text = sim.IsHoled
                    ? SuccessLine(sim.Strokes, gen.Course.Par)
                    : sim.IsFailed ? "Out of strokes — retry!" : "";
            }
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
            if (importField == null)
            {
                return;
            }

            if (_modes.ImportReplay(importField.text.Trim()))
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
            if (toastText != null && toastChip != null)
            {
                toastText.text = message;
                toastChip.SetActive(true);
                _toastUntil = Time.unscaledTime + 2.5f;
            }
        }
    }
}
