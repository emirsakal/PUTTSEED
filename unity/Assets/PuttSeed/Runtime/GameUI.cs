#nullable enable
using PuttSeed.Core.Replay;
using UnityEngine;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// The GDD's minimal UI, built entirely in code: stroke/par counter,
    /// status line, retry, share (copies the PUTT- code of the finished run),
    /// import field (plays any pasted code as a ghost, loading its course if
    /// needed), and an author-ghost toggle. Pure presentation + boundary calls.
    /// </summary>
    public sealed class GameUI : MonoBehaviour
    {
        private SimRunner _runner = null!;
        private CourseRenderer _courseRenderer = null!;
        private Camera _camera = null!;

        private Text _counter = null!;
        private Text _status = null!;
        private Text _toast = null!;
        private InputField _importField = null!;
        private float _toastUntil;

        /// <summary>Builds the canvas hierarchy and wires events.</summary>
        public void Initialize(SimRunner runner, CourseRenderer courseRenderer, Camera cam)
        {
            _runner = runner;
            _courseRenderer = courseRenderer;
            _camera = cam;

            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();

            _counter = CreateText(canvasGo.transform, "Counter", new Vector2(0.02f, 0.93f), new Vector2(0.7f, 0.99f), 44, TextAnchor.UpperLeft);
            _status = CreateText(canvasGo.transform, "Status", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.72f), 72, TextAnchor.MiddleCenter);
            _toast = CreateText(canvasGo.transform, "Toast", new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.2f), 34, TextAnchor.MiddleCenter);

            CreateButton(canvasGo.transform, "Retry", new Vector2(0.02f, 0.02f), new Vector2(0.24f, 0.09f), OnRetry);
            CreateButton(canvasGo.transform, "Share", new Vector2(0.26f, 0.02f), new Vector2(0.48f, 0.09f), OnShare);
            CreateButton(canvasGo.transform, "Ghost", new Vector2(0.5f, 0.02f), new Vector2(0.72f, 0.09f), OnToggleAuthorGhost);

            _importField = CreateInputField(canvasGo.transform, new Vector2(0.02f, 0.1f), new Vector2(0.72f, 0.155f));
            CreateButton(canvasGo.transform, "Watch", new Vector2(0.74f, 0.1f), new Vector2(0.98f, 0.155f), OnImport);

            runner.StateChanged += Refresh;
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
            if (sim == null || gen == null)
            {
                _counter.text = "generating…";
                return;
            }

            _counter.text = $"Strokes {sim.Strokes}/{sim.StrokeLimit}   Par {gen.Course.Par}   {gen.Difficulty}";
            _status.text = sim.IsHoled
                ? SuccessLine(sim.Strokes, gen.Course.Par)
                : sim.IsFailed ? "Out of strokes — retry!" : "";
        }

        private static string SuccessLine(int strokes, int par)
            => strokes <= par - 1 ? "Under par — brilliant!"
             : strokes == par ? "Par — well played!"
             : "Holed!";

        private void OnRetry()
        {
            _runner.Retry();
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
            var text = _importField.text.Trim();
            int at = text.IndexOf("PUTT-", System.StringComparison.Ordinal);
            if (at < 0 || !ReplayCodec.TryDecode(ExtractCode(text, at), out var seed, out var shots))
            {
                ShowToast("Not a valid PUTT- code.");
                return;
            }

            if (seed != _runner.Seed)
            {
                _runner.LoadSeed(seed);
                _courseRenderer.Rebuild(_runner.Generation!.Course);
                CameraFramer.Frame(_camera, _runner.Generation.Course);
                ShowToast("Loaded that day's course; ghost playing.");
            }
            else
            {
                ShowToast("Ghost playing.");
            }

            _runner.AddGhost(shots, "import");
        }

        private static string ExtractCode(string text, int start)
        {
            int end = start;
            while (end < text.Length && !char.IsWhiteSpace(text[end]))
            {
                end++;
            }

            return text.Substring(start, end - start);
        }

        private void ShowToast(string message)
        {
            _toast.text = message;
            _toastUntil = Time.unscaledTime + 2.5f;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private static Font UiFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int size, TextAnchor align)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = UiFont();
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
        {
            var rect = CreateRect(parent, $"Button{label}", anchorMin, anchorMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.45f);
            var button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            var text = CreateText(rect, "Label", Vector2.zero, Vector2.one, 36, TextAnchor.MiddleCenter);
            text.text = label;
        }

        private static InputField CreateInputField(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = CreateRect(parent, "ImportField", anchorMin, anchorMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.12f);
            var field = rect.gameObject.AddComponent<InputField>();

            var placeholder = CreateText(rect, "Placeholder", Vector2.zero, Vector2.one, 30, TextAnchor.MiddleLeft);
            placeholder.text = "  paste PUTT- code…";
            placeholder.color = new Color(1f, 1f, 1f, 0.4f);
            placeholder.raycastTarget = false;

            var text = CreateText(rect, "Text", Vector2.zero, Vector2.one, 30, TextAnchor.MiddleLeft);
            text.supportRichText = false;
            text.raycastTarget = false;

            field.textComponent = text;
            field.placeholder = placeholder;
            return field;
        }
    }
}
