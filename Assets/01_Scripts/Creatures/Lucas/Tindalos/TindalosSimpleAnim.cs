using UnityEngine;

namespace LasGranjasDelHastur.Creatures
{
    [DisallowMultipleComponent]
    public sealed class TindalosSimpleAnim : MonoBehaviour
    {
        TindalosHoundDef _def;
        Vector3 _baseLocal;
        bool _captured;
        float _t0;

        public void Setup(TindalosHoundDef def)
        {
            _def = def;
        }

        void OnEnable() => _t0 = Time.unscaledTime;

        void Capture()
        {
            if (_captured)
                return;
            _baseLocal = transform.localPosition;
            _captured = true;
        }

        void LateUpdate()
        {
            Capture();
            var t = (Time.unscaledTime - _t0) * (_def.ElderSlow ? 0.65f : 1f);
            var h = _def.BobHz;
            var a = _def.BobAmp;
            var y = Mathf.Sin(t * h * 6.2831f) * a;
            var x = Mathf.Sin(t * h * 3.1f) * a * 0.35f;
            transform.localPosition = _baseLocal + new Vector3(x, y, 0f);

            var s = 1f + 0.03f * Mathf.Sin(t * h * 4f);
            transform.localScale = new Vector3(s, s, 1f);

            if (!_def.ShadowPulse)
                return;
            var bodyT = transform.Find("Body");
            if (bodyT == null)
                return;
            var sr = bodyT.GetComponent<SpriteRenderer>();
            if (sr == null)
                return;
            var c = sr.color;
            c.a = 0.55f + 0.2f * Mathf.Sin(t * 2.1f);
            sr.color = c;
        }
    }
}
