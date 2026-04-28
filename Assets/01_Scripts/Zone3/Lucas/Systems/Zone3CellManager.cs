using System;
using System.Collections.Generic;
using LasGranjasDelHastur.Zone1;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LasGranjasDelHastur.Zone3.Systems
{
    [DisallowMultipleComponent]
    public sealed class Zone3CellManager : MonoBehaviour
    {
        public event Action<int> SelectedSlotChanged;
        public event Action CellsChanged;

        [Header("Grid Layout (match Zone1 feel)")]
        [SerializeField] int columns = 4;
        [SerializeField] int rows = 3;
        [SerializeField] Vector2 spacing = new(2.2f, 2.2f);
        [SerializeField] Vector2 origin = new(-3.3f, 1.8f);

        [Header("Sprites (pack defaults)")]
        [SerializeField] string lockedSpritePath = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/Cells/Base/Cell_Locked.png";
        [SerializeField] string emptySpritePath = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/Cells/Base/Cell_Empty.png";

        readonly List<Slot> _slots = new();

        public int SelectedSlotIndex { get; private set; } = -1;
        public int SlotCount => _slots.Count;

        sealed class Slot
        {
            public int index;
            public GameObject go;
            public SpriteRenderer sr;
            public BoxCollider2D col;
        }

        void Awake()
        {
            BuildGridIfEmpty();
        }

        void Update()
        {
            if (!InputAdapter.LeftMouseDownThisFrame())
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var cam = UnityEngine.Camera.main;
            if (cam == null)
                return;

            var world = cam.ScreenToWorldPoint(InputAdapter.MousePosition());
            var hit = Physics2D.OverlapPoint(new Vector2(world.x, world.y));
            if (hit == null)
                return;

            var slot = hit.GetComponent<Zone3CellSlot>() ?? hit.GetComponentInParent<Zone3CellSlot>();
            if (slot == null)
                return;

            SelectSlot(slot.SlotIndex);
        }

        public void SelectSlot(int slotIndex)
        {
            if (SelectedSlotIndex == slotIndex)
            {
                SelectedSlotChanged?.Invoke(slotIndex);
                return;
            }
            SelectedSlotIndex = slotIndex;
            SelectedSlotChanged?.Invoke(slotIndex);
        }

        public void ClearSelection()
        {
            SelectedSlotIndex = -1;
            SelectedSlotChanged?.Invoke(-1);
        }

        public void BuildGridIfEmpty()
        {
            if (_slots.Count > 0)
                return;

            columns = Mathf.Max(1, columns);
            rows = Mathf.Max(1, rows);

            var locked = Zone1ArtProvider.LoadSprite(lockedSpritePath) ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                {
                    var idx = r * columns + c;
                    var go = new GameObject($"CellSlot_{idx:00}");
                    go.transform.SetParent(transform, false);
                    go.transform.position = new Vector3(origin.x + c * spacing.x, origin.y - r * spacing.y, 0f);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = locked;
                    sr.color = new Color(0.12f, 0.12f, 0.14f, 1f);
                    sr.sortingOrder = 40 + (rows - r) * 4;
                    go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

                    var col = go.AddComponent<BoxCollider2D>();
                    col.size = Vector2.one;

                    var slot = go.AddComponent<Zone3CellSlot>();
                    slot.Configure(idx);

                    _slots.Add(new Slot { index = idx, go = go, sr = sr, col = col });
                }
            }

            CellsChanged?.Invoke();
        }

        public void ApplyVisual(int slotIndex, string spritePath, Color tint)
        {
            var s = GetSlot(slotIndex);
            if (s == null || s.sr == null)
                return;

            var sprite = !string.IsNullOrEmpty(spritePath)
                ? Zone1ArtProvider.LoadSprite(spritePath)
                : null;

            if (sprite == null)
                sprite = Zone1ArtProvider.LoadSprite(emptySpritePath) ?? RuntimeSpriteFactory.OpaqueWhiteSprite;

            s.sr.sprite = sprite;
            s.sr.color = tint;
        }

        public void SetSlotColliderEnabled(int slotIndex, bool enabled)
        {
            var s = GetSlot(slotIndex);
            if (s?.col == null)
                return;
            s.col.enabled = enabled;
        }

        Slot GetSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
                return null;
            return _slots[slotIndex];
        }
    }

    [DisallowMultipleComponent]
    public sealed class Zone3CellSlot : MonoBehaviour
    {
        [SerializeField] int slotIndex;
        public int SlotIndex => slotIndex;
        public void Configure(int idx) => slotIndex = idx;
    }
}

