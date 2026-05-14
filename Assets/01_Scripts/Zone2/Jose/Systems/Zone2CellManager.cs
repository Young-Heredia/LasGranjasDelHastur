using System;
using System.Collections.Generic;
using LasGranjasDelHastur;
using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone2;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LasGranjasDelHastur.Zone2.Jose.Systems
{
    [DisallowMultipleComponent]
    public sealed class Zone2CellManager : MonoBehaviour
    {
        public event Action<int> SelectedSlotChanged;
        public event Action CellsChanged;

        [Header("Grid Layout (6×5 = 30, mismo espaciado base que Zona 1)")]
        [SerializeField] int columns = 6;
        [SerializeField] int rows = 5;
        [SerializeField] Vector2 spacing = new(2.14f, 1.92f);
        [SerializeField] Vector2 origin = new(-5.35f, 3.65f);

        [Header("Sprites (pack defaults)")]
        [SerializeField] string lockedSpritePath = "Assets/02_Sprites/Lucas/Zone2/Cells/Buildings/z2_building_souls_pit_blocked.png";
        [SerializeField] string emptySpritePath = "Assets/02_Sprites/Lucas/Zone2/Cells/Buildings/z2_building_souls_pit_idle.png";

        [Header("Opcional")]
        [Tooltip("Si se asigna, se instancia en lugar de crear celdas en vacío (recomendado: bake desde menú Editor Zona 2).")]
        [SerializeField] GameObject cellSlotPrefab;

        readonly List<Slot> _slots = new();

        public int SelectedSlotIndex { get; private set; } = -1;
        public int SlotCount => _slots.Count;

        sealed class Slot
        {
            public int index;
            public GameObject go;
            public SpriteRenderer sr;
            public BoxCollider2D col;
            public Zone2CellVisualDriver driver;
        }

        void Awake()
        {
            NormalizeSpriteDefaults();
            BuildGridIfEmpty();
        }

        void NormalizeSpriteDefaults()
        {
            // Force Zone2 "new cells" set if scene still had old pack defaults serialized.
            if (string.IsNullOrWhiteSpace(lockedSpritePath) ||
                lockedSpritePath.IndexOf("hastur_pixel_art_pack/Cells/Base/Cell_Locked", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                lockedSpritePath = "Assets/02_Sprites/Lucas/Zone2/Cells/Buildings/z2_building_souls_pit_blocked.png";
            }

            if (string.IsNullOrWhiteSpace(emptySpritePath) ||
                emptySpritePath.IndexOf("hastur_pixel_art_pack/Cells/Base/Cell_Empty", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                emptySpritePath = "Assets/02_Sprites/Lucas/Zone2/Cells/Buildings/z2_building_souls_pit_idle.png";
            }
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

            var slot = hit.GetComponent<Zone2CellSlot>() ?? hit.GetComponentInParent<Zone2CellSlot>();
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
                    var pos = new Vector3(origin.x + c * spacing.x, origin.y - r * spacing.y, 0f);
                    var sortOrder = 40 + (rows - r) * 4;

                    GameObject go;
                    SpriteRenderer sr;
                    BoxCollider2D col;
                    Zone2CellVisualDriver driver;

                    if (cellSlotPrefab != null)
                    {
                        go = Instantiate(cellSlotPrefab, transform, false);
                        go.name = $"CellSlot_{idx:00}";
                        go.transform.position = pos;
                        driver = go.GetComponent<Zone2CellVisualDriver>();
                        if (driver == null)
                            driver = go.AddComponent<Zone2CellVisualDriver>();
                        sr = driver.MainRenderer != null
                            ? driver.MainRenderer
                            : go.GetComponent<SpriteRenderer>() ?? go.GetComponentInChildren<SpriteRenderer>();
                        if (driver.MainRenderer == null && sr != null)
                            driver.Initialize(sr);
                        col = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
                        if (col.size.sqrMagnitude < 0.01f)
                            col.size = Vector2.one;
                    }
                    else
                    {
                        go = new GameObject($"CellSlot_{idx:00}");
                        go.transform.SetParent(transform, false);
                        go.transform.position = pos;

                        sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = locked;
                        sr.color = new Color(0.12f, 0.12f, 0.14f, 1f);
                        sr.sortingOrder = sortOrder;
                        go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

                        col = go.AddComponent<BoxCollider2D>();
                        col.size = Vector2.one;

                        driver = go.AddComponent<Zone2CellVisualDriver>();
                        driver.Initialize(sr);
                    }

                    sr.sprite = locked;
                    sr.color = new Color(0.12f, 0.12f, 0.14f, 1f);
                    sr.sortingOrder = sortOrder;

                    var slot = go.GetComponent<Zone2CellSlot>() ?? go.AddComponent<Zone2CellSlot>();
                    slot.Configure(idx);

                    _slots.Add(new Slot { index = idx, go = go, sr = sr, col = col, driver = driver });
                }
            }

            CellsChanged?.Invoke();
        }

        public void ApplyDistrictVisual(int slotIndex, Zone2DistrictType district, Zone2CellVisualState state, int cellLevel = 1, bool cellUnlocked = true)
        {
            var s = GetSlot(slotIndex);
            if (s?.driver != null)
            {
                s.driver.enabled = true;
                s.driver.Apply(district, state, cellLevel, cellUnlocked);
                return;
            }

            var path = Zone2DistrictPaths.GetSpritePath(district);
            var sprite = Zone1ArtProvider.LoadSprite(path)
                ?? Zone1ArtProvider.LoadSprite(emptySpritePath)
                ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            if (s?.sr == null)
                return;
            s.sr.sprite = sprite;
            var col = Zone2CellVisualDriverTintOnly(state);
            if (cellUnlocked)
                col *= Zone2CellLevelRules.LevelTintForMainSprite(cellLevel, true);
            s.sr.color = col;
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
            if (s.driver != null)
                s.driver.enabled = false;
        }

        static Color Zone2CellVisualDriverTintOnly(Zone2CellVisualState state)
        {
            return state switch
            {
                Zone2CellVisualState.Locked => new Color(0.55f, 0.55f, 0.60f, 1f),
                Zone2CellVisualState.Idle => Color.white,
                Zone2CellVisualState.Producing => new Color(0.75f, 0.90f, 1f, 1f),
                Zone2CellVisualState.Ready => new Color(1f, 0.96f, 0.80f, 1f),
                Zone2CellVisualState.Corrupted => new Color(0.75f, 0.55f, 0.90f, 1f),
                _ => Color.white,
            };
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
    public sealed class Zone2CellSlot : MonoBehaviour
    {
        [SerializeField] int slotIndex;
        public int SlotIndex => slotIndex;
        public void Configure(int idx) => slotIndex = idx;
    }
}

