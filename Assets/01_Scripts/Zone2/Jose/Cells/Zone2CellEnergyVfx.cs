using LasGranjasDelHastur;
using UnityEngine;

namespace LasGranjasDelHastur.Zone2.Jose
{
    /// <summary>Efecto visual de energía: anillos y núcleo pulsante según estado y arquetipo.</summary>
    [DisallowMultipleComponent]
    public sealed class Zone2CellEnergyVfx : MonoBehaviour
    {
        Transform _vfxRoot;
        SpriteRenderer _ring;
        SpriteRenderer _core;
        int _baseMainOrder = 40;
        bool _built;
        Zone2CellVisualState _state;
        Color _tint;
        float _levelBoost = 1f;

        public void SetVisual(Zone2DistrictType district, Zone2CellVisualState state, int cellLevel = 1, bool unlocked = true)
        {
            EnsureBuilt();
            _state = state;
            _tint = HueForDistrict(district);
            var l = Zone2CellLevelRules.ClampLevel(cellLevel);
            _levelBoost = unlocked ? 0.75f + 0.1f * (l - 1) : 0.35f;
            if (state == Zone2CellVisualState.Corrupted)
                _tint = Color.Lerp(_tint, new Color(0.78f, 0.2f, 0.45f, 1f), 0.55f);

            var d = GetComponent<Zone2CellVisualDriver>();
            if (d != null && d.MainRenderer != null)
                _baseMainOrder = d.MainRenderer.sortingOrder;
            else
            {
                var m = GetComponent<SpriteRenderer>();
                if (m != null)
                    _baseMainOrder = m.sortingOrder;
            }
        }

        void EnsureBuilt()
        {
            if (_built)
                return;
            var existing = transform.Find("Z2_EnergyVfx");
            if (existing != null)
            {
                _vfxRoot = existing;
                _ring = existing.Find("Ring")?.GetComponent<SpriteRenderer>();
                _core = existing.Find("Core")?.GetComponent<SpriteRenderer>();
                if (_ring != null && _core != null)
                {
                    _built = true;
                    return;
                }
            }

            _vfxRoot = new GameObject("Z2_EnergyVfx").transform;
            _vfxRoot.SetParent(transform, false);
            _vfxRoot.localPosition = new Vector3(0f, 0.1f, 0f);
            _vfxRoot.localScale = Vector3.one;

            _ring = AddQuad("Ring", 1.2f, 1.2f);
            _core = AddQuad("Core", 0.48f, 0.48f);
            _built = true;
        }

        SpriteRenderer AddQuad(string name, float sx, float sy)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_vfxRoot, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = new Vector3(sx, sy, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = new Color(0.2f, 0.9f, 0.7f, 0.15f);
            sr.sortingOrder = 10;
            return sr;
        }

        static Color HueForDistrict(Zone2DistrictType d) =>
            d switch
            {
                Zone2DistrictType.LunarGarden => new Color(0.45f, 0.95f, 0.6f, 1f),
                Zone2DistrictType.CometMill => new Color(0.35f, 0.82f, 1f, 1f),
                Zone2DistrictType.PlanetaryCore => new Color(1f, 0.55f, 0.2f, 1f),
                _ => new Color(0.92f, 0.48f, 1f, 1f),
            };

        void LateUpdate()
        {
            if (!_built || _ring == null || _core == null)
                return;

            if (_state == Zone2CellVisualState.Locked)
            {
                _ring.color = new Color(0, 0, 0, 0);
                _core.color = new Color(0, 0, 0, 0);
                return;
            }

            if (_state == Zone2CellVisualState.Idle)
            {
                _vfxRoot.localScale = Vector3.one;
                _ring.color = new Color(_tint.r, _tint.g, _tint.b, 0.07f * _levelBoost);
                _core.color = new Color(_tint.r, _tint.g, _tint.b, 0.05f * _levelBoost);
                _ring.sortingOrder = _baseMainOrder - 1;
                _core.sortingOrder = _baseMainOrder - 2;
                return;
            }

            var t = Time.unscaledTime;
            var freq = _state == Zone2CellVisualState.Ready
                ? 4.5f
                : _state == Zone2CellVisualState.Corrupted
                    ? 1.2f
                    : 2.3f;
            if (_state == Zone2CellVisualState.Corrupted)
            {
                var flick = 0.5f + 0.5f * Mathf.PerlinNoise(t * 2.1f, 0.7f);
                var pr = 0.15f + 0.2f * flick;
                _vfxRoot.localScale = new Vector3(0.9f + 0.15f * flick, 0.9f + 0.15f * flick, 1f);
                _ring.color = new Color(_tint.r, _tint.g, _tint.b, pr * _levelBoost);
                _core.color = new Color(1f, 0.4f, 0.5f, pr * 1.1f * _levelBoost);
            }
            else
            {
                var s = 0.5f + 0.5f * Mathf.Sin(t * freq);
                var scale = 0.9f + 0.12f * s;
                if (_state == Zone2CellVisualState.Ready)
                    scale = 0.88f + 0.2f * (0.5f + 0.5f * Mathf.Sin(t * 6.2f));
                scale *= 0.85f + 0.04f * (_levelBoost * 1.2f);
                _vfxRoot.localScale = new Vector3(scale, scale, 1f);
                var aRing = (_state == Zone2CellVisualState.Ready
                    ? 0.2f + 0.3f * s
                    : 0.12f + 0.2f * s) * _levelBoost;
                _ring.color = new Color(_tint.r, _tint.g, _tint.b, aRing);
                var aCore = (0.15f + 0.3f * s) * _levelBoost;
                _core.color = new Color(_tint.r, _tint.g, _tint.b, aCore);
            }

            _ring.sortingOrder = _baseMainOrder - 1;
            _core.sortingOrder = _baseMainOrder - 2;
        }
    }
}
