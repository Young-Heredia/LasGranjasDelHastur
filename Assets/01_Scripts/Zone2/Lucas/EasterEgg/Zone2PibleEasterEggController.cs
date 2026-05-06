using System;
using System.Collections.Generic;
using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1;
using TMPro;
using UnityEngine;

namespace LasGranjasDelHastur.Zone2.Lucas
{
    /// <summary>
    /// Easter egg Ciudades: 9 Tindalos Pible; misma lógica de clics que cultistas Z1 (varios clics hasta desbloquear).
    /// Música: <see cref="AudioManager.TriggerZone2EasterEgg"/>; bono 4000 monedas la primera vez en la run.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Zone2PibleEasterEggController : MonoBehaviour
    {
        public static event Action EasterEggActivated;

        [Header("Sprites")]
        [SerializeField] string idleSpritePath =
            "Assets/02_Sprites/Lucas/Zone2/EasterEgg/zone1_easteregg_tindalos_pible_idle_v3.png";

        [Header("Dance sheet (24x1, 64px)")]
        [SerializeField] string danceSheetPath =
            "Assets/02_Sprites/Lucas/Zone2/EasterEgg/zone1_easteregg_tindalos_pible_dance_spritesheet_24x1_64.png";
        [SerializeField, Min(1f)] float danceFps = 10f;

        [Header("Trigger (igual que Zona 1 cultistas)")]
        [SerializeField] int clicksToTrigger = 5;

        [Header("First-time bonus")]
        [SerializeField, Min(0)] int firstTimeBonusDarkCoins = 4000;

        [Header("Click UI")]
        [SerializeField] float clickPopupSeconds = 1.0f;
        [SerializeField] Vector3 clickPopupOffset = new(0f, 0.85f, 0f);

        readonly List<PibleVisual> _pibles = new();
        int _clickCount;
        bool _lastActive;
        ResourceManager _resources;
        TextMeshPro _popup;
        float _popupRemaining;

        struct PibleVisual
        {
            public SpriteRenderer sr;
            public SpriteSheetAnimator anim;
        }

        void Update()
        {
            var am = AudioManager.Instance;
            var active = am != null && am.IsZone2EasterEggActive;
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

        public void PruneDestroyedPibles()
        {
            for (var i = _pibles.Count - 1; i >= 0; i--)
            {
                var v = _pibles[i];
                if (v.sr == null || v.anim == null)
                    _pibles.RemoveAt(i);
            }
        }

        public void RegisterPible(Transform pibleRoot)
        {
            if (pibleRoot == null)
                return;

            var sr = pibleRoot.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = pibleRoot.gameObject.AddComponent<SpriteRenderer>();

            var anim = pibleRoot.GetComponent<SpriteSheetAnimator>();
            if (anim == null)
                anim = pibleRoot.gameObject.AddComponent<SpriteSheetAnimator>();

            _pibles.Add(new PibleVisual { sr = sr, anim = anim });

            var active = AudioManager.Instance != null && AudioManager.Instance.IsZone2EasterEggActive;
            _lastActive = active;
            ApplyOne(active, sr, anim);
        }

        public void RegisterPibleClick(Vector3 worldPos)
        {
            _clickCount += 1;
            ShowClickPopup(worldPos);
            if (_clickCount < clicksToTrigger)
                return;

            _clickCount = 0;
            AudioManager.Instance?.TriggerZone2EasterEgg();
            EasterEggActivated?.Invoke();
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
            if (sm == null || sm.CachedData == null || sm.CachedData.zone2 == null)
                return BonusResult.FailedNoSave;

            if (sm.CachedData.zone2.zone2PibleEasterBonusClaimed)
                return BonusResult.AlreadyClaimed;

            if (_resources == null)
                _resources = FindFirstObjectByType<ResourceManager>();
            if (_resources == null)
                return BonusResult.FailedNoResources;

            _resources.Add(ResourceType.DarkCoins, firstTimeBonusDarkCoins);
            sm.CachedData.zone2 ??= new Zone2SaveData();
            sm.CachedData.zone2.zone2PibleEasterBonusClaimed = true;
            sm.CachedData.zone2Available = true;
            sm.CachedData.zone2.valid = true;

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
                var go = new GameObject("PibleClickPopup");
                go.transform.SetParent(transform, false);
                _popup = go.AddComponent<TextMeshPro>();
                _popup.fontSize = 4;
                _popup.alignment = TextAlignmentOptions.Center;
                _popup.color = new Color(0.75f, 0.95f, 1f, 1f);
                _popup.gameObject.SetActive(false);
            }

            _popup.text = $"Pible {_clickCount}/{clicksToTrigger}";
            _popup.transform.position = worldPos + clickPopupOffset;
            _popup.sortingOrder = 200;
            _popup.gameObject.SetActive(true);
            _popupRemaining = clickPopupSeconds;
        }

        void ApplyState(bool active)
        {
            foreach (var p in _pibles)
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
                    anim.Configure(danceSheetPath, 64, 64, danceFps, shouldLoop: true);
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
