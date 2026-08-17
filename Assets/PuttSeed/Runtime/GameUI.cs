#nullable enable
using PuttSeed.Core.Replay;
using UnityEngine;
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
        public GameObject? starsRow;
        public Image[] starImages = new Image[0];
        public GameObject? failPanel;
        public Button? failRetryButton;
        public GameObject? toastChip;
        public Text? toastText;
        public Button? menuButton;
        public Button? nextLessonButton;
        public GameObject? importRow;
        public InputField? importField;
        public Button? watchButton;
        public Button? retryButton;
        public Button? shareButton;
        public Button? ghostButton;
        public Button? undoButton;

        private SimRunner _runner = null!;
        private ModeController _modes = null!;
        private float _toastUntil;
        private bool _starsRevealed;
        private Coroutine? _starReveal;
        private Vector3 _toastBase;
        private bool _toastBaseCached;
        private Coroutine? _toastAnim;
        private int _lastStrokesShown;
        private Coroutine? _counterPulse;
        private bool? _undoLaidOut;

        // Prompt each clipboard code once per app run, not per scene entry.
        private static string? _promptedClipboardCode;

        /// <summary>Wires behavior onto the scene-authored controls.</summary>
        public void Initialize(SimRunner runner, ModeController modes)
        {
            _runner = runner;
            _modes = modes;

            menuButton?.onClick.AddListener(OnMenu);
            nextLessonButton?.onClick.AddListener(() => _modes.NextTutorial());
            watchButton?.onClick.AddListener(OnImport);
            retryButton?.onClick.AddListener(() => _runner.Retry());
            failRetryButton?.onClick.AddListener(() => _runner.Retry());
            shareButton?.onClick.AddListener(OnShare);
            ghostButton?.onClick.AddListener(OnToggleAuthorGhost);

            undoButton?.onClick.AddListener(OnUndo);
            runner.StateChanged += Refresh;
            modes.ModeChanged += Refresh;
            modes.AchievementUnlocked += def => ShowToast(string.Format(Loc.Tr("Achievement — {0}!"), Loc.Tr(def.Title)));
            modes.PracticeBestImproved += strokes => ShowToast(string.Format(Loc.Tr("New practice best — {0}!"), strokes));
            OfferClipboardReplay();
            Refresh();
        }

        private void Update()
        {
            if (toastChip != null && toastChip.activeSelf && Time.unscaledTime > _toastUntil)
            {
                toastChip.SetActive(false);
            }

            // Android back button (Escape) returns to the menu.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UiSounds.ClickDown();
                OnMenu();
            }
        }

        private void Refresh()
        {
            var sim = _runner.Sim;
            var gen = _runner.Generation;
            if (hintText != null)
            {
                hintText.text = Loc.Tr(_modes.CurrentHint);
            }

            hintChip?.SetActive(_modes.CurrentHint.Length > 0);
            nextLessonButton?.gameObject.SetActive(_modes.Mode == GameMode.Tutorial);
            // The mulligan is a teaching tool — never on the daily. The bar
            // reflows so five buttons share the row evenly when Undo is gone.
            bool showUndo = _modes.Mode != GameMode.Daily;
            undoButton?.gameObject.SetActive(showUndo);
            if (_undoLaidOut != showUndo)
            {
                _undoLaidOut = showUndo;
                LayoutBottomBar(showUndo);
            }

            if (counterText == null)
            {
                return;
            }

            if (sim == null || gen == null)
            {
                counterText.text = Loc.Tr("generating…");
                return;
            }

            string modeLabel = _modes.Mode switch
            {
                GameMode.Daily => _modes.DailyModeLabel,
                GameMode.Practice => string.Format(Loc.Tr("Practice · {0}"), Loc.Tr(gen.Difficulty.ToString())),
                _ => string.Format(Loc.Tr("Tutorial {0}/{1}"), _modes.TutorialIndex + 1, TutorialConfig.Stages.Length),
            };

            int streak = _modes.Stats.Data.streak;
            string streakLabel = streak > 0 ? string.Format(Loc.Tr("   Streak {0}"), streak) : "";
            counterText.text = string.Format(Loc.Tr("{0}   Strokes {1}/{2}   Par {3}{4}"),
                modeLabel, sim.Strokes, sim.StrokeLimit, gen.Course.Par, streakLabel);
            if (sim.Strokes > _lastStrokesShown)
            {
                if (_counterPulse != null)
                {
                    StopCoroutine(_counterPulse);
                }

                _counterPulse = StartCoroutine(PulseCounter());
            }

            _lastStrokesShown = sim.Strokes;
            if (statusText != null)
            {
                // The fail state gets its own panel; the status line is for success.
                statusText.text = sim.IsHoled ? Loc.Tr(GolfTerms.SuccessLine(sim.Strokes, gen.Course.Par)) : "";
            }

            if (failPanel != null && failPanel.activeSelf != sim.IsFailed)
            {
                if (sim.IsFailed)
                {
                    UiFx.PopIn(this, failPanel);
                }
                else
                {
                    failPanel.SetActive(false);
                }
            }

            RefreshStars(sim, gen.Course.Par);
        }

        /// <summary>
        /// Stars reveal one by one on hole-out (in step with the rising audio
        /// notes: 0.5 s lead, 0.16 s apart), each with a settle-down pop;
        /// unearned slots stay dim from the start.
        /// </summary>
        private void RefreshStars(PuttSeed.Core.Sim.GolfSim sim, int par)
        {
            if (starsRow == null)
            {
                return;
            }

            if (!sim.IsHoled)
            {
                starsRow.SetActive(false);
                _starsRevealed = false;
                if (_starReveal != null)
                {
                    StopCoroutine(_starReveal);
                    _starReveal = null;
                }

                return;
            }

            if (!_starsRevealed)
            {
                _starsRevealed = true;
                _starReveal = StartCoroutine(
                    RevealStars(PuttSeed.Core.Sim.Scoring.Stars(sim.Strokes, par)));
            }
        }

        /// <summary>
        /// Distributes the bottom-bar buttons evenly across the row — five
        /// slots when Undo is hidden (daily), six when it shows.
        /// </summary>
        private void LayoutBottomBar(bool withUndo)
        {
            var buttons = withUndo
                ? new[] { menuButton, retryButton, shareButton, ghostButton, watchButton, undoButton }
                : new[] { menuButton, retryButton, shareButton, ghostButton, watchButton };
            const float margin = 0.02f;
            const float gap = 0.012f;
            float width = (1f - 2f * margin - (buttons.Length - 1) * gap) / buttons.Length;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                {
                    continue;
                }

                var rect = (RectTransform)buttons[i]!.transform;
                float x0 = margin + i * (width + gap);
                rect.anchorMin = new Vector2(x0, 0.016f);
                rect.anchorMax = new Vector2(x0 + width, 0.077f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        /// <summary>A quick swell on the counter when a stroke is spent.</summary>
        private System.Collections.IEnumerator PulseCounter()
        {
            var rect = counterText!.transform;
            const float duration = 0.18f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                rect.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }

            rect.localScale = Vector3.one;
            _counterPulse = null;
        }

        private System.Collections.IEnumerator RevealStars(int stars)
        {
            starsRow!.SetActive(true);
            var dim = new Color(1f, 1f, 1f, 0.16f);
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].color = dim;
                    starImages[i].transform.localScale = Vector3.one;
                }
            }

            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < stars && i < starImages.Length; i++)
            {
                var image = starImages[i];
                if (image != null)
                {
                    image.color = UIStyle.Accent;
                    for (float t = 0f; t < 0.15f; t += Time.deltaTime)
                    {
                        image.transform.localScale =
                            Vector3.one * Mathf.Lerp(1.8f, 1f, Mathf.SmoothStep(0f, 1f, t / 0.15f));
                        yield return null;
                    }

                    image.transform.localScale = Vector3.one;
                }

                // Remaining gap so the next star lands with its audio note.
                yield return new WaitForSeconds(0.01f);
            }

            _starReveal = null;
        }

        private static void OnMenu()
        {
            SceneFader.LoadScene("Menu");
        }

        /// <summary>
        /// If the clipboard holds a valid PUTT- code, prefill the import field
        /// and point at Watch — pasting by hand becomes a single tap. Each
        /// code is offered once per app run.
        /// </summary>
        private void OfferClipboardReplay()
        {
            string clip = GUIUtility.systemCopyBuffer ?? "";
            int at = clip.IndexOf("PUTT-", System.StringComparison.Ordinal);
            if (at < 0)
            {
                return;
            }

            int end = at;
            while (end < clip.Length && !char.IsWhiteSpace(clip[end]))
            {
                end++;
            }

            string token = clip.Substring(at, end - at);
            if (token == _promptedClipboardCode
                || !ReplayCodec.TryDecode(token, out _, out _))
            {
                return;
            }

            _promptedClipboardCode = token;
            if (importField != null)
            {
                importField.text = token;
                if (importRow != null && !importRow.activeSelf)
                {
                    UiFx.SlideUp(this, importRow); // surface the prefilled chip
                }

                ShowToast(Loc.Tr("Replay code found in clipboard — tap Watch."));
            }
        }

        private void OnUndo()
        {
            if (_runner.TryUndoShot())
            {
                ShowToast(Loc.Tr("Shot undone."));
            }
        }

        private void OnShare()
        {
            var sim = _runner.Sim;
            if (sim == null || !sim.IsHoled)
            {
                // Practice courses are shareable BEFORE finishing: a zero-shot
                // code is a course invitation, not a replay.
                if (sim != null && _modes.Mode == GameMode.Practice && _runner.Generation != null)
                {
                    var courseCode = ReplayCodec.Encode(_runner.Seed, System.Array.Empty<PuttSeed.Core.Sim.ShotInput>());
                    string invite = $"PUTTSEED — can you beat par {_runner.Generation.Course.Par}? Play: {courseCode}";
                    GUIUtility.systemCopyBuffer = invite;
                    ShowToast(Loc.Tr(NativeShare.Share(invite) ? "Sharing course…" : "Course code copied!"));
                    return;
                }

                ShowToast(Loc.Tr("Finish the hole to share your run."));
                return;
            }

            var shots = new PuttSeed.Core.Sim.ShotInput[_runner.PlayedShots.Count];
            for (int i = 0; i < shots.Length; i++)
            {
                shots[i] = _runner.PlayedShots[i];
            }

            var code = ReplayCodec.Encode(_runner.Seed, shots);
            string text = _modes.BuildShareText(sim.Strokes, _runner.Generation!.Course.Par, code);
            GUIUtility.systemCopyBuffer = text; // clipboard as well, on every platform
            ShowToast(Loc.Tr(NativeShare.Share(text) ? "Sharing…" : "Copied to clipboard!"));
        }

        private void OnToggleAuthorGhost()
        {
            if (_runner.Generation == null)
            {
                return;
            }

            // Only the author ghost toggles here; best/imported ghosts have
            // their own lifecycles (daily load, import field).
            if (_runner.HasGhost("author"))
            {
                _runner.RemoveGhosts("author");
                ShowToast(Loc.Tr("Author ghost off."));
            }
            else
            {
                _runner.AddAuthorGhost();
                ShowToast(Loc.Tr("Author ghost on (amber)."));
            }
        }

        private void OnImport()
        {
            if (importField == null || importRow == null)
            {
                return;
            }

            // Watch is a toggle-and-confirm: first tap opens the paste chip,
            // the next tap imports (or closes an empty chip again).
            if (!importRow.activeSelf)
            {
                UiFx.SlideUp(this, importRow);
                return;
            }

            string text = importField.text.Trim();
            if (text.Length == 0)
            {
                importRow.SetActive(false);
                return;
            }

            if (_modes.ImportReplay(text))
            {
                ShowToast(Loc.Tr("Ghost playing (pink)."));
                importField.text = "";
                importRow.SetActive(false);
            }
            else
            {
                ShowToast(Loc.Tr("Not a valid PUTT- code."));
            }
        }

        private void ShowToast(string message)
        {
            if (toastText != null && toastChip != null)
            {
                if (!_toastBaseCached)
                {
                    _toastBase = toastChip.transform.localPosition;
                    _toastBaseCached = true;
                }

                if (_toastAnim != null)
                {
                    StopCoroutine(_toastAnim);
                }

                toastChip.transform.localPosition = _toastBase;
                toastText.text = message;
                _toastAnim = UiFx.SlideUp(this, toastChip);
                _toastUntil = Time.unscaledTime + 2.5f;
            }
        }
    }
}
