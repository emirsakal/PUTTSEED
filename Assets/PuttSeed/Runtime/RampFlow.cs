#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// One bright chevron gliding down a ramp, the way the ramp pushes.
    ///
    /// The static arrows say which way the slope points; this says that it
    /// KEEPS pushing — a conveyor read, bought with a single moving glyph
    /// instead of an animated texture. It fades in and out at the zone's ends
    /// so the loop never pops, and it is skipped entirely under reduced
    /// motion, where the static arrows keep carrying the direction.
    /// </summary>
    public sealed class RampFlow : MonoBehaviour
    {
        private const float Speed = 0.55f;      // fraction of the ramp per second
        private const float Alpha = 0.85f;

        private Vector2[] _quad = System.Array.Empty<Vector2>();
        private MeshRenderer[] _strokes = System.Array.Empty<MeshRenderer>();
        private MaterialPropertyBlock? _block;
        private float _phase;
        private bool _travelAlongU;
        private bool _travelForward;

        /// <summary>Builds the glider for one ramp quad and its push direction.</summary>
        public void Initialize(Vector2[] quad, Vector2 direction)
        {
            _quad = quad;
            _block = new MaterialPropertyBlock();
            _phase = (transform.GetSiblingIndex() * 0.37f) % 1f;

            // The glide has to follow the PUSH, and the quad's parameter axes
            // owe the push nothing — vertex order is the generator's business.
            // The first version slid along u regardless, which on some ramps
            // meant a chevron pointing uphill-left while sailing downhill-
            // right: an arrow that lies about the one thing it exists to say.
            // So measure both axes against the acceleration and travel along
            // the aligned one, in the aligned sense.
            var uAxis = CourseRenderer.Bilerp(quad, 1f, 0.5f) - CourseRenderer.Bilerp(quad, 0f, 0.5f);
            var vAxis = CourseRenderer.Bilerp(quad, 0.5f, 1f) - CourseRenderer.Bilerp(quad, 0.5f, 0f);
            float alongU = Vector2.Dot(uAxis.normalized, direction);
            float alongV = Vector2.Dot(vAxis.normalized, direction);
            _travelAlongU = Mathf.Abs(alongU) >= Mathf.Abs(alongV);
            _travelForward = (_travelAlongU ? alongU : alongV) >= 0f;

            // The chevron is built centred on the origin so the OBJECT can be
            // placed and rotated — a mesh carrying absolute coordinates spins
            // around the world origin, which is the stripe lesson again.
            const float size = 0.16f;
            var tip = Vector2.zero;
            var back = -Vector2.right * size;
            var side = Vector2.up;
            var color = new Color(1f, 1f, 1f, Alpha);
            _strokes = new MeshRenderer[2];
            _strokes[0] = MeshFactory.CreateMeshObject(transform, "FlowA",
                MeshFactory.Outline(new[] { back + side * size * 0.7f, tip }, 0.04f, color,
                    closed: false), 0f).GetComponent<MeshRenderer>();
            _strokes[1] = MeshFactory.CreateMeshObject(transform, "FlowB",
                MeshFactory.Outline(new[] { back - side * size * 0.7f, tip }, 0.04f, color,
                    closed: false), 0f).GetComponent<MeshRenderer>();
            transform.localEulerAngles = new Vector3(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void LateUpdate()
        {
            if (_block == null || _quad.Length != 4)
            {
                return;
            }

            _phase = (_phase + Time.deltaTime * Speed) % 1f;
            float t = _travelForward ? _phase : 1f - _phase;
            var p = _travelAlongU
                ? CourseRenderer.Bilerp(_quad, t, 0.5f)
                : CourseRenderer.Bilerp(_quad, 0.5f, t);
            transform.position = new Vector3(p.x, p.y, -0.0064f);
            _block.SetColor("_Color", new Color(1f, 1f, 1f, WindStreaks.FadeFor(_phase)));
            for (int i = 0; i < _strokes.Length; i++)
            {
                _strokes[i].SetPropertyBlock(_block);
            }
        }
    }
}
