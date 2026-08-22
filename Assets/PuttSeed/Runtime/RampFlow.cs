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

        /// <summary>Builds the glider for one ramp quad and its push direction.</summary>
        public void Initialize(Vector2[] quad, Vector2 direction)
        {
            _quad = quad;
            _block = new MaterialPropertyBlock();
            _phase = (transform.GetSiblingIndex() * 0.37f) % 1f;

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
            var p = CourseRenderer.Bilerp(_quad, _phase, 0.5f);
            transform.position = new Vector3(p.x, p.y, -0.0064f);
            _block.SetColor("_Color", new Color(1f, 1f, 1f, WindStreaks.FadeFor(_phase)));
            for (int i = 0; i < _strokes.Length; i++)
            {
                _strokes[i].SetPropertyBlock(_block);
            }
        }
    }
}
