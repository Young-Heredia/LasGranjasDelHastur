using System;
using System.Collections.Generic;
using LasGranjasDelHastur;
using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1.Cells;
using LasGranjasDelHastur.Zone1.Gacha;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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

        [Header("Generator Rules")]
        [Tooltip("Máximo de celdas tipo Fosa de Almas (generador de almas débiles) que el jugador puede tener desbloqueadas a la vez (incluye la inicial).")]
        [SerializeField, Min(1)] private int maxOwnedSoulPitCells = 2;

        [Header("Prefab (optional)")]
        [Tooltip("Raíz: SpriteRenderer, BoxCollider2D, FarmCell, WorldCellClickable, hijos GroundShadow, SelectionRing, ReadyPulse, ProducingFx, ReadyFx, AssistantMarker. Generar con Jose/Editor: Bake FarmCellSlot prefab. Si null, se construye en código como antes.")]
        [SerializeField] private GameObject cellSlotPrefab;

        readonly List<FarmCell> _cells = new();
        readonly Dictionary<Zone1CellType, Zone1CellDefinition> _defsByType = new();
        readonly Dictionary<FarmCell, GameObject> _selectionRings = new();
        readonly Dictionary<FarmCell, GameObject> _readyPulses = new();
        readonly Dictionary<FarmCell, GameObject> _producingParticles = new();
        readonly Dictionary<FarmCell, GameObject> _readyParticles = new();
        readonly Dictionary<FarmCell, GameObject> _assistantMarkers = new();

        ResourceManager _resources;
        bool _initialized;

        public IReadOnlyList<FarmCell> Cells => _cells;
        public FarmCell SelectedCell { get; private set; }

        public int CountPurchasedCellsForTax()
        {
            var n = 0;
            for (var i = 0; i < _cells.Count; i++)
            {
                var c = _cells[i];
                if (c == null)
                    continue;
                // Blocked == not bought. Any other state means the player owns a producing slot.
                if (c.State != CellState.Blocked)
                    n++;
            }
            return n;
        }
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
                return;
            }

            _resources = resources;

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
            SpreadPurchasableDefinitionsAcrossBlockedSlots();
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
            var point = new Vector2(world.x, world.y);
            var hits = Physics2D.OverlapPointAll(point);
            if (hits == null || hits.Length == 0)
                return;

            for (var i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h == null)
                    continue;
                if (HasGachaWorldTrigger(h.gameObject))
                {
                    Zone1GachaController.Instance?.OpenFromWorld();
                    return;
                }
            }

            FarmCell cell = null;
            for (var i = 0; i < hits.Length; i++)
            {
                var c = hits[i].GetComponent<FarmCell>() ?? hits[i].GetComponentInParent<FarmCell>();
                if (c != null)
                {
                    cell = c;
                    break;
                }
            }

            if (cell == null)
                return;

            SelectCell(cell);
        }

        static bool HasGachaWorldTrigger(GameObject go)
        {
            return go.GetComponent<Zone1GachaFountainInteract>() != null ||
                   go.GetComponentInParent<Zone1GachaFountainInteract>() != null ||
                   go.GetComponent<GachaWorldInteract>() != null ||
                   go.GetComponentInParent<GachaWorldInteract>() != null;
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

        public bool TryPurchaseCell(FarmCell cell)
        {
            if (cell == null || _resources == null)
                return false;
            if (cell.State != CellState.Blocked)
                return false;

            var def = ResolvePurchasableDefinitionForBlockedCell(cell);
            if (def == null)
                return false;

            cell.Configure(cell.SlotIndex, def, CellState.Blocked, 1);
            cell.ConfigureEconomy(purchaseSlotScalePercent, upgradeBaseCost, upgradePerLevelAdd, upgradeLevelMultiplier);

            if (!cell.TryBuy(_resources))
                return false;

            ApplyVisual(cell);
            CellsChanged?.Invoke();
            return true;
        }

        public bool CanPurchaseBlockedSlot(FarmCell cell)
        {
            if (cell == null || _resources == null)
                return false;
            if (cell.State != CellState.Blocked)
                return false;

            var def = ResolvePurchasableDefinitionForBlockedCell(cell);
            if (def == null)
                return false;

            var cost = CalculatePurchaseCostDarkCoins(def, cell.SlotIndex);
            return _resources.Get(ResourceType.DarkCoins) >= cost;
        }

        public bool TryGetBlockedPurchasePreview(FarmCell cell, out Zone1CellDefinition definition, out int purchaseCostDarkCoins, out string displayName)
        {
            definition = null;
            purchaseCostDarkCoins = 0;
            displayName = "";

            if (cell == null || cell.State != CellState.Blocked)
                return false;

            var def = ResolvePurchasableDefinitionForBlockedCell(cell);
            if (def == null)
                return false;

            definition = def;
            purchaseCostDarkCoins = CalculatePurchaseCostDarkCoins(def, cell.SlotIndex);
            displayName = def != null ? def.displayName : cell.DisplayName;
            return true;
        }

        int CountOwnedSoulPits()
        {
            var n = 0;
            for (var i = 0; i < _cells.Count; i++)
            {
                var c = _cells[i];
                if (c == null || c.State == CellState.Blocked)
                    continue;
                if (c.CellType == Zone1CellType.SoulPit)
                    n++;
            }
            return n;
        }

        Zone1CellDefinition ResolvePurchasableDefinitionForBlockedCell(FarmCell cell)
        {
            var desired = cell != null ? cell.CellType : Zone1CellType.SoulPit;

            if (desired == Zone1CellType.SoulPit && CountOwnedSoulPits() >= maxOwnedSoulPitCells)
                desired = Zone1CellType.EnergyWell;

            var def = GetDef(desired);

            if (def != null && def.cellType == Zone1CellType.SoulPit && CountOwnedSoulPits() >= maxOwnedSoulPitCells)
                def = null;

            if (def != null)
                return def;

            // Map purchases use the cell layout (no level gate): pick next non-soul type by slot.
            var rotation = new[] { Zone1CellType.EnergyWell, Zone1CellType.EchoChamber, Zone1CellType.BrokenAltar };
            var start = Mathf.Abs(cell.SlotIndex) % rotation.Length;
            for (var k = 0; k < rotation.Length; k++)
            {
                var t = rotation[(start + k) % rotation.Length];
                var d = GetDef(t);
                if (d != null)
                    return d;
            }

            if (CountOwnedSoulPits() < maxOwnedSoulPitCells)
                return GetDef(Zone1CellType.SoulPit);

            return null;
        }

        int CalculatePurchaseCostDarkCoins(Zone1CellDefinition def, int slotIndex)
        {
            var baseCost = def != null ? def.purchaseCostDarkCoins : 50;
            return Mathf.Max(0, Mathf.RoundToInt(baseCost * (1f + purchaseSlotScalePercent * slotIndex)));
        }

        void SpreadPurchasableDefinitionsAcrossBlockedSlots()
        {
            if (_cells.Count == 0)
                return;

            var start = Mathf.Clamp(initiallyUnlockedCells + initiallyPurchasableCells, 0, _cells.Count);
            if (start >= _cells.Count)
                return;

            var pattern = new[]
            {
                Zone1CellType.EnergyWell,
                Zone1CellType.EchoChamber,
                Zone1CellType.BrokenAltar,
                Zone1CellType.EnergyWell,
            };

            var p = 0;
            for (var i = start; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell == null || cell.State != CellState.Blocked)
                    continue;

                var t = pattern[p % pattern.Length];
                p++;

                var def = GetDef(t);

                if (def == null)
                    def = GetDef(Zone1CellType.SoulPit);

                if (def == null)
                    continue;

                cell.Configure(cell.SlotIndex, def, CellState.Blocked, 1);
                cell.ConfigureEconomy(purchaseSlotScalePercent, upgradeBaseCost, upgradePerLevelAdd, upgradeLevelMultiplier);
                ApplyVisual(cell);
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
                    var baseSorting = 40 + (rows - r) * 4;
                    var go = CreateCellGameObject(slotIndex, r, c);

                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr == null)
                        sr = go.AddComponent<SpriteRenderer>();

                    var blockedPath = CellSpritePathResolver.ResolveByFileName("zone1_soulpit_blocked.png");
                    if (sr.sprite == null)
                        sr.sprite = Zone1ArtProvider.LoadSprite(blockedPath) ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
                    sr.color = new Color(0.12f, 0.12f, 0.14f, 1f);

                    sr.sortingOrder = baseSorting;

                    var box = go.GetComponent<BoxCollider2D>();
                    if (box == null)
                        box = go.AddComponent<BoxCollider2D>();
                    box.size = Vector2.one;

                    var cell = go.GetComponent<FarmCell>();
                    if (cell == null)
                        cell = go.AddComponent<FarmCell>();
                    cell.Configure(slotIndex, GetDef(Zone1CellType.SoulPit), CellState.Blocked, 1);
                    cell.ConfigureEconomy(purchaseSlotScalePercent, upgradeBaseCost, upgradePerLevelAdd, upgradeLevelMultiplier);
                    cell.Changed += _ => CellsChanged?.Invoke();

                    var clickable = go.GetComponent<WorldCellClickable>();
                    if (clickable == null)
                        clickable = go.AddComponent<WorldCellClickable>();
                    clickable.Bind(this, cell);

                    var fx = FarmCellSlotHierarchy.Ensure(go.transform, baseSorting);
                    _selectionRings[cell] = fx.SelectionRing;
                    _readyPulses[cell] = fx.ReadyPulse;
                    _producingParticles[cell] = fx.ProducingFx;
                    _readyParticles[cell] = fx.ReadyFx;
                    _assistantMarkers[cell] = fx.AssistantMarker;
                    _cells.Add(cell);
                }
            }
        }

        GameObject CreateCellGameObject(int slotIndex, int r, int c)
        {
            var pos = new Vector3(origin.x + c * cellSpacing.x, origin.y - r * cellSpacing.y, 0f);
            if (SceneManager.GetActiveScene().name == "Zone2_Cities")
            {
                var wobble = LasGranjasDelHastur.Zone2.Jose.Zone2CellGridLayout.SlotWobble(slotIndex);
                pos.x += wobble.x;
                pos.y += wobble.y;
            }

            var slotScale = SceneManager.GetActiveScene().name == "Zone2_Cities" ? 1.05f : 1.2f;

            if (cellSlotPrefab != null)
            {
                var go = Instantiate(cellSlotPrefab, transform, false);
                go.name = $"CellSlot_{slotIndex:00}";
                go.transform.position = pos;
                go.transform.localScale = new Vector3(slotScale, slotScale, 1f);
                return go;
            }

            var g = new GameObject($"CellSlot_{slotIndex:00}");
            g.transform.SetParent(transform, false);
            g.transform.position = pos;
            g.transform.localScale = new Vector3(slotScale, slotScale, 1f);
            return g;
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

            RefreshCellStateParticles(cell);
        }

        void RefreshCellStateParticles(FarmCell cell)
        {
            if (cell == null)
                return;
            var corrupt = cell.IsCorrupted;
            SetLoopingParticlesActive(_producingParticles, cell, !corrupt && cell.State == CellState.Producing);
            SetLoopingParticlesActive(_readyParticles, cell, !corrupt && cell.State == CellState.ReadyToCollect);
        }

        static void SetLoopingParticlesActive(Dictionary<FarmCell, GameObject> hosts, FarmCell cell, bool active)
        {
            if (!hosts.TryGetValue(cell, out var go) || go == null)
                return;
            var ps = go.GetComponent<ParticleSystem>();
            if (active)
            {
                if (!go.activeSelf)
                    go.SetActive(true);
                if (ps != null && !ps.isPlaying)
                    ps.Play();
            }
            else
            {
                if (ps != null)
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (go.activeSelf)
                    go.SetActive(false);
            }
        }

        static string GetCellSpritePath(FarmCell cell) => CellSpritePathResolver.ResolveForCell(cell);

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
            soulPit.purchaseCostDarkCoins = 45;
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

