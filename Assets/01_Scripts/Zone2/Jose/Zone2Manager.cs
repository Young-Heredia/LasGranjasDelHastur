using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone1.Gacha;
using LasGranjasDelHastur.Zone1.UI;
using LasGranjasDelHastur.Zone2.Lucas;
using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1.Cells;
using UnityEngine;
using LasGranjasDelHastur.Camera;

namespace LasGranjasDelHastur.Zone2.Jose
{
    /// <summary>
    /// Zona 2 usando el stack de managers de Zona 1 para reutilizar exactamente su UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Zone2Manager : MonoBehaviour
    {
        [Header("Scene Wiring (runtime scaffold fills these)")]
        [SerializeField] ResourceManager resourceManager;
        [SerializeField] ProgressionManager progressionManager;
        [SerializeField] CellManager cellManager;
        [SerializeField] AssistantManager assistantManager;
        [SerializeField] BuyerManager buyerManager;
        [SerializeField] TaxManager taxManager;
        [SerializeField] UIManager uiManager;
        [SerializeField] Zone1GachaController gachaController;
        [Tooltip("Opcional: mismo prefab que Zona 1. Si null → Resources o UI runtime.")]
        [SerializeField] GameObject zone1GachaPanelPrefab;

        bool _initialized;
        bool _autoRestoredFromCache;

        void Awake()
        {
            AudioManager.EnsureInstance();
            TryInitialize();
        }

        void Start()
        {
            TryInitialize();
        }

        void OnDisable()
        {
            PersistToSaveCache();
        }

        [ContextMenu("Auto Wire References")]
        public void AutoWireReferences()
        {
            if (resourceManager == null) resourceManager = FindFirstObjectByType<ResourceManager>();
            if (progressionManager == null) progressionManager = FindFirstObjectByType<ProgressionManager>();
            if (cellManager == null) cellManager = FindFirstObjectByType<CellManager>();
            if (assistantManager == null) assistantManager = FindFirstObjectByType<AssistantManager>();
            if (buyerManager == null) buyerManager = FindFirstObjectByType<BuyerManager>();
            if (taxManager == null) taxManager = FindFirstObjectByType<TaxManager>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
            if (gachaController == null) gachaController = GetComponent<Zone1GachaController>();
            if (gachaController == null) gachaController = FindFirstObjectByType<Zone1GachaController>();
        }

        void TryInitialize()
        {
            if (_initialized)
                return;

            // Ensure scaffold exists (roots + camera + event system).
            Zone2RuntimeScaffold.EnsureSceneScaffold();

            AutoWireReferences();

            if (resourceManager == null || progressionManager == null || cellManager == null || assistantManager == null || buyerManager == null || taxManager == null || uiManager == null)
                return;

            // Mount the cell grid under the standard world slot root if present.
            var slots = GameObject.Find("WorldRoot")?.transform.Find("CellSlotsRoot");
            if (slots != null && cellManager.transform.parent != slots)
                cellManager.transform.SetParent(slots, worldPositionStays: false);

            // Basic camera bounds so movement works even without Zone1Config.
            var camController = FindFirstObjectByType<CameraController2D>();
            camController?.SetBounds(new Vector2(-30f, -18f), new Vector2(30f, 18f));

            cellManager.ConfigureGrid(
                Zone2CellGridLayout.Columns,
                Zone2CellGridLayout.Rows,
                Zone2CellGridLayout.Spacing,
                Zone2CellGridLayout.Origin,
                Zone2CellGridLayout.InitiallyUnlockedCells,
                Zone2CellGridLayout.InitiallyPurchasableCells);

            // Initialize stack exactly like Zone 1.
            cellManager.Initialize(resourceManager, progressionManager);
            assistantManager.Initialize(cellManager, resourceManager, progressionManager);
            buyerManager.Initialize(resourceManager, progressionManager);
            taxManager.Initialize(resourceManager, cellManager);
            TryRestoreFromCachedData();
            uiManager.Initialize(resourceManager, progressionManager, cellManager, assistantManager, buyerManager, taxManager);

            if (gachaController == null)
                gachaController = gameObject.AddComponent<Zone1GachaController>();
            gachaController.Setup(resourceManager, uiManager, zone1GachaPanelPrefab);

            var worldRoot = GameObject.Find("WorldRoot")?.transform;
            if (worldRoot != null)
            {
                Zone2GachaWorldBootstrap.EnsureStellarShields(worldRoot);
                Zone2PibleEasterEggBootstrap.EnsurePibles(worldRoot);
            }

            cellManager.RefreshAssistantVisuals(assistantManager);

            _initialized = true;
        }

        void TryRestoreFromCachedData()
        {
            var sm = SaveManager.Instance;
            if (sm == null || sm.CachedData == null)
                return;

            var data = sm.CachedData.zone2;
            if (data == null || !data.valid)
            {
                if (sm.ShouldRestoreFromSave)
                    sm.MarkRestoreConsumed();
                return;
            }

            if (!sm.ShouldRestoreFromSave && _autoRestoredFromCache)
                return;

            if (resourceManager != null)
                resourceManager.Set(ResourceType.DarkCoins, Mathf.Max(0, data.darkCoins));

            if (data.cells != null && data.cells.Count > 0)
            {
                for (var i = 0; i < data.cells.Count; i++)
                {
                    var saved = data.cells[i];
                    if (saved == null)
                        continue;
                    var cell = cellManager.GetCellBySlotIndex(saved.cellId);
                    if (cell == null)
                        continue;

                    var unlocked = saved.unlocked;
                    var state = unlocked
                        ? saved.corrupted
                            ? CellState.Corrupted
                            : saved.ready
                                ? CellState.ReadyToCollect
                                : saved.producing
                                    ? CellState.Producing
                                    : CellState.Available
                        : CellState.Blocked;
                    var level = Mathf.Max(1, saved.level);
                    cell.Configure(cell.SlotIndex, cell.Definition, state, level);
                    cell.RestoreState(state, level, saved.corrupted, saved.remainingSeconds);
                    cellManager.ApplyVisual(cell);
                }
            }

            assistantManager.ApplySaveData(Mathf.Max(1, data.assistantsTotal), data.assistants);
            cellManager.RefreshAssistantVisuals(assistantManager);
            uiManager.RefreshFromExternalState();

            if (sm.ShouldRestoreFromSave)
                sm.MarkRestoreConsumed();
            _autoRestoredFromCache = true;
        }

        void PersistToSaveCache()
        {
            var sm = SaveManager.Instance;
            if (!_initialized || sm == null || sm.CachedData == null || cellManager == null || assistantManager == null || resourceManager == null)
                return;

            var outData = sm.CachedData.zone2 ?? new Zone2SaveData();
            outData.valid = true;
            outData.darkCoins = resourceManager.Get(ResourceType.DarkCoins);
            outData.assistantsTotal = assistantManager.TotalAssistants;
            outData.assistants = assistantManager.CaptureSaveData();

            outData.cells ??= new System.Collections.Generic.List<Zone2CellSaveData>();
            outData.cells.Clear();
            var cells = cellManager.Cells;
            for (var i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                if (c == null)
                    continue;
                outData.cells.Add(new Zone2CellSaveData
                {
                    cellId = c.SlotIndex,
                    displayName = c.DisplayName,
                    unlocked = c.State != CellState.Blocked,
                    level = c.Level,
                    producing = c.State == CellState.Producing,
                    ready = c.State == CellState.ReadyToCollect,
                    corrupted = c.IsCorrupted,
                    remainingSeconds = c.ProducingRemainingSeconds,
                    assignedAssistants = assistantManager.GetAssistantCountOnCell(c),
                });
            }

            sm.CachedData.zone2 = outData;
            sm.CachedData.zone2Available = true;
            sm.WriteCachedDataNow();
        }
    }
}

