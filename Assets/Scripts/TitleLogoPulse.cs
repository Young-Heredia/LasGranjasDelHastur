using UnityEngine;

/// <summary>
/// Escala el logo del menú con un ciclo suave (efecto "respiración").
/// </summary>
[DisallowMultipleComponent]
public class TitleLogoPulse : MonoBehaviour
{
    [SerializeField] float minScale = 0.96f;
    [SerializeField] float maxScale = 1.04f;
    [SerializeField] float speed = 1.35f;
    [SerializeField] bool useUnscaledTime = true;

    Vector3 _baseScale;

    void Awake()
    {
        _baseScale = transform.localScale;
    }

    /// <summary>Otros sistemas (p. ej. ZoneSelection) pueden ajustar el pulso tras AddComponent.</summary>
    public void SetPulse(float min, float max, float pulseSpeed)
    {
        minScale = min;
        maxScale = max;
        speed = pulseSpeed;
    }

    void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float pulse = (Mathf.Sin(t * speed) + 1f) * 0.5f;
        float s = Mathf.Lerp(minScale, maxScale, pulse);
        transform.localScale = _baseScale * s;
    }
}
