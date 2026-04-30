using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    public class Zone1CultistEasterEggController : MonoBehaviour
    {
        [Header("Sprites (static)")]
        [SerializeField] string staffSpritePath = "Assets/02_Sprites/Lucas/Zone1/Props/zone1_cultist_yellow_staff_64.png";
        [SerializeField] string bookSpritePath = "Assets/02_Sprites/Lucas/Zone1/Props/zone1_cultist_yellow_book_64.png";

        [Header("Dance sheet (12x1, 64px frames)")]
        [SerializeField] string danceSheetPath = "Assets/02_Sprites/Lucas/Zone1/Spritesheets/zone1_cultist_yellow_dance_nobook_transparent_spritesheet_12x1_64.png";
        [SerializeField, Min(1f)] float danceFps = 10f;

        [Header("Trigger")]
        [SerializeField] int clicksToTrigger = 5;

        readonly List<Zone1CultistVisual> _cultists = new();
        int _clickCount;
        bool _lastActive;

        [Header("Click UI")]
        [SerializeField] float clickPopupSeconds = 1.0f;
        [SerializeField] Vector3 clickPopupOffset = new(0f, 0.9f, 0f);
        TextMeshPro _popup;
        float _popupRemaining;

        struct Zone1CultistVisual
        {
            public bool isBook;
            public SpriteRenderer sr;
            public SpriteSheetAnimator anim;
        }

        void Update()
        {
            var am = AudioManager.Instance;
            var active = am != null && am.IsZone1EasterEggActive;
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

        public void PruneDestroyedCultists()
        {
            for (var i = _cultists.Count - 1; i >= 0; i--)
            {
                var v = _cultists[i];
                if (v.sr == null || v.anim == null)
                    _cultists.RemoveAt(i);
            }
        }

        public void RegisterCultist(Transform cultist, bool isBook)
        {
            if (cultist == null)
                return;
            var sr = cultist.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = cultist.gameObject.AddComponent<SpriteRenderer>();

            var anim = cultist.GetComponent<SpriteSheetAnimator>();
            if (anim == null)
                anim = cultist.gameObject.AddComponent<SpriteSheetAnimator>();

            _cultists.Add(new Zone1CultistVisual
            {
                isBook = isBook,
                sr = sr,
                anim = anim,
            });

            // Apply current state immediately for late-registered cultists.
            var active = AudioManager.Instance != null && AudioManager.Instance.IsZone1EasterEggActive;
            _lastActive = active;
            ApplyOne(active, sr, anim, isBook);
        }

        public void RegisterCultistClick(Vector3 worldPos)
        {
            _clickCount += 1;
            ShowClickPopup(worldPos);
            if (_clickCount < clicksToTrigger)
                return;

            _clickCount = 0;
            AudioManager.Instance?.TriggerZone1EasterEgg();
        }

        void ShowClickPopup(Vector3 worldPos)
        {
            if (_popup == null)
            {
                var go = new GameObject("CultistClickPopup");
                go.transform.SetParent(transform, false);
                _popup = go.AddComponent<TextMeshPro>();
                _popup.fontSize = 4;
                _popup.alignment = TextAlignmentOptions.Center;
                _popup.color = new Color(1f, 0.92f, 0.55f, 1f);
                _popup.gameObject.SetActive(false);
            }

            _popup.text = $"Easter {_clickCount}/{clicksToTrigger}";
            _popup.transform.position = worldPos + clickPopupOffset;
            _popup.sortingOrder = 200;
            _popup.gameObject.SetActive(true);
            _popupRemaining = clickPopupSeconds;
        }

        void ApplyState(bool active)
        {
            foreach (var c in _cultists)
                ApplyOne(active, c.sr, c.anim, c.isBook);
        }

        void ApplyOne(bool active, SpriteRenderer sr, SpriteSheetAnimator anim, bool isBook)
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

            // Static.
            if (anim != null)
                anim.enabled = false;
            var path = isBook ? bookSpritePath : staffSpritePath;
            sr.sprite = Zone1ArtProvider.LoadSprite(path) ?? sr.sprite;
            sr.color = Color.white;
        }
    }
}

