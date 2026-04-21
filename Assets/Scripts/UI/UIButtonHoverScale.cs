using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Escala el botón al pasar el cursor (feedback de selección sin animación continua).
/// </summary>
[DisallowMultipleComponent]
public class UIButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] float hoverScale = 1.08f;
    [SerializeField] float transitionSpeed = 12f;

    RectTransform _rt;
    Vector3 _baseScale;
    float _targetFactor = 1f;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (_rt != null)
            _baseScale = _rt.localScale;
    }

    void Update()
    {
        if (_rt == null)
            return;
        float t = Mathf.Clamp01(Time.unscaledDeltaTime * transitionSpeed);
        var target = _baseScale * _targetFactor;
        _rt.localScale = Vector3.Lerp(_rt.localScale, target, t);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _targetFactor = hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetFactor = 1f;
    }

    void OnDisable()
    {
        if (_rt != null)
            _rt.localScale = _baseScale;
        _targetFactor = 1f;
    }
}
