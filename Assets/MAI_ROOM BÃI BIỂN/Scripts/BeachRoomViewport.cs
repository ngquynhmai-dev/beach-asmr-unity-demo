using UnityEngine;

namespace Mai.Beach
{
    /// <summary>Fits the authored room while a separate background covers the viewport.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BeachRoomViewport : MonoBehaviour
    {
        [SerializeField] private Camera _roomCamera;
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private Vector2 _contentSize = new Vector2(10f, 10f);
        [SerializeField] private Vector2 _contentCenter = Vector2.zero;
        [SerializeField, Min(1f)] private float _contentMargin = 1.02f;
        [Header("Optional viewport-edge foliage")]
        [SerializeField] private SpriteRenderer _leftPalm;
        [SerializeField] private SpriteRenderer _rightPalm;
        [Header("Authored palm poses, anchored to the viewport edges")]
        [SerializeField] private Vector2 _leftPalmEdgeOffset = new Vector2(-0.26f, 3.31f);
        [SerializeField] private Vector2 _rightPalmEdgeOffset = new Vector2(-0.74f, 2.42f);
        [SerializeField] private Vector3 _leftPalmScale = new Vector3(0.47088f, 0.47088f, 0.78787f);
        [SerializeField] private Vector3 _rightPalmScale = new Vector3(0.38107f, 0.38107f, 0.63761f);
        [SerializeField] private Vector3 _leftPalmRotation = Vector3.zero;
        [SerializeField] private Vector3 _rightPalmRotation = new Vector3(0f, -6.3461f, 0f);

        private float _lastAspect = -1f;
        private Sprite _lastBackgroundSprite;

        private void OnEnable()
        {
            Refresh();
        }

        private void OnValidate()
        {
            _contentSize.x = Mathf.Max(0.01f, _contentSize.x);
            _contentSize.y = Mathf.Max(0.01f, _contentSize.y);
            _contentMargin = Mathf.Max(1f, _contentMargin);
            Refresh();
        }

        private void LateUpdate()
        {
            UpdateForViewport();
        }

        private void OnPreCull()
        {
            UpdateForViewport();
        }

        private void UpdateForViewport()
        {
            if (_roomCamera == null || _background == null)
                return;

            float aspect = (float)_roomCamera.pixelWidth / Mathf.Max(1, _roomCamera.pixelHeight);
            if (Mathf.Abs(aspect - _lastAspect) > 0.0001f ||
                _background.sprite != _lastBackgroundSprite)
            {
                _roomCamera.ResetAspect();
                ApplyForAspect(aspect);
            }
        }

        public void Refresh()
        {
            _lastAspect = -1f;
            if (_roomCamera != null)
            {
                _roomCamera.ResetAspect();
                ApplyForAspect(_roomCamera.aspect);
            }
        }

        /// <summary>Also allows deterministic off-screen checks at a given aspect.</summary>
        public void ApplyForAspect(float aspect)
        {
            if (_roomCamera == null || aspect <= 0f ||
                float.IsNaN(aspect) || float.IsInfinity(aspect))
                return;

            _roomCamera.orthographic = true;
            _roomCamera.rect = new Rect(0f, 0f, 1f, 1f);
            _roomCamera.orthographicSize = Mathf.Max(
                _contentSize.y * 0.5f,
                _contentSize.x * 0.5f / aspect) * _contentMargin;

            Vector3 position = _roomCamera.transform.position;
            position.x = _contentCenter.x;
            position.y = _contentCenter.y;
            _roomCamera.transform.position = position;

            if (_background != null && _background.sprite != null)
            {
                Vector2 nativeSize = _background.sprite.bounds.size;
                float viewHeight = _roomCamera.orthographicSize * 2f;
                float viewWidth = viewHeight * aspect;
                float cover = Mathf.Max(1f, Mathf.Max(
                    viewWidth / Mathf.Max(0.01f, nativeSize.x),
                    viewHeight / Mathf.Max(0.01f, nativeSize.y)) * 1.005f);
                _background.transform.localScale = new Vector3(cover, cover, 1f);
                _background.transform.position = new Vector3(
                    _contentCenter.x, _contentCenter.y, _background.transform.position.z);
                _lastBackgroundSprite = _background.sprite;
            }

            FitPalm(_leftPalm, false, aspect);
            FitPalm(_rightPalm, true, aspect);
            _lastAspect = aspect;
        }

        private void FitPalm(SpriteRenderer palm, bool right, float aspect)
        {
            if (palm == null || palm.sprite == null)
                return;

            float halfWidth = _roomCamera.orthographicSize * aspect;
            Vector2 offset = right ? _rightPalmEdgeOffset : _leftPalmEdgeOffset;
            palm.transform.localScale = right ? _rightPalmScale : _leftPalmScale;
            palm.transform.localEulerAngles = right ? _rightPalmRotation : _leftPalmRotation;
            palm.flipX = right;
            palm.transform.position = new Vector3(
                _contentCenter.x + (right ? halfWidth : -halfWidth) + offset.x,
                _contentCenter.y + offset.y,
                -0.002f);
        }
    }
}

