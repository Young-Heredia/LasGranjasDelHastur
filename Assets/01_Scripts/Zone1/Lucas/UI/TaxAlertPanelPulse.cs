using UnityEngine;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone1.UI
{
    /// <summary>Pulso sutil en el borde del panel de impuesto mientras la alerta está abierta.</summary>
    [DisallowMultipleComponent]
    public sealed class TaxAlertPanelPulse : MonoBehaviour
    {
        [SerializeField] float pulseSpeed = 2.4f;
        Outline _outline;
        Color _outlineRest;

        void Awake()
        {
            _outline = GetComponent<Outline>();
            if (_outline != null)
                _outlineRest = _outline.effectColor;
        }

        void Update()
        {
            if (_outline == null || !isActiveAndEnabled)
                return;
            var w = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            _outline.effectColor = Color.Lerp(
                new Color(0.82f, 0.22f, 0.12f, 0.38f),
                new Color(1f, 0.62f, 0.18f, 0.92f),
                w);
        }

        void OnDisable()
        {
            if (_outline != null)
                _outline.effectColor = _outlineRest;
        }
    }
}
