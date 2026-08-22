#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Renders the player ball at the interpolated sim position with a fading
    /// trail. Pure presentation: reads positions, never touches the sim.
    /// </summary>
    public sealed class BallView : MonoBehaviour
    {
        private static readonly Color TrailIce = new Color(0.68f, 0.85f, 1f, 0.55f);
        private static readonly Color TrailSand = new Color(0.9f, 0.82f, 0.6f, 0.5f);

        /// <summary>
        /// The equipped trail's tint — the color the trail rests at. Ice and
        /// sand still take over while the ball is on them: those cues carry
        /// information, and information outranks cosmetics.
        /// </summary>
        private Color _trailBase = new Color(1f, 1f, 1f, 0.5f);

        private SimRunner _runner = null!;
        private TrailRenderer _trail = null!;
        private float _squash;
        private float _squashAngle;
        private float _popIn;
        private float _sink;
        private TrailStyle _trailStyle;
        private readonly System.Collections.Generic.List<MeshRenderer> _cosmetics =
            new System.Collections.Generic.List<MeshRenderer>();

        // The bubble pool (TrailStyle.Bubbles): a dozen discs shed along the
        // path and recycled. Sized so even an ice glide never runs dry.
        private const int BubbleCount = 12;
        private Transform[] _bubbles = System.Array.Empty<Transform>();
        private MeshRenderer[] _bubbleRenderers = System.Array.Empty<MeshRenderer>();
        private readonly float[] _bubbleAges = new float[BubbleCount];
        private readonly float[] _bubbleLives = new float[BubbleCount];
        private readonly float[] _bubbleSizes = new float[BubbleCount];
        private MaterialPropertyBlock? _bubbleBlock;
        private float _bubbleGap;
        private int _nextBubble;
        private readonly System.Random _bubbleRng = new System.Random(77);
        private Transform _body = null!;
        private Transform? _spin;
        private float _spinAngle;
        private Color _trailColor = new Color(1f, 1f, 1f, 0.5f);

        private MeshRenderer _renderer = null!;

        /// <summary>Creates the ball visuals and subscribes to run resets.</summary>
        public void Initialize(SimRunner runner)
            => Initialize(runner, BallSkins.All[0], BallTrails.All[0]);

        /// <summary>Creates the ball visuals for the equipped cosmetics.</summary>
        public void Initialize(SimRunner runner, BallSkinDef skin, BallTrailDef trail)
        {
            _runner = runner;
            var ballColor = skin.Color;
            _trailBase = trail.Color;
            _trailColor = trail.Color;
            _trailStyle = trail.Style;

            // The disc lives on a rotatable child so impact squash can align
            // to the contact axis without swinging the shadow or the trail.
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(transform, false);
            _body = bodyGo.transform;

            // A dark contour one layer behind the disc. The ball is the
            // smallest thing on screen and shares a hue band with the bumpers
            // on some skins; a rim wins that contrast fight on every surface,
            // and it squashes with the body because it rides the same
            // transform.
            var rimGo = new GameObject("Rim");
            rimGo.transform.SetParent(bodyGo.transform, false);
            rimGo.transform.localPosition = new Vector3(0f, 0f, 0.004f);
            rimGo.AddComponent<MeshFilter>().sharedMesh =
                MeshFactory.Disc(Vector2.zero, 0.118f, new Color(0.09f, 0.12f, 0.10f, 0.55f));
            var rimRenderer = rimGo.AddComponent<MeshRenderer>();
            rimRenderer.sharedMaterial = PaletteMaterials.Shared;
            rimRenderer.sortingOrder = SortingLayers.Ball;
            rimRenderer.enabled = false; // follows the body's first-course rule

            var mesh = MeshFactory.Disc(Vector2.zero, 0.1f, ballColor);
            bodyGo.AddComponent<MeshFilter>().sharedMesh = mesh;

            // The pattern rides the BODY, so impact squash bends it with the
            // ball — a stripe that stayed rigid while the disc squashed would
            // read as a sticker floating over it. Renderers start disabled and
            // follow the same first-course rule as the disc itself.
            if (skin.Pattern == BallPattern.Stripe)
            {
                var stripeGo = new GameObject("Stripe");
                stripeGo.transform.SetParent(bodyGo.transform, false);
                stripeGo.transform.localPosition = new Vector3(0f, 0f, -0.001f);
                stripeGo.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Quad(
                    new Vector2(-0.093f, -0.026f), new Vector2(0.093f, 0.026f), skin.PatternColor);
                var stripeRenderer = stripeGo.AddComponent<MeshRenderer>();
                stripeRenderer.sharedMaterial = PaletteMaterials.Shared;
                stripeRenderer.sortingOrder = SortingLayers.Ball;
                stripeRenderer.enabled = false;
                _cosmetics.Add(stripeRenderer);
            }
            else if (skin.Pattern == BallPattern.Dots)
            {
                var spots = new[]
                {
                    Vector2.zero,
                    new Vector2(0.055f, 0.02f),
                    new Vector2(-0.055f, 0.02f),
                    new Vector2(0.028f, -0.052f),
                    new Vector2(-0.028f, -0.052f),
                };
                foreach (var at in spots)
                {
                    var dotGo = new GameObject("PatternDot");
                    dotGo.transform.SetParent(bodyGo.transform, false);
                    dotGo.transform.localPosition = new Vector3(0f, 0f, -0.001f);
                    dotGo.AddComponent<MeshFilter>().sharedMesh =
                        MeshFactory.Disc(at, 0.021f, skin.PatternColor);
                    var dotRenderer = dotGo.AddComponent<MeshRenderer>();
                    dotRenderer.sharedMaterial = PaletteMaterials.Shared;
                    dotRenderer.sortingOrder = SortingLayers.Ball;
                    dotRenderer.enabled = false;
                    _cosmetics.Add(dotRenderer);
                }
            }
            _renderer = bodyGo.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = PaletteMaterials.Shared;
            _renderer.sortingOrder = SortingLayers.Ball;
            // Invisible until the first course is actually loaded — a ball
            // floating over an empty field must never render.
            _renderer.enabled = false;

            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.material = PaletteMaterials.Shared;
            _trail.startColor = new Color(1f, 1f, 1f, 0.5f);
            _trail.endColor = new Color(1f, 1f, 1f, 0f);
            _trail.sortingOrder = SortingLayers.BallTrail;
            if (_trailStyle == TrailStyle.Comet)
            {
                // Short, wide, dying to a point: a head with a tail rather
                // than a ribbon.
                _trail.time = 0.3f;
                _trail.startWidth = 0.15f;
                _trail.endWidth = 0f;
            }
            else
            {
                _trail.time = 0.6f;
                _trail.startWidth = 0.09f;
                _trail.endWidth = 0.01f;
            }

            _trail.enabled = false;
            if (_trailStyle == TrailStyle.Bubbles)
            {
                BuildBubblePool();
            }

            // Three faint dimples rotating with speed — sells the roll on an
            // otherwise flat disc (stylized: rate follows speed, not heading).
            var spinGo = new GameObject("Spin");
            spinGo.transform.SetParent(transform, false);
            spinGo.transform.localPosition = new Vector3(0f, 0f, -0.004f);
            _spin = spinGo.transform;
            var dimple = new Color(0.8f, 0.8f, 0.76f);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 2f * Mathf.PI / 3f;
                var dotGo = new GameObject($"Dimple{i}");
                dotGo.transform.SetParent(spinGo.transform, false);
                dotGo.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 0.048f, Mathf.Sin(angle) * 0.048f, 0f);
                dotGo.AddComponent<MeshFilter>().sharedMesh =
                    MeshFactory.Disc(Vector2.zero, 0.02f, dimple);
                var dotRenderer = dotGo.AddComponent<MeshRenderer>();
                dotRenderer.sharedMaterial = PaletteMaterials.Shared;
                dotRenderer.sortingOrder = SortingLayers.Ball;
            }

            // A fixed up-left highlight completes the sphere read (it stays
            // put while the dimples spin underneath it).
            var highlightGo = new GameObject("Highlight");
            highlightGo.transform.SetParent(transform, false);
            highlightGo.transform.localPosition = new Vector3(-0.032f, 0.032f, -0.006f);
            highlightGo.AddComponent<MeshFilter>().sharedMesh =
                MeshFactory.Disc(Vector2.zero, 0.028f, new Color(1f, 1f, 1f, 0.75f));
            var highlightRenderer = highlightGo.AddComponent<MeshRenderer>();
            highlightRenderer.sharedMaterial = PaletteMaterials.Shared;
            highlightRenderer.sortingOrder = SortingLayers.Ball;

            // Soft drop shadow trailing the ball down-right, one layer behind.
            var shadowGo = new GameObject("BallShadow");
            shadowGo.transform.SetParent(transform, false);
            shadowGo.transform.localPosition = new Vector3(0.04f, -0.05f, 0.012f);
            shadowGo.AddComponent<MeshFilter>().sharedMesh =
                MeshFactory.Disc(Vector2.zero, 0.1f, PaletteMaterials.Shadow);
            var shadowRenderer = shadowGo.AddComponent<MeshRenderer>();
            shadowRenderer.sharedMaterial = PaletteMaterials.Shared;
            shadowRenderer.sortingOrder = SortingLayers.BallTrail;
            shadowGo.SetActive(false);

            runner.RunReset += () =>
            {
                _renderer.enabled = true;
                rimRenderer.enabled = true;
                for (int i = 0; i < _cosmetics.Count; i++)
                {
                    _cosmetics[i].enabled = true; // patterns follow the body's rule
                }

                _trail.enabled = _trailStyle != TrailStyle.Bubbles;
                _trail.Clear();
                _sink = 0f; // a retry starts with a ball, not a hole
                shadowGo.SetActive(true);
            };
        }

        /// <summary>
        /// Impact juice: a brief squash pulse, compressed along the impact
        /// axis (the ball's travel direction at contact) — side hits finally
        /// read as side hits.
        /// </summary>
        public void Squash(Vector2 impactDirection)
        {
            _squash = 1f;
            if (impactDirection.sqrMagnitude > 0.0001f)
            {
                _squashAngle = Mathf.Atan2(impactDirection.y, impactDirection.x) * Mathf.Rad2Deg;
            }
        }

        /// <summary>Scale-in pop after a teleport (the water reset).</summary>
        public void PopIn() => _popIn = 1f;

        /// <summary>
        /// The ball drops into the cup. Everything about the capture used to
        /// happen AROUND the ball — a ring, a flash, a zoom, confetti, a
        /// slow-motion replay — while the ball itself simply stopped on top of
        /// the hole and stayed there. The one moment the whole game is about
        /// had no motion of its own.
        /// </summary>
        public void Sink() => _sink = 1f;

        private void LateUpdate()
        {
            if (_runner == null || _runner.Sim == null)
            {
                return;
            }

            var p = _runner.BallRenderPosition;
            transform.position = new Vector3(p.x, p.y, -0.06f);

            if (_spin != null)
            {
                float speed = FixView.ToVector2(_runner.Sim!.Ball.Velocity).magnitude;
                _spinAngle -= speed * Time.deltaTime * 340f; // deg — wheel-rate at r=0.1
                _spin.localEulerAngles = new Vector3(0f, 0f, _spinAngle);
            }

            float pop = 1f;
            if (_popIn > 0f)
            {
                _popIn = Mathf.Max(0f, _popIn - Time.deltaTime * 5f);
                pop = Mathf.SmoothStep(0.25f, 1f, 1f - _popIn);
            }

            // Down the hole: a quick shrink with a moment of hesitation at the
            // lip, which is what a real putt looks like when it drops.
            if (_sink > 0f)
            {
                _sink = Mathf.Max(0f, _sink - Time.deltaTime * 5.5f);
                float fallen = 1f - _sink;
                pop *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((fallen - 0.15f) / 0.85f));
                _trail.enabled = false;
            }

            transform.localScale = Vector3.one * pop;

            if (_squash > 0f)
            {
                _squash = Mathf.Max(0f, _squash - Time.deltaTime * 8f);
                float k = _squash * 0.22f;
                // Body-local: compressed along the impact axis, bulging across.
                _body.localEulerAngles = new Vector3(0f, 0f, _squashAngle);
                _body.localScale = new Vector3(1f - k, 1f + k, 1f);
            }
            else
            {
                _body.localEulerAngles = Vector3.zero;
                _body.localScale = Vector3.one;
            }

            // The trail borrows the ground's tone: icy blue on ice, warm tan
            // in sand, cream elsewhere — eased so transitions never snap.
            var target = InZone(_runner.Generation?.Course.IceZones) ? TrailIce
                : InZone(_runner.Generation?.Course.SandZones) ? TrailSand
                : _trailBase;
            _trailColor = Color.Lerp(_trailColor, target, Time.deltaTime * 6f);
            _trail.startColor = _trailColor;

            if (_trailStyle == TrailStyle.Bubbles)
            {
                UpdateBubbles();
            }
        }

        private void BuildBubblePool()
        {
            _bubbleBlock = new MaterialPropertyBlock();
            _bubbles = new Transform[BubbleCount];
            _bubbleRenderers = new MeshRenderer[BubbleCount];
            var bubbleMesh = MeshFactory.Ring(Vector2.zero, 0.62f, 1f, Color.white, 14);
            for (int i = 0; i < BubbleCount; i++)
            {
                var go = new GameObject("Bubble");
                go.transform.SetParent(transform.parent, false); // world-anchored, not ball-anchored
                go.AddComponent<MeshFilter>().sharedMesh = bubbleMesh;
                var bubbleRenderer = go.AddComponent<MeshRenderer>();
                bubbleRenderer.sharedMaterial = PaletteMaterials.Shared;
                bubbleRenderer.sortingOrder = SortingLayers.BallTrail;
                go.SetActive(false);
                _bubbles[i] = go.transform;
                _bubbleRenderers[i] = bubbleRenderer;
                _bubbleAges[i] = float.MaxValue;
            }
        }

        /// <summary>
        /// Sheds a hollow ring every fifth of a unit of travel and lets each
        /// swell and pop. The rings are parented to the WORLD, not the ball —
        /// a bubble that followed the ball would just be a lumpy ribbon.
        /// </summary>
        private void UpdateBubbles()
        {
            if (_bubbleBlock == null || _runner.Sim == null)
            {
                return;
            }

            float speed = FixView.ToVector2(_runner.Sim.Ball.Velocity).magnitude;
            _bubbleGap += speed * Time.deltaTime;
            if (_bubbleGap > 0.2f && speed > 0.4f && _sink <= 0f)
            {
                _bubbleGap = 0f;
                int i = _nextBubble;
                _nextBubble = (_nextBubble + 1) % BubbleCount;
                _bubbleAges[i] = 0f;
                _bubbleLives[i] = 0.38f + (float)_bubbleRng.NextDouble() * 0.22f;
                _bubbleSizes[i] = 0.028f + (float)_bubbleRng.NextDouble() * 0.03f;
                _bubbles[i].position = new Vector3(
                    transform.position.x + ((float)_bubbleRng.NextDouble() - 0.5f) * 0.06f,
                    transform.position.y + ((float)_bubbleRng.NextDouble() - 0.5f) * 0.06f,
                    -0.055f);
                _bubbles[i].gameObject.SetActive(true);
            }

            for (int i = 0; i < BubbleCount; i++)
            {
                if (_bubbleAges[i] >= _bubbleLives[i])
                {
                    if (_bubbles[i].gameObject.activeSelf)
                    {
                        _bubbles[i].gameObject.SetActive(false);
                    }

                    continue;
                }

                _bubbleAges[i] += Time.deltaTime;
                float k = Mathf.Clamp01(_bubbleAges[i] / _bubbleLives[i]);
                _bubbles[i].localScale = Vector3.one * (_bubbleSizes[i] * (0.7f + 0.7f * k));
                _bubbleBlock.SetColor("_Color", new Color(
                    _trailColor.r, _trailColor.g, _trailColor.b, (1f - k) * 0.8f));
                _bubbleRenderers[i].SetPropertyBlock(_bubbleBlock);
            }
        }

        private bool InZone(PuttSeed.Core.Sim.ZonePolygon[]? zones)
        {
            var sim = _runner.Sim;
            if (zones == null || sim == null)
            {
                return false;
            }

            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i].Contains(sim.Ball.Position))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
