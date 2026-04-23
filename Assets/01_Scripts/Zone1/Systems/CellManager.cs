using System;
using System.Collections.Generic;
using LasGranjasDelHastur;
using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1.Cells;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    public class CellManager : MonoBehaviour
    {
        public event Action<FarmCell> SelectedCellChanged;
        public event Action CellsChanged;

        [Header("Grid Layout")]
        [SerializeField] private int columns = 4;
        [SerializeField] private int rows = 3;
        [SerializeField] private Vector2 cellSpacing = new(2.2f, 2.2f);
        [SerializeField] private Vector2 origin = new(-3.3f, 1.8f);

        [Header("Definitions (optional; runtime defaults if empty)")]
        [SerializeField] private List<Zone1CellDefinition> cellDefinitions = new();

        [Header("Starting Setup")]
        [SerializeField] private int initiallyUnlockedCells = 1; // available at start
        [SerializeField] private int initiallyPurchasableCells = 2; // blocked but visible for buying

        [Header("Economy Scaling")]
        [SerializeField, Range(0f, 1f)] private float purchaseSlotScalePercent = 0.12f;
        [SerializeField, Min(1)] private int upgradeBaseCost = 25;
        [SerializeField, Min(0)] private int upgradePerLevelAdd = 20;
        [SerializeField, Range(1f, 3f)] private float upgradeLevelMultiplier = 1.15f;

        readonly List<FarmCell> _cells = new();
        readonly Dictionary<Zone1CellType, Zone1CellDefinition> _defsByType = new();
        readonly Dictionary<FarmCell, GameObject> _selectionRings = new();
        readonly Dictionary<FarmCell, GameObject> _readyPulses = new();
        readonly Dictionary<FarmCell, GameObject> _assistantMarkers = new();

        ResourceManager _resources;
        ProgressionManager _progression;
        bool _initialized;

        public IReadOnlyList<FarmCell> Cells => _cells;
        public FarmCell SelectedCell { get; private set; }
        public FarmCell GetCellBySlotIndex(int slotIndex)
        {
            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell != null && cell.SlotIndex == slotIndex)
                    return cell;
            }
            return null;
        }

        public void Initialize(ResourceManager resources, ProgressionManager progression)
        {
            if (_initialized)
            {
                _resources = resources;
                _progression = progression;
                return;
            }

            _resources = resources;
            _progression = progression;

            if (cellDefinitions == null || cellDefinitions.Count == 0)
                cellDefinitions = CreateRuntimeDefaultCellDefs();

            _defsByType.Clear();
            foreach (var def in cellDefinitions)
            {
                if (def == null)
                    continue;
                _defsByType[def.cellType] = def;
            }

            BuildGridIfEmpty();
            ApplyInitialUnlocks();

            CellsChanged?.Invoke();
            _initialized = true;
        }

        void Update()
        {
            if (!_initialized || !InputAdapter.LeftMouseDownThisFrame())
                return;

            // Ignore world click when user is interacting with UI.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var cam = UnityEngine.Camera.main;
            if (cam == null)
                return;

            var world = cam.ScreenToWorldPoint(InputAdapter.MousePosition());
            var hit = Physics2D.OverlapPoint(new Vector2(world.x, world.y));
            if (hit == null)
                return;

            var cell = hit.GetComponent<FarmCell>();
            if (cell == null)
                cell = hit.GetComponentInParent<FarmCell>();
            if (cell == null)
                return;

            SelectCell(cell);
        }

        public void ConfigureGrid(int newColumns, int newRows, Vector2 newSpacing, Vector2 newOrigin, int unlockedAtStart, int purchasableAtStart)
        {
            columns = Mathf.Max(1, newColumns);
            rows = Mathf.Max(1, newRows);
            cellSpacing = newSpacing;
            origin = newOrigin;
            initiallyUnlockedCells = Mathf.Max(0, unlockedAtStart);
            initiallyPurchasableCells = Mathf.Max(0, purchasableAtStart);
        }

        public void ConfigureEconomy(float newPurchaseSlotScalePercent, int newUpgradeBaseCost, int newUpgradePerLevelAdd, float newUpgradeLevelMultiplier)
        {
            purchaseSlotScalePercent = Mathf.Clamp(newPurchaseSlotScalePercent, 0f, 1f);
            upgradeBaseCost = Mathf.Max(1, newUpgradeBaseCost);
            upgradePerLevelAdd = Mathf.Max(0, newUpgradePerLevelAdd);
            upgradeLevelMultiplier = Mathf.Max(1f, newUpgradeLevelMultiplier);

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell == null)
                    continue;
                cell.ConfigureEconomy(purchaseSlotScalePercent, upgradeBaseCost, upgradePerLevelAdd, upgradeLevelMultiplier);
            }
        }

        void BuildGridIfEmpty()
        {
            if (_cells.Count > 0)
                return;

            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                {
                    var slotIndex = r * columns + c;
                    var go = new GameObject($"CellSlot_{slotIndex:00}");
                    go.transform.SetParent(transform, false);
                    go.transform.position = new Vector3(origin.x + c * cellSpacing.x, origin.y - r * cellSpacing.y, 0f);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Zone1/Cells/zone1_soulpit_blocked.png") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
                    go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                    sr.color = new Color(0.12f, 0.12f, 0.14f, 1f);
                    sr.sortingOrder = 40 + (rows - r) * 4;

                    var col = go.AddComponent<BoxCollider2D>();
                    col.size = Vector2.one;

                    var cell = go.AddComponent<FarmCell>();
                    cell.Configure(slotIndex, GetDef(Zone1CellType.SoulPit), CellState.Blocked, 1);
                    cell.ConfigureEconomy(purchaseSlotScalePercent, upgradeBaseCost, upgradePerLevelAdd, upgradeLevelMultiplier);
                    cell.Changed += _ => CellsChanged?.Invoke();

                    var clickable = go.AddComponent<WorldCellClickable>();
                    clickable.Bind(this, cell);

                    SetupCellFx(go.transform, cell, sr.sortingOrder);
                    _cells.Add(cell);
                }
            }
        }

        void SetupCellFx(Transform parent, FarmCell cell, int baseSortingOrder)
        {
            var shadow = new GameObject("GroundShadow");
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = new Vector3(0f, -0.58f, 0f);
            shadow.transform.localScale = new Vector3(1.1f, 0.55f, 1f);
            var shadowSr = shadow.AddComponent<SpriteRenderer>();
            shadowSr.sprite = RuntimeSpriteFactory.OpaqueWhiteSprite;
            shadowSr.color = new Color(0f, 0f, 0f, 0.28f);
            shadowSr.sortingOrder = baseSortingOrder - 1;

            var ring = new GameObject("SelectionRing");
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = new Vector3(0f, -0.65f, 0f);
            ring.transform.localScale = new Vector3(1.1f, 0.45f, 1f);
            var ringSr = ring.AddComponent<SpriteRenderer>();
            ringSr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Zone1/Spritesheets/zone1_select_ring_sheet.png");
            ringSr.color = new Color(1f, 1f, 1f, 0.9f);
            ringSr.sortingOrder = baseSortingOrder + 3;
            var ringAnim = ring.AddComponent<SpriteSheetAnimator>();
            ringAnim.Configure("Assets/02_Sprites/Zone1/Spritesheets/zone1_select_ring_sheet.png", 32, 32, 8f);
            ring.SetActive(false);
            _selectionRings[cell] = ring;

            var ready = new GameObject("ReadyPulse");
            ready.transform.SetParent(parent, false);
            ready.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            ready.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            var readySr = ready.AddComponent<SpriteRenderer>();
            readySr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Zone1/Spritesheets/zone1_ready_collect_sheet.png");
            readySr.color = Color.white;
            readySr.sortingOrder = baseSortingOrder + 4;
            var readyAnim = ready.AddComponent<SpriteSheetAnimator>();
            readyAnim.Configure("Assets/02_Sprites/Zone1/Spritesheets/zone1_ready_collect_sheet.png", 32, 32, 10f);
            ready.SetActive(false);
            _readyPulses[cell] = ready;

            var assistant = new GameObject("AssistantMarker");
            assistant.transform.SetParent(parent, false);
            assistant.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            assistant.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            var assistantSr = assistant.AddComponent<SpriteRenderer>();
            assistantSr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Zone1/Icons/zone1_icon_level.png")
                ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            assistantSr.color = new Color(0.80f, 0.95f, 1f, 0.95f);
            assistantSr.sortingOrder = baseSortingOrder + 5;
            assistant.SetActive(false);
            _assistantMarkers[cell] = assistant;
        }

        void ApplyInitialUnlocks()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                if (i < initiallyUnlockedCells)
                {
                    var cell = _cells[i];
                    var def = GetDef(Zone1CellType.SoulPit);
                    cell.Configure(cell.SlotIndex, def, CellState.Available, 1);
                    ApplyVisual(cell);
                }
                else if (i < initiallyUnlockedCells + initiallyPurchasableCells)
                {
                    var cell = _cells[i];
                    cell.Configure(cell.SlotIndex, GetDef(Zone1CellType.EnergyWell), CellState.Blocked, 1);
                    ApplyVisual(cell);
                }
                else
                {
                    ApplyVisual(_cells[i]);
                }
            }
        }

        public void SelectCell(FarmCell cell)
        {
            if (SelectedCell == cell)
            {
                SelectedCellChanged?.Invoke(cell);
                return;
            }
            SelectedCell = cell;
            RefreshSelectionFx();
            SelectedCellChanged?.Invoke(cell);
        }

        public void ClearSelection(bool notify = true)
        {
            SelectedCell = null;
            RefreshSelectionFx();
            if (notify)
                SelectedCellChanged?.Invoke(null);
        }

        void RefreshSelectionFx()
        {
            foreach (var kv in _selectionRings)
            {
                if (kv.Value == null || kv.Key == null)
                    continue;
                kv.Value.SetActive(kv.Key == SelectedCell);
            }
        }

        public bool TryCorruptRandomCell()
        {
            var candidates = new List<FarmCell>();
            foreach (var c in _cells)
            {
                if (c == null)
                    continue;
                if (c.State == CellState.Available || c.State == CellState.ReadyToCollect || c.State == CellState.Producing)
                    if (!c.IsCorrupted)
                        candidates.Add(c);
            }

            if (candidates.Count == 0)
                return false;

            var chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            chosen.Corrupt();
            ApplyVisual(chosen);
            return true;
        }

        public void RefreshAllVisuals()
        {
            foreach (var c in _cells)
                ApplyVisual(c);
        }

        public void RefreshAssistantVisuals(AssistantManager assistants)
        {
            foreach (var c in _cells)
            {
                if (c == null)
                    continue;
                if (!_assistantMarkers.TryGetValue(c, out var marker) || marker == null)
                    continue;

                var show = assistants != null && assistants.HasAssistantOnCell(c);
                marker.SetActive(show);
            }
        }

        public void ApplyVisual(FarmCell cell)
        {
            if (cell == null)
                return;
            var sr = cell.GetComponent<SpriteRenderer>();
            if (sr == null)
                return;

            var spritePath = GetCellSpritePath(cell);
            var sprite = Zone1ArtProvider.LoadSprite(spritePath);
            if (sprite != null)
                sr.sprite = sprite;

            sr.color = Color.white;
            if (_readyPulses.TryGetValue(cell, out var pulse) && pulse != null)
                pulse.SetActive(cell.State == CellState.ReadyToCollect && !cell.IsCorrupted);
        }

        static string GetCellSpritePath(FarmCell cell)
        {
            var type = cell.CellType switch
            {
                Zone1CellType.SoulPit => "soulpit",
                Zone1CellType.EnergyWell => "energywell",
                Zone1CellType.EchoChamber => "echochamber",
                Zone1CellType.BrokenAltar => "brokenaltar",
                _ => "soulpit"
            };

            var state = cell.IsCorrupted ? "corrupt" : cell.State switch
            {
                CellState.Blocked => "blocked",
                CellState.Producing => "producing",
                CellState.ReadyToCollect => "ready",
                _ => "idle"
            };

            return $"Assets/02_Sprites/Zone1/Cells/zone1_{type}_{state}.png";
        }

        public List<CellSaveData> CaptureSaveData()
        {
            var data = new List<CellSaveData>(_cells.Count);
            foreach (var c in _cells)
            {
                if (c == null)
                    continue;
                data.Add(new CellSaveData
                {
                    slotIndex = c.SlotIndex,
                    cellType = c.CellType,
                    state = c.State,
                    level = c.Level,
                    isCorrupted = c.IsCorrupted,
                    producingRemainingSeconds = c.ProducingRemainingSeconds,
                });
            }
            return data;
        }

        public void ApplySaveData(List<CellSaveData> saveCells)
        {
            if (saveCells == null || saveCells.Count == 0)
                return;

            var bySlot = new Dictionary<int, CellSaveData>();
            foreach (var saved in saveCells)
                bySlot[saved.slotIndex] = saved;

            foreach (var c in _cells)
            {
                if (c == null)
                    continue;
                if (!bySlot.TryGetValue(c.SlotIndex, out var saved))
                    continue;

                var def = GetDef(saved.cellType);
                c.Configure(c.SlotIndex, def, saved.state, saved.level);
                c.RestoreState(saved.state, saved.level, saved.isCorrupted, saved.producingRemainingSeconds);
                ApplyVisual(c);
            }

            CellsChanged?.Invoke();
        }

        Zone1CellDefinition GetDef(Zone1CellType type)
        {
            if (_defsByType.TryGetValue(type, out var d) && d != null)
                return d;
            return cellDefinitions != null && cellDefinitions.Count > 0 ? cellDefinitions[0] : null;
        }

        List<Zone1CellDefinition> CreateRuntimeDefaultCellDefs()
        {
            var list = new List<Zone1CellDefinition>();

            var soulPit = ScriptableObject.CreateInstance<Zone1CellDefinition>();
            soulPit.displayName = "Fosa de Almas";
            soulPit.cellType = Zone1CellType.SoulPit;
            soulPit.producesResource = ResourceType.WeakSouls;
            soulPit.productionSeconds = 4f;
            soulPit.productionAmount = 2;
            soulPit.purchaseCostDarkCoins = 0;
            soulPit.corruptionRiskOnCollect = 0f;
            list.Add(soulPit);

            var energyWell = ScriptableObject.CreateInstance<Zone1CellDefinition>();
            energyWell.displayName = "Pozo de Energía";
            energyWell.cellType = Zone1CellType.EnergyWell;
            energyWell.producesResource = ResourceType.PureEnergy;
            energyWell.productionSeconds = 6f;
            energyWell.productionAmount = 1;
            energyWell.purchaseCostDarkCoins = 60;
            energyWell.corruptionRiskOnCollect = 0f;
            list.Add(energyWell);

            var echo = ScriptableObject.CreateInstance<Zone1CellDefinition>();
            echo.displayName = "Cámara de Ecos";
            echo.cellType = Zone1CellType.EchoChamber;
            echo.producesResource = ResourceType.MemoryShards;
            echo.productionSeconds = 9f;
            echo.productionAmount = 1;
            echo.purchaseCostDarkCoins = 120;
            echo.corruptionRiskOnCollect = 0f;
            list.Add(echo);

            var altar = ScriptableObject.CreateInstance<Zone1CellDefinition>();
            altar.displayName = "Altar Roto";
            altar.cellType = Zone1CellType.BrokenAltar;
            altar.producesResource = ResourceType.UnstableSouls;
            altar.productionSeconds = 11f;
            altar.productionAmount = 1;
            altar.purchaseCostDarkCoins = 200;
            altar.corruptionRiskOnCollect = 0.12f;
            list.Add(altar);

            return list;
        }
    }
}

