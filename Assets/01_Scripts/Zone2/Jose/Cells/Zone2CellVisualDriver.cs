using LasGranjasDelHastur;
using LasGranjasDelHastur.Zone1;
using UnityEngine;

namespace LasGranjasDelHastur.Zone2.Jose
{
    [DisallowMultipleComponent]
    public sealed class Zone2CellVisualDriver : MonoBehaviour
    {
        [SerializeField] SpriteRenderer mainRenderer;
        [SerializeField] string emptySpritePath = "Assets/02_Sprites/Lucas/Zone2/Cells/Buildings/z2_building_souls_pit_idle.png";

        Vector3 _baseScale;
        bool _baseCaptured;
        bool _animProducing;
        bool _animReady;
        float _levelScale = 1f;
        int _visLevel = 1;
        Zone2CellEnergyVfx _energyVfx;

        public SpriteRenderer MainRenderer => mainRenderer;

        void Awake()
        {
            if (mainRenderer == null)
                mainRenderer = GetComponent<SpriteRenderer>();
            if (string.IsNullOrWhiteSpace(emptySpritePath) ||
                emptySpritePath.IndexOf("hastur_pixel_art_pack/Cells/Base/Cell_Empty", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                emptySpritePath = "Assets/02_Sprites/Lucas/Zone2/Cells/Buildings/z2_building_souls_pit_idle.png";
            }
            _energyVfx = GetComponent<Zone2CellEnergyVfx>();
            if (_energyVfx == null)
                _energyVfx = gameObject.AddComponent<Zone2CellEnergyVfx>();
            CaptureBaseIfNeeded();
        }

        public void Initialize(SpriteRenderer sr)
        {
            mainRenderer = sr;
            CaptureBaseIfNeeded();
        }

        void CaptureBaseIfNeeded()
        {
            if (_baseCaptured)
                return;
            _baseScale = transform.localScale;
            _baseCaptured = true;
        }

        public void Apply(Zone2DistrictType district, Zone2CellVisualState state, int cellLevel = 1, bool cellUnlocked = true)
        {
            CaptureBaseIfNeeded();
            if (mainRenderer == null)
                return;

            _visLevel = Zone2CellLevelRules.ClampLevel(cellLevel);
            _levelScale = Zone2CellLevelRules.VisualScaleMultiplier(_visLevel);
            if (!cellUnlocked)
                _levelScale = 1f;

            var path = Zone2CellSpritePathResolver.ResolveDistrict(district);
            var sp = Zone1ArtProvider.LoadSprite(path);
            if (sp == null)
                sp = Zone1ArtProvider.LoadSprite(emptySpritePath) ?? RuntimeSpriteFactory.OpaqueWhiteSprite;

            mainRenderer.sprite = sp;
            var c = ResolveTint(state);
            if (cellUnlocked)
                c *= Zone2CellLevelRules.LevelTintForMainSprite(_visLevel, true);
            mainRenderer.color = c;
            _animProducing = state == Zone2CellVisualState.Producing;
            _animReady = state == Zone2CellVisualState.Ready;
            if (_energyVfx == null)
                _energyVfx = GetComponent<Zone2CellEnergyVfx>() ?? gameObject.AddComponent<Zone2CellEnergyVfx>();
            _energyVfx.SetVisual(district, state, _visLevel, cellUnlocked);
        }

        void LateUpdate()
        {
            if (!_baseCaptured)
                return;
            if (_animProducing)
            {
                var t = 1f + 0.045f * Mathf.Sin(Time.time * 2.4f);
                transform.localScale = _baseScale * _levelScale * t;
            }
            else if (_animReady)
            {
                var t = 1f + 0.085f * Mathf.Sin(Time.time * 4.2f);
                transform.localScale = _baseScale * _levelScale * t;
            }
            else
            {
                transform.localScale = _baseScale * _levelScale;
            }
        }

        static Color ResolveTint(Zone2CellVisualState s)
        {
            return s switch
            {
                Zone2CellVisualState.Locked => new Color(0.55f, 0.55f, 0.60f, 1f),
                Zone2CellVisualState.Idle => Color.white,
                Zone2CellVisualState.Producing => new Color(0.75f, 0.90f, 1f, 1f),
                Zone2CellVisualState.Ready => new Color(1f, 0.96f, 0.80f, 1f),
                Zone2CellVisualState.Corrupted => new Color(0.75f, 0.55f, 0.90f, 1f),
                _ => Color.white,
            };
        }
    }
}
