using UnityEngine;
using LasGranjasDelHastur;

namespace LasGranjasDelHastur.Camera
{
    [DisallowMultipleComponent]
    public class CameraController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnityEngine.Camera targetCamera;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 1f;
        [SerializeField] private float panSmoothing = 14f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 6f;
        [SerializeField] private float zoomSmoothing = 16f;
        [SerializeField] private float minOrthoSize = 3.5f;
        [SerializeField] private float maxOrthoSize = 10f;

        [Header("Bounds (world)")]
        [SerializeField] private Vector2 minBounds = new(-18f, -10f);
        [SerializeField] private Vector2 maxBounds = new(18f, 10f);

        Vector3 _targetPos;
        float _targetZoom;
        Vector3 _dragOriginWorld;
        bool _dragging;

        void Awake()
        {
            if (targetCamera == null)
                targetCamera = GetComponent<UnityEngine.Camera>();
            if (targetCamera == null)
                targetCamera = UnityEngine.Camera.main;

            _targetPos = transform.position;
            _targetZoom = targetCamera != null ? targetCamera.orthographicSize : 5f;
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
            _targetPos = ClampToBounds(_targetPos);
            transform.position = _targetPos;
        }

        void Update()
        {
            if (targetCamera == null)
                return;

            HandleZoom();
            HandlePan();

            _targetPos = ClampToBounds(_targetPos);
            transform.position = Vector3.Lerp(transform.position, _targetPos, 1f - Mathf.Exp(-panSmoothing * Time.deltaTime));
            targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, _targetZoom, 1f - Mathf.Exp(-zoomSmoothing * Time.deltaTime));
        }

        void HandleZoom()
        {
            var scroll = InputAdapter.MouseScrollY();
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetZoom -= scroll * zoomSpeed * 0.1f;
                _targetZoom = Mathf.Clamp(_targetZoom, minOrthoSize, maxOrthoSize);
            }
        }

        void HandlePan()
        {
            if (InputAdapter.MiddleMouseDownThisFrame() || (InputAdapter.LeftMouseDownThisFrame() && InputAdapter.IsSpacePressed()))
            {
                _dragging = true;
                _dragOriginWorld = ScreenToWorld(InputAdapter.MousePosition());
            }

            if (InputAdapter.MiddleMouseUpThisFrame() || (InputAdapter.LeftMouseUpThisFrame() && !InputAdapter.IsSpacePressed()))
                _dragging = false;

            if (!_dragging)
                return;

            var currentWorld = ScreenToWorld(InputAdapter.MousePosition());
            var delta = _dragOriginWorld - currentWorld;
            _targetPos += delta * panSpeed;
        }

        Vector3 ScreenToWorld(Vector3 screen)
        {
            var z = -targetCamera.transform.position.z;
            screen.z = z;
            return targetCamera.ScreenToWorldPoint(screen);
        }

        Vector3 ClampToBounds(Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
            return pos;
        }
    }
}

