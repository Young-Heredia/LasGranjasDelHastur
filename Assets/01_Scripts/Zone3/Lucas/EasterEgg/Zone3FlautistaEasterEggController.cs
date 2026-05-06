using System.Collections.Generic;
using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1;
using TMPro;
using UnityEngine;

namespace LasGranjasDelHastur.Zone3.Lucas
{
    [DisallowMultipleComponent]
    public sealed class Zone3FlautistaEasterEggController : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] string idleSpritePath = "Assets/02_Sprites/Lucas/Zone3/EasterEgg/flautista_amorfo_pixel_art_64x64.png";

        [Header("Dance sheet (4x3)")]
        [SerializeField] string danceSheetPath = "Assets/02_Sprites/Lucas/Zone3/EasterEgg/SpriteFlautista.png";
        [SerializeField] int danceColumns = 4;
        [SerializeField] int danceRows = 3;
        [SerializeField, Min(32f)] float dancePixelsPerUnit = 128f;
        [SerializeField, Min(1f)] float danceFps = 7f;
        [Tooltip("Índices válidos del sheet para evitar frames fantasma.")]
        [SerializeField] int[] danceFrameSequence = { 1, 8, 11, 8 };

        [Header("Trigger")]
        [SerializeField] int clicksToTrigger = 5;

        [Header("First-time bonus")]
        [SerializeField, Min(0)] int firstTimeBonusDarkCoins = 12000;

        [Header("Click UI")]
        [SerializeField] float clickPopupSeconds = 1.0f;
        [SerializeField] Vector3 clickPopupOffset = new(0f, 0.85f, 0f);

        readonly List<FlautistaVisual> _flautistas = new();
        int _clickCount;
        bool _lastActive;
        ResourceManager _resources;
        TextMeshPro _popup;
        float _popupRemaining;

        struct FlautistaVisual
        {
            public SpriteRenderer sr;
            public SpriteSheetAnimator anim;
        }

        void Update()
        {
            var am = AudioManager.Instance;
            var active = am != null && am.IsZone3EasterEggActive;
            if (active != _lastActive)
            {
                _lastActive = active;
                ApplyState(active);
            }

            if (_popup != null && _popupRemaining > 0f)
            {
                _popupRemaining -= Time.deltaTime;
                if (_popupRemaining <= 0f)
                    _popup.gameObject.SetActive(false);
            }
        }

        public void PruneDestroyedFlautistas()
        {
            for (var i = _flautistas.Count - 1; i >= 0; i--)
            {
                var v = _flautistas[i];
                if (v.sr == null || v.anim == null)
                    _flautistas.RemoveAt(i);
            }
        }

        public void RegisterFlautista(Transform root)
        {
            if (root == null)
                return;

            var sr = root.GetComponent<SpriteRenderer>() ?? root.gameObject.AddComponent<SpriteRenderer>();
            var anim = root.GetComponent<SpriteSheetAnimator>() ?? root.gameObject.AddComponent<SpriteSheetAnimator>();

            _flautistas.Add(new FlautistaVisual { sr = sr, anim = anim });

            var active = AudioManager.Instance != null && AudioManager.Instance.IsZone3EasterEggActive;
            _lastActive = active;
            ApplyOne(active, sr, anim);
        }

        public void RegisterFlautistaClick(Vector3 worldPos)
        {
            _clickCount += 1;
            ShowClickPopup(worldPos);
            if (_clickCount < clicksToTrigger)
                return;

            _clickCount = 0;
            AudioManager.Instance?.TriggerZone3EasterEgg();
            var bonusResult = TryGrantFirstTimeBonus();
            if (bonusResult != BonusResult.None)
                ShowBonusPopup(worldPos, bonusResult);
        }

        enum BonusResult
        {
            None = 0,
            Granted,
            AlreadyClaimed,
            FailedNoSave,
            FailedNoResources,
        }

        BonusResult TryGrantFirstTimeBonus()
        {
            if (firstTimeBonusDarkCoins <= 0)
                return BonusResult.None;

            var sm = SaveManager.Instance;
            if (sm == null || sm.CachedData == null || sm.CachedData.zone3 == null)
                return BonusResult.FailedNoSave;

            if (sm.CachedData.zone3.zone3FlautistaEasterBonusClaimed)
                return BonusResult.AlreadyClaimed;

            if (_resources == null)
                _resources = FindFirstObjectByType<ResourceManager>();
            if (_resources == null)
                return BonusResult.FailedNoResources;

            _resources.Add(ResourceType.DarkCoins, firstTimeBonusDarkCoins);
            sm.CachedData.zone3 ??= new Zone3SaveData();
            sm.CachedData.zone3.zone3FlautistaEasterBonusClaimed = true;
            sm.CachedData.zone3Available = true;
            sm.CachedData.zone3.valid = true;
            sm.SaveNow();
            return BonusResult.Granted;
        }

        void ShowBonusPopup(Vector3 worldPos, BonusResult result)
        {
            if (_popup == null)
                ShowClickPopup(worldPos);

            if (_popup == null)
                return;

            _popup.text = result switch
            {
                BonusResult.Granted => $"+{firstTimeBonusDarkCoins} monedas (bono 1ª vez)",
                BonusResult.AlreadyClaimed => "Bono ya cobrado (hasta Game Over)",
                BonusResult.FailedNoSave => "Bono falló: SaveManager no listo",
                BonusResult.FailedNoResources => "Bono falló: ResourceManager no listo",
                _ => ""
            };
            _popup.transform.position = worldPos + clickPopupOffset;
            _popup.sortingOrder = 200;
            _popup.gameObject.SetActive(true);
            _popupRemaining = Mathf.Max(_popupRemaining, clickPopupSeconds);
        }

        void ShowClickPopup(Vector3 worldPos)
        {
            if (_popup == null)
            {
                var go = new GameObject("FlautistaClickPopup");
                go.transform.SetParent(transform, false);
                _popup = go.AddComponent<TextMeshPro>();
                _popup.fontSize = 4;
                _popup.alignment = TextAlignmentOptions.Center;
                _popup.color = new Color(0.75f, 0.95f, 1f, 1f);
                _popup.gameObject.SetActive(false);
            }

            _popup.text = $"Flautista {_clickCount}/{clicksToTrigger}";
            _popup.transform.position = worldPos + clickPopupOffset;
            _popup.sortingOrder = 200;
            _popup.gameObject.SetActive(true);
            _popupRemaining = clickPopupSeconds;
        }

        void ApplyState(bool active)
        {
            foreach (var p in _flautistas)
                ApplyOne(active, p.sr, p.anim);
        }

        void ApplyOne(bool active, SpriteRenderer sr, SpriteSheetAnimator anim)
        {
            if (sr == null)
                return;

            if (active)
            {
                if (anim != null)
                {
                    anim.enabled = true;
                    anim.ConfigureGridSequence(danceSheetPath, danceColumns, danceRows, danceFrameSequence, danceFps, shouldLoop: true, pixelsPerUnit: dancePixelsPerUnit);
                }
                sr.color = Color.white;
                return;
            }

            if (anim != null)
                anim.enabled = false;
            sr.sprite = Zone1ArtProvider.LoadSprite(idleSpritePath) ?? sr.sprite;
            sr.color = Color.white;
        }
    }
}
