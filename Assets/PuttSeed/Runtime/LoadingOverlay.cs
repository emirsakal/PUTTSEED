#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Full-screen loading cover with a little putt vignette: a club swings,
    /// the ball rolls toward the cup while loading runs, and on Hide it drops
    /// in before the cover closes. The hierarchy is scene-authored (baked by
    /// PuttSeed → Rebuild Scenes; reskinnable in the Inspector) — this
    /// component only drives the animation and the trailing dots.
    /// </summary>
    public sealed class LoadingOverlay : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            Swing,
            Rolling,
            Finishing,
            Sinking,
        }

        [Header("Scene-authored UI (assigned by PuttSeed → Rebuild Scenes)")]
        public GameObject? root;
        public Text? label;
        public RectTransform? club;
        public RectTransform? ball;
        public RectTransform? hole;

        [Header("Animation tuning")]
        [Tooltip("Club wind-up angle (degrees) at the start of the swing.")]
        public float clubWindUpAngle = 40f;
        [Tooltip("Club follow-through angle (degrees) at the end of the swing.")]
        public float clubThroughAngle = -25f;
        [Tooltip("Seconds of club swing before the ball launches.")]
        public float swingSeconds = 0.28f;
        [Tooltip("While loading, the ball approaches but never passes this fraction of the distance.")]
        [Range(0.5f, 0.95f)] public float rollCapFraction = 0.85f;
        [Tooltip("Minimum seconds the whole putt vignette should be visible. Blocking " +
            "loads render almost no frames, so the close-out stretches the remaining " +
            "roll to fill this — the 'fake fill' that stops the ball teleporting.")]
        public float minCinematicSeconds = 1.5f;
        [Tooltip("Fastest allowed final roll into the cup (used when the roll already animated).")]
        public float minFinishSeconds = 0.35f;
        [Tooltip("Seconds for the ball to sink out of sight in the cup.")]
        public float sinkSeconds = 0.15f;

        private string _baseText = "";
        private float _dotTimer;
        private Phase _phase = Phase.Idle;
        private float _phaseTime;
        private float _shownTime;
        private float _progress;
        private float _finishStartProgress;
        private float _finishStartClubAngle;
        private float _finishSwingSeconds;
        private float _finishRollSeconds;
        private Vector2 _ballStart;
        private Vector2 _holePosition;
        private bool _positionsCached;

        /// <summary>True while the cover is visible (including the finish putt).</summary>
        public bool IsShown => root != null && root.activeSelf;

        /// <summary>Shows the cover with a message and restarts the putt vignette.</summary>
        public void Show(string text)
        {
            _baseText = text;
            _dotTimer = 0f;
            if (label != null)
            {
                label.text = text;
            }

            root?.SetActive(true);

            if (ball != null && hole != null)
            {
                if (!_positionsCached)
                {
                    _ballStart = ball.anchoredPosition;
                    _holePosition = hole.anchoredPosition;
                    _positionsCached = true;
                }

                ball.anchoredPosition = _ballStart;
                ball.localScale = Vector3.one;
                _progress = 0f;
                _phaseTime = 0f;
                _shownTime = 0f;
                _phase = Phase.Swing;
                if (club != null)
                {
                    club.localEulerAngles = new Vector3(0f, 0f, clubWindUpAngle);
                }
            }
        }

        /// <summary>
        /// Requests closing: the ball finishes its putt, drops into the cup,
        /// and only then does the cover deactivate. Instant when the vignette
        /// references are absent (old scenes).
        /// </summary>
        public void Hide()
        {
            if (!IsShown)
            {
                return;
            }

            if (ball == null || hole == null || _phase == Phase.Idle)
            {
                root?.SetActive(false);
                _phase = Phase.Idle;
                return;
            }

            if (_phase != Phase.Finishing && _phase != Phase.Sinking)
            {
                _finishStartProgress = _progress;
                _finishStartClubAngle = club != null ? SignedZ(club) : clubThroughAngle;

                // Fake fill: if the vignette barely got to animate (blocking
                // load starved the frames), stretch the close-out so the ball
                // visibly ROLLS the whole way instead of teleporting.
                bool swingPending = _phase == Phase.Swing;
                _finishSwingSeconds = swingPending ? swingSeconds : 0f;
                _finishRollSeconds = Mathf.Max(minFinishSeconds,
                    minCinematicSeconds - _shownTime - _finishSwingSeconds - sinkSeconds);

                _phaseTime = 0f;
                _phase = Phase.Finishing;
            }
        }

        private void Update()
        {
            if (!IsShown)
            {
                return;
            }

            if (label != null)
            {
                _dotTimer += Time.unscaledDeltaTime;
                int dots = 1 + (int)(_dotTimer * 2f) % 3;
                label.text = _baseText + new string('.', dots);
            }

            if (ball == null || hole == null || _phase == Phase.Idle)
            {
                return;
            }

            _phaseTime += Time.unscaledDeltaTime;
            _shownTime += Time.unscaledDeltaTime;
            switch (_phase)
            {
                case Phase.Swing:
                    float swing = Mathf.Clamp01(_phaseTime / swingSeconds);
                    if (club != null)
                    {
                        // Ease-in swing: slow lift, fast strike.
                        float eased = swing * swing;
                        club.localEulerAngles = new Vector3(0f, 0f,
                            Mathf.Lerp(clubWindUpAngle, clubThroughAngle, eased));
                    }

                    if (swing >= 1f)
                    {
                        _phase = Phase.Rolling;
                        _phaseTime = 0f;
                    }

                    break;

                case Phase.Rolling:
                    // Approach the cup asymptotically; never arrive while loading.
                    _progress = rollCapFraction * (1f - Mathf.Exp(-1.1f * _phaseTime));
                    ball.anchoredPosition = Vector2.Lerp(_ballStart, _holePosition, _progress);
                    break;

                case Phase.Finishing:
                    // Part 1: finish the stroke first (ball waits for contact).
                    if (_phaseTime < _finishSwingSeconds)
                    {
                        if (club != null)
                        {
                            float s = _phaseTime / _finishSwingSeconds;
                            club.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(
                                _finishStartClubAngle, clubThroughAngle, s * s));
                        }

                        break;
                    }

                    if (club != null)
                    {
                        club.localEulerAngles = new Vector3(0f, 0f, clubThroughAngle);
                    }

                    // Part 2: roll the remaining distance smoothly — eased so
                    // the ball glides and settles into the cup, never jumps.
                    float u = Mathf.Clamp01((_phaseTime - _finishSwingSeconds) / _finishRollSeconds);
                    float eased = u * u * (3f - 2f * u); // smoothstep
                    _progress = Mathf.Lerp(_finishStartProgress, 1f, eased);
                    ball.anchoredPosition = Vector2.Lerp(_ballStart, _holePosition, _progress);
                    if (u >= 1f)
                    {
                        _phase = Phase.Sinking;
                        _phaseTime = 0f;
                    }

                    break;

                case Phase.Sinking:
                    float sink = Mathf.Clamp01(_phaseTime / sinkSeconds);
                    ball.localScale = Vector3.one * (1f - sink);
                    if (sink >= 1f)
                    {
                        _phase = Phase.Idle;
                        root?.SetActive(false);
                    }

                    break;
            }
        }

        private static float SignedZ(RectTransform rect)
        {
            float z = rect.localEulerAngles.z;
            return z > 180f ? z - 360f : z;
        }
    }
}
