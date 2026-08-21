#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Additive camera effects: a decaying micro-shake on hard impacts and a
    /// brief zoom toward the cup on capture. While idle it tracks the framed
    /// camera as its baseline, so CameraFramer stays the source of truth.
    /// </summary>
    public sealed class CameraJuice : MonoBehaviour
    {
        private const float ZoomInTime = 0.35f;
        private const float ZoomHoldTime = 0.9f;
        private const float ZoomOutTime = 0.35f;
        private const float ZoomScale = 0.86f;

        private Camera _cam = null!;
        private Vector3 _basePos;
        private float _baseSize;

        private float _shakeTime;
        private float _shakeDuration = 1f;
        private float _shakeAmplitude;

        private bool _zooming;
        private float _zoomTime;
        private Vector2 _zoomTarget;
        private float _zoomScale = ZoomScale;
        private float _zoomIn = ZoomInTime;
        private float _zoomHold = ZoomHoldTime;
        private float _zoomOut = ZoomOutTime;
        private float _zoomPull = 0.4f;

        private bool EffectActive => _shakeTime > 0f || _zooming;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        /// <summary>A short decaying positional shake (bumper punch).</summary>
        public void Shake(float amplitude, float duration)
        {
            _shakeAmplitude = amplitude;
            _shakeDuration = duration;
            _shakeTime = duration;
        }

        /// <summary>Zoom toward a point and back (hole capture celebration).</summary>
        public void CelebrateZoom(Vector2 target)
            => StartZoom(target, ZoomScale, ZoomInTime, ZoomHoldTime, ZoomOutTime, pull: 0.4f);

        /// <summary>
        /// A small, quick tightening — the near miss. It is deliberately a
        /// twelfth of the celebration's zoom and a third of its length: the
        /// ball is still rolling and the player is still watching it, so this
        /// has to register without moving the thing being watched.
        /// </summary>
        public void Tighten(Vector2 target)
            => StartZoom(target, 0.965f, 0.09f, 0.05f, 0.18f, pull: 0.12f);

        private void StartZoom(Vector2 target, float scale, float inTime, float hold, float outTime, float pull)
        {
            _zooming = true;
            _zoomTime = 0f;
            _zoomTarget = target;
            _zoomScale = scale;
            _zoomIn = inTime;
            _zoomHold = hold;
            _zoomOut = outTime;
            _zoomPull = pull;
        }

        /// <summary>Stops all effects and restores the framed view (run reset).</summary>
        public void CancelEffects()
        {
            if (EffectActive)
            {
                transform.position = _basePos;
                _cam.orthographicSize = _baseSize;
            }

            _shakeTime = 0f;
            _zooming = false;
        }

        private void LateUpdate()
        {
            if (!EffectActive)
            {
                // Idle: whatever CameraFramer set is the baseline to return to.
                _basePos = transform.position;
                _baseSize = _cam.orthographicSize;
                return;
            }

            var pos = _basePos;
            float size = _baseSize;

            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.deltaTime;
                float k = Mathf.Max(0f, _shakeTime / _shakeDuration);
                var jitter = Random.insideUnitCircle * (_shakeAmplitude * k);
                pos += new Vector3(jitter.x, jitter.y, 0f);
            }

            if (_zooming)
            {
                _zoomTime += Time.deltaTime;
                float total = _zoomIn + _zoomHold + _zoomOut;
                float w = _zoomTime < _zoomIn ? _zoomTime / _zoomIn
                    : _zoomTime < _zoomIn + _zoomHold ? 1f
                    : 1f - (_zoomTime - _zoomIn - _zoomHold) / _zoomOut;
                if (_zoomTime >= total)
                {
                    _zooming = false;
                    w = 0f;
                }

                w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(w));
                size = Mathf.Lerp(_baseSize, _baseSize * _zoomScale, w);
                var toward = new Vector3(_zoomTarget.x, _zoomTarget.y, _basePos.z);
                pos = Vector3.Lerp(pos, toward, _zoomPull * w);
            }

            transform.position = pos;
            _cam.orthographicSize = size;

            if (!EffectActive)
            {
                // Effects just ended this frame — land exactly on the baseline.
                transform.position = _basePos;
                _cam.orthographicSize = _baseSize;
            }
        }
    }
}
