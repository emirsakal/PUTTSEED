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

        /// <summary>Mode, par and streak — the small print, right-aligned.</summary>
        public Text? counterText;

        /// <summary>The hero number: strokes taken, with the limit hung off it.</summary>
        public Text? strokeText;

        /// <summary>The score-to-par chip beside it (E, +1, -1).</summary>
        public GameObject? parChip;

        /// <summary>Text inside the par chip.</summary>
        public Text? parChipText;

        /// <summary>The day's closing card, shown once a daily is holed.</summary>
        public GameObject? dailyCard;

        /// <summary>Result line on the closing card.</summary>
        public Text? dailyCardResult;

        /// <summary>Streak line on the closing card.</summary>
        public Text? dailyCardStreak;

        /// <summary>Countdown line on the closing card (ticks in Update).</summary>
        public Text? dailyCardCountdown;

        /// <summary>The card's primary share button.</summary>
        public Button? dailyShareButton;
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
        private int _barShape = -1;
        private string _lastCountdown = "";

        private static readonly Color UnderPar = new Color(0.45f, 0.85f, 0.45f);
        private static readonly Color OverPar = new Color(0.95f, 0.36f, 0.30f);

        // Prompt each clipboard code once per app run, not per scene entry.
        private static string? _promptedClipboardCode;

        /// <summary>Wires behavior onto the scene-authored controls.</summary>
        public void Initialize(SimRunner runner, ModeController modes)
        {
            _runner = runner;
            _modes = modes;

            menuButton?.onClick.AddListener(OnMenu);
            nextLessonButton?.onClick.AddListener(() =>
            {
                if (_modes.Mode == GameMode.Journey)
                {
                    _modes.NextJourneyLevel();
                }
                else if (_modes.Mode == GameMode.Gauntlet)
                {
                    _modes.NextGauntletHole();
                }
                else if (_modes.HasNextTutorialStage)
                {
                    _modes.NextTutorial();
                }
                else
                {
                    // The last lesson ends the tutorial rather than looping
                    // back to the first: a course a player has already been
                    // taught is not the reward for finishing being taught.
                    OnMenu();
                }
            });
            watchButton?.onClick.AddListener(OnImport);
            retryButton?.onClick.AddListener(() => _runner.Retry());
            failRetryButton?.onClick.AddListener(() => _runner.Retry());
            shareButton?.onClick.AddListener(OnShare);
            dailyShareButton?.onClick.AddListener(OnShare);
            ghostButton?.onClick.AddListener(OnToggleAuthorGhost);

            undoButton?.onClick.AddListener(OnUndo);
            runner.StateChanged += Refresh;
            modes.ModeChanged += Refresh;
            modes.AchievementUnlocked += def => ShowToast(string.Format(Loc.Tr("Achievement — {0}!"), Loc.Tr(def.Title)));
            modes.PracticeBestImproved += strokes => ShowToast(string.Format(Loc.Tr("New practice best — {0}!"), strokes));
            modes.GauntletFinished += strokes =>
                ShowToast(string.Format(Loc.Tr("Week done — {0} strokes!"), strokes));
            OfferClipboardReplay();
            Refresh();
        }

        private void Update()
        {
            if (toastChip != null && toastChip.activeSelf && Time.unscaledTime > _toastUntil)
            {
                toastChip.SetActive(false);
            }

            // Refresh() is event-driven and the sim goes quiet the moment the
            // ball drops, so the countdown has to tick from here — and only
            // when the second actually changes, not once a frame.
            if (dailyCard != null && dailyCard.activeSelf && dailyCardCountdown != null)
            {
                string remaining = DailyCountdown.Format(
                    DailyCountdown.UntilNextHole(System.DateTime.UtcNow));
                if (remaining != _lastCountdown)
                {
                    _lastCountdown = remaining;
                    dailyCardCountdown.text = string.Format(Loc.Tr("Next hole in {0}"), remaining);
                }
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

            // One advance button, two modes: every tutorial stage offers the
            // next lesson; a journey level offers the next level once holed.
            // A gauntlet hole offers the next one as soon as it is settled —
            // holed OR out of strokes, because a failed hole still counts its
            // strokes and the week carries on.
            bool gauntletHoleDone = _modes.Mode == GameMode.Gauntlet && sim != null
                && (sim.IsHoled || sim.IsFailed) && _modes.HasNextGauntletHole;
            bool showNext = _modes.Mode == GameMode.Tutorial
                || (_modes.Mode == GameMode.Journey && sim != null && sim.IsHoled
                    && _modes.HasNextJourneyLevel)
                || gauntletHoleDone;
            nextLessonButton?.gameObject.SetActive(showNext);
            if (showNext && nextLessonButton != null)
            {
                var label = nextLessonButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = Loc.Tr(_modes.Mode == GameMode.Journey ? "Next level"
                        : _modes.Mode == GameMode.Gauntlet ? "Next hole"
                        : _modes.HasNextTutorialStage ? "Next lesson"
                        : "Finish tutorial");
                }
            }

            // The mulligan is a teaching tool — practice and tutorial only;
            // the bar reflows so five buttons share the row when Undo is gone.
            bool tutorial = _modes.Mode == GameMode.Tutorial;
            bool showUndo = _modes.Mode == GameMode.Practice || tutorial;
            undoButton?.gameObject.SetActive(showUndo);

            // Share arrives with the run worth sharing. It used to sit there
            // through every attempt only to answer "finish the hole first".
            // On a finished daily the closing card carries the primary Share,
            // so the bar stands down rather than offering it twice.
            bool cardUp = _modes.Mode == GameMode.Daily && sim != null && sim.IsHoled;

            // With an advance button in the row, the row has to make space for
            // it: sharing a tutorial lesson means nothing, the author ghost is
            // a teaching aid that only belongs in one, and pasting a
            // stranger's replay code belongs to neither.
            bool showShare = CanShare() && !cardUp && !(showNext && tutorial);
            bool showGhost = !showNext || tutorial;
            bool showWatch = !showNext;
            shareButton?.gameObject.SetActive(showShare);
            ghostButton?.gameObject.SetActive(showGhost);
            watchButton?.gameObject.SetActive(showWatch);

            int barShape = (showUndo ? 1 : 0) | (showShare ? 2 : 0) | (showNext ? 4 : 0)
                | (showGhost ? 8 : 0) | (showWatch ? 16 : 0);
            if (_barShape != barShape)
            {
                _barShape = barShape;
                LayoutBottomBar(showUndo, showShare, showGhost, showWatch, showNext);
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
                // The daily names its rating because the cup's rule follows
                // it: below Hard any touch drops, on Hard the speed threshold
                // stands. A day that changes that silently reads as physics
                // misbehaving.
                GameMode.Daily => string.Format(Loc.Tr("{0} · {1}"),
                    _modes.DailyModeLabel, Loc.Tr(gen.Difficulty.ToString())),
                GameMode.Practice => string.Format(Loc.Tr("Practice · {0}"), Loc.Tr(gen.Difficulty.ToString())),
                GameMode.Journey => string.Format(Loc.Tr("Level {0}/{1}"),
                    _modes.JourneyLevel + 1, JourneyConfig.Seeds.Length),
                GameMode.Gauntlet => string.Format(Loc.Tr("Gauntlet {0}/{1}  ·  {2} total"),
                    _modes.GauntletHole + 1, PuttSeed.Core.Daily.GauntletWeek.Length,
                    _modes.GauntletTotalStrokes),
                _ => string.Format(Loc.Tr("Tutorial {0}/{1}"), _modes.TutorialIndex + 1, TutorialConfig.Stages.Length),
            };

            // A themed day announces itself in the top bar: without it, ice
            // underfoot or a crosswind just reads as the game misbehaving.
            string mutator = _modes.MutatorLabel;
            if (mutator.Length > 0)
            {
                modeLabel = string.Format(Loc.Tr("{0} · {1}"), modeLabel, mutator);
            }

            // Say so while it is happening: the day is already answered and
            // this run is practice on the same hole.
            if (_modes.DailyAlreadyAnswered)
            {
                modeLabel = string.Format(Loc.Tr("{0} · {1}"), modeLabel, Loc.Tr("Practice"));
            }

            // Small print on the right; the numbers that matter are the hero
            // and the chip.
            int streak = _modes.Stats.Data.streak;
            string right = string.Format(Loc.Tr("{0} · {1}"),
                modeLabel, string.Format(Loc.Tr("Par {0}"), gen.Course.Par));
            if (streak > 0)
            {
                right = string.Format(Loc.Tr("{0} · {1}"), right,
                    string.Format(Loc.Tr("Streak {0}"), streak));
            }

            counterText.text = right;

            if (strokeText != null)
            {
                strokeText.text = $"{sim.Strokes}<size=26>/{sim.StrokeLimit}</size>";
            }

            RefreshParChip(sim.Strokes, gen.Course.Par);
            RefreshDailyCard(sim, gen.Course.Par);
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
        /// Score to par — the number a golfer reads before the stroke count.
        /// Colour says the same thing again for anyone who does not yet know
        /// what "+1" costs them.
        /// </summary>
        private void RefreshParChip(int strokes, int par)
        {
            if (parChipText == null)
            {
                return;
            }

            int diff = strokes - par;
            parChipText.text = diff == 0 ? "E" : diff > 0 ? $"+{diff}" : diff.ToString();
            parChipText.color = diff <= 0 ? UnderPar
                : diff == 1 ? UIStyle.Accent
                : OverPar;
        }

        /// <summary>
        /// The day's closing card: what you scored, what it did to your
        /// streaks, and how long until the next hole — the three things a
        /// player wants once the ball drops, gathered where they are already
        /// looking instead of spread over the menu and the stats panel.
        /// </summary>
        private void RefreshDailyCard(PuttSeed.Core.Sim.GolfSim sim, int par)
        {
            if (dailyCard == null)
            {
                return;
            }

            bool show = _modes.Mode == GameMode.Daily && sim.IsHoled;
            if (dailyCard.activeSelf != show)
            {
                if (show)
                {
                    UiFx.PopIn(this, dailyCard);
                }
                else
                {
                    dailyCard.SetActive(false);
                }
            }

            if (!show)
            {
                return;
            }

            var data = _modes.Stats.Data;
            var record = _modes.Stats.FindDay(_modes.ActiveDayNumber);
            int official = record != null && record.completed ? record.firstStrokes : sim.Strokes;
            int best = record != null && record.completed ? record.bestStrokes : sim.Strokes;

            if (dailyCardResult != null)
            {
                // The day's answer first, the personal best beside it — and
                // only when a retry actually beat the answer, so a clean first
                // finish is not made to look like two numbers.
                dailyCardResult.text = best < official
                    ? string.Format(Loc.Tr("{0} strokes · best {1}"), official, best)
                    : string.Format(Loc.Tr("{0} strokes"), official);
            }

            if (dailyCardStreak != null)
            {
                dailyCardStreak.text = string.Format(Loc.Tr("Streak {0} · par streak {1}"),
                    data.streak, data.parStreak);
            }
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
        /// Lays out the bottom bar by WEIGHT, not by equal shares. Five
        /// identical buttons said that leaving the course and pasting a
        /// stranger's replay code matter as much as the button pressed dozens
        /// of times a day, so Retry now takes better than twice a neighbour's
        /// width. Share only appears once there is a run to share — before
        /// that it existed only to refuse.
        /// </summary>
        private void LayoutBottomBar(bool withUndo, bool withShare, bool withGhost,
            bool withWatch, bool withNext)
        {
            var buttons = new System.Collections.Generic.List<Button?>(6) { menuButton, retryButton };
            var weights = new System.Collections.Generic.List<float>(6)
            {
                1f,
                withNext ? 1.7f : 2.3f, // the advance button takes the lead when there is one
            };

            if (withShare)
            {
                buttons.Add(shareButton);
                weights.Add(1.15f);
            }

            if (withGhost)
            {
                buttons.Add(ghostButton);
                weights.Add(1f);
            }

            if (withWatch)
            {
                buttons.Add(watchButton);
                weights.Add(1f);
            }

            if (withUndo)
            {
                buttons.Add(undoButton);
                weights.Add(1f);
            }

            if (withNext)
            {
                buttons.Add(nextLessonButton);
                weights.Add(2.2f);
            }

            const float margin = 0.02f;
            const float gap = 0.012f;
            float total = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                total += weights[i];
            }

            float unit = (1f - 2f * margin - (buttons.Count - 1) * gap) / total;

            float x0 = margin;
            for (int i = 0; i < buttons.Count; i++)
            {
                float width = unit * weights[i];
                if (buttons[i] != null)
                {
                    var rect = (RectTransform)buttons[i]!.transform;
                    rect.anchorMin = new Vector2(x0, 0.016f);
                    rect.anchorMax = new Vector2(x0 + width, 0.077f);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                }

                x0 += width + gap;
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

        /// <summary>
        /// Whether Share has anything to give right now — the same three cases
        /// <see cref="OnShare"/> acts on, so the bar can never hide a button
        /// that had something to offer. A practice course shares as an
        /// invitation before it is finished, and a gauntlet whose last hole ran
        /// out of strokes still finished its week.
        /// </summary>
        private bool CanShare()
        {
            var sim = _runner.Sim;
            if (sim == null)
            {
                return false;
            }

            if (sim.IsHoled || _modes.Mode == GameMode.Practice)
            {
                return true;
            }

            return _modes.Mode == GameMode.Gauntlet && !_modes.HasNextGauntletHole && sim.IsFailed;
        }

        private void OnShare()
        {
            // A finished gauntlet shares the WEEK, not the hole: one code
            // carries all seven, and the seeds come back from the week index.
            if (_modes.Mode == GameMode.Gauntlet && !_modes.HasNextGauntletHole
                && _runner.Sim != null && (_runner.Sim.IsHoled || _runner.Sim.IsFailed))
            {
                var weekCode = _modes.BuildGauntletCode();
                string weekText = string.Format(
                    "PUTTSEED week {0} — {1} strokes. Watch: {2}",
                    _modes.GauntletWeekIndex, _modes.GauntletTotalStrokes, weekCode);
                GUIUtility.systemCopyBuffer = weekText;
                ShowToast(Loc.Tr(NativeShare.Share(weekText) ? "Sharing…" : "Copied!"));
                return;
            }

            var sim = _runner.Sim;
            if (sim == null || !sim.IsHoled)
            {
                // Practice courses are shareable BEFORE finishing: a zero-shot
                // code is a course invitation, not a replay.
                if (sim != null && _modes.Mode == GameMode.Practice && _runner.Generation != null)
                {
                    var courseCode = ReplayCodec.Encode(_runner.Seed,
                        System.Array.Empty<PuttSeed.Core.Sim.ShotInput>(),
                        System.Array.Empty<int>(), _modes.ShareVersion);
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

            var code = ReplayCodec.Encode(_runner.Seed, shots, _modes.ShareShotClocks(), _modes.ShareVersion);
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
