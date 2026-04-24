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
        [SerializeField] private float dynamicPanSmoothingBoost = 0.7f;
        [SerializeField] private float maxDragStepWorld = 2.2f;
        [SerializeField] private float positionSnapEpsilon = 0.0005f;

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
        Vector3 _lastDragWorld;
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
            var distance = Vector3.Distance(transform.position, _targetPos);
            var dynamicSmoothing = panSmoothing * (1f + Mathf.Clamp01(distance * 0.35f) * dynamicPanSmoothingBoost);
            var panLerp = 1f - Mathf.Exp(-dynamicSmoothing * Time.deltaTime);
            var nextPos = Vector3.Lerp(transform.position, _targetPos, panLerp);
            transform.position = distance <= positionSnapEpsilon ? _targetPos : nextPos;
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
                _lastDragWorld = ScreenToWorld(InputAdapter.MousePosition());
            }

            if (InputAdapter.MiddleMouseUpThisFrame() || InputAdapter.LeftMouseUpThisFrame())
                _dragging = false;

            if (!_dragging)
                return;

            var currentWorld = ScreenToWorld(InputAdapter.MousePosition());
            var delta = _lastDragWorld - currentWorld;
            if (delta.sqrMagnitude > maxDragStepWorld * maxDragStepWorld)
                delta = delta.normalized * maxDragStepWorld;

            _targetPos += delta * panSpeed;
            _lastDragWorld = currentWorld;
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

