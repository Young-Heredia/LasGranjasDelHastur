using LasGranjasDelHastur.Camera;
using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1.UI;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    public class Zone1Manager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private Zone1Config zone1Config;

        [Header("Scene Wiring (runtime bootstrap fills these)")]
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private ProgressionManager progressionManager;
        [SerializeField] private CellManager cellManager;
        [SerializeField] private AssistantManager assistantManager;
        [SerializeField] private BuyerManager buyerManager;
        [SerializeField] private TaxManager taxManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private Zone1ArtTuner artTuner;

        [Header("Camera")]
        [SerializeField] private CameraController2D cameraController;

        [Header("Editor Debug")]
        [SerializeField] private bool balanceDebugLogs;
        [SerializeField, Min(1f)] private float balanceDebugInterval = 5f;

        public ResourceManager Resources => resourceManager;
        public ProgressionManager Progression => progressionManager;
        public CellManager Cells => cellManager;
        public AssistantManager Assistants => assistantManager;
        public BuyerManager Buyers => buyerManager;
        public TaxManager Taxes => taxManager;
        public Zone1Config Config => zone1Config;
        bool _initialized;
        float _debugTimer;

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
            if (cameraController == null) cameraController = FindFirstObjectByType<CameraController2D>();
            if (artTuner == null) artTuner = FindFirstObjectByType<Zone1ArtTuner>();
        }

        void Awake()
        {
            AudioManager.EnsureInstance();
            TryInitialize();
        }

        void Start()
        {
            TryInitialize();
        }

        void Update()
        {
            if (!_initialized)
            {
                TryInitialize();
                return;
            }

            HandleDebugTelemetry();
        }

        void HandleDebugTelemetry()
        {
            if (InputAdapter.KeyDown(KeyCode.F7))
                balanceDebugLogs = !balanceDebugLogs;

            if (!balanceDebugLogs)
                return;

            _debugTimer += Time.deltaTime;
            if (_debugTimer < balanceDebugInterval)
                return;
            _debugTimer = 0f;

            Debug.Log(
                $"[Zone1 Balance] Tax={taxManager.CalculateTaxAmount()} NextTax={taxManager.TimeToNextTaxSeconds:0.0}s " +
                $"Coins={resourceManager.Get(ResourceType.DarkCoins)} WS={resourceManager.Get(ResourceType.WeakSouls)}/{resourceManager.GetCapacity(ResourceType.WeakSouls)} " +
                $"PE={resourceManager.Get(ResourceType.PureEnergy)}/{resourceManager.GetCapacity(ResourceType.PureEnergy)} " +
                $"Buyers={BuildBuyerDebugLine()}");
        }

        string BuildBuyerDebugLine()
        {
            if (buyerManager == null || buyerManager.Buyers == null || buyerManager.Buyers.Count == 0)
                return "-";

            var entries = "";
            for (var i = 0; i < buyerManager.Buyers.Count; i++)
            {
                var buyer = buyerManager.Buyers[i];
                if (buyer == null)
                    continue;
                if (entries.Length > 0)
                    entries += " | ";
                entries += $"{buyer.buyerName}:{buyerManager.GetCurrentPrice(buyer)}/u";
            }

            return string.IsNullOrEmpty(entries) ? "-" : entries;
        }

        void TryInitialize()
        {
            if (_initialized)
                return;

            EnsureConfig();
            AutoWireReferences();

            if (resourceManager == null || progressionManager == null || cellManager == null || assistantManager == null || buyerManager == null || taxManager == null || uiManager == null)
            {
                return;
            }

            ApplyConfig();
            cellManager.Initialize(resourceManager, progressionManager);
            assistantManager.Initialize(cellManager, resourceManager, progressionManager);
            assistantManager.Changed += OnAssistantAssignmentsChanged;
            buyerManager.Initialize(resourceManager, progressionManager);
            taxManager.Initialize(resourceManager, cellManager);
            uiManager.Initialize(resourceManager, progressionManager, cellManager, assistantManager, buyerManager, taxManager);

            // Camera bounds tuned by config.
            if (cameraController != null)
                cameraController.SetBounds(zone1Config.cameraMinBounds, zone1Config.cameraMaxBounds);

            TryRestoreFromSaveIfRequested();
            cellManager.RefreshAssistantVisuals(assistantManager);
            artTuner?.Apply();

            _initialized = true;
        }

        void EnsureConfig()
        {
            if (zone1Config != null)
                return;

            // Safe runtime fallback so scene remains playable even without authored asset.
            zone1Config = ScriptableObject.CreateInstance<Zone1Config>();
            zone1Config.name = "Zone1Config_Runtime";
        }

        void ApplyConfig()
        {
            if (zone1Config == null)
                return;

            resourceManager.ConfigureInitialValues(
                zone1Config.initialDarkCoins,
                zone1Config.initialWeakSouls,
                zone1Config.initialPureEnergy,
                zone1Config.initialMemoryShards,
                zone1Config.initialUnstableSouls,
                resetNow: true);
            resourceManager.ConfigureStorageCaps(
                zone1Config.weakSoulsCapacity,
                zone1Config.pureEnergyCapacity,
                zone1Config.memoryShardsCapacity,
                zone1Config.unstableSoulsCapacity,
                clampCurrentAmounts: true);

            progressionManager.Configure(
                zone1Config.initialLevel,
                zone1Config.initialXp,
                zone1Config.baseXpToLevel,
                zone1Config.xpGrowthPerLevel,
                resetNow: true);

            cellManager.ConfigureGrid(
                zone1Config.gridColumns,
                zone1Config.gridRows,
                zone1Config.gridSpacing,
                zone1Config.gridOrigin,
                zone1Config.initiallyUnlockedCells,
                zone1Config.initiallyPurchasableCells);
            cellManager.ConfigureEconomy(
                zone1Config.cellPurchaseSlotScale,
                zone1Config.cellUpgradeBaseCost,
                zone1Config.cellUpgradePerLevelAdd,
                zone1Config.cellUpgradeLevelMultiplier);

            buyerManager.ConfigureEconomy(
                zone1Config.buyerPriceMinMultiplier,
                zone1Config.buyerPriceMaxMultiplier,
                zone1Config.buyerLevelDemandBonusPerLevel,
                zone1Config.buyerStockPenaltyAtFull,
                zone1Config.buyerSoldPressurePerUnit,
                zone1Config.buyerPressureRecoveryPerSecond);

            assistantManager.Configure(
                zone1Config.initialAssistants,
                zone1Config.maxAssistants,
                zone1Config.assistantAutomationTickSeconds);

            taxManager.Configure(
                zone1Config.collectorName,
                zone1Config.baseTaxPercent,
                zone1Config.taxIntervalSeconds,
                zone1Config.payWindowSeconds,
                zone1Config.moneyLossOnFail,
                zone1Config.finePerStrikeStep,
                zone1Config.maxStrikesBeforeGameOver);
        }

        void TryRestoreFromSaveIfRequested()
        {
            if (SaveManager.Instance == null)
                return;
            if (!SaveManager.Instance.ShouldRestoreFromSave)
                return;

            var data = SaveManager.Instance.CachedData;
            if (data == null || data.zone1 == null || !data.zone1.valid)
            {
                SaveManager.Instance.MarkRestoreConsumed();
                return;
            }

            ApplySaveData(data.zone1);
            SaveManager.Instance.MarkRestoreConsumed();
        }

        public Zone1SaveData CaptureSaveData()
        {
            var data = new Zone1SaveData
            {
                valid = true,
                darkCoins = resourceManager.Get(ResourceType.DarkCoins),
                weakSouls = resourceManager.Get(ResourceType.WeakSouls),
                pureEnergy = resourceManager.Get(ResourceType.PureEnergy),
                memoryShards = resourceManager.Get(ResourceType.MemoryShards),
                unstableSouls = resourceManager.Get(ResourceType.UnstableSouls),
                level = progressionManager.Level,
                xp = progressionManager.Xp,
                strikes = taxManager.Strikes,
                fineDebt = taxManager.FineDebt,
                timeToNextTaxSeconds = taxManager.TimeToNextTaxSeconds,
                taxAlertActive = taxManager.IsAlertActive,
                payWindowRemainingSeconds = taxManager.PayWindowRemainingSeconds,
                cells = cellManager.CaptureSaveData(),
                assistantTotal = assistantManager.TotalAssistants,
                assistants = assistantManager.CaptureSaveData(),
            };
            return data;
        }

        public void ApplySaveData(Zone1SaveData data)
        {
            if (data == null || !data.valid)
                return;

            resourceManager.Set(ResourceType.DarkCoins, data.darkCoins);
            resourceManager.Set(ResourceType.WeakSouls, data.weakSouls);
            resourceManager.Set(ResourceType.PureEnergy, data.pureEnergy);
            resourceManager.Set(ResourceType.MemoryShards, data.memoryShards);
            resourceManager.Set(ResourceType.UnstableSouls, data.unstableSouls);

            progressionManager.SetProgress(data.level, data.xp);
            cellManager.ApplySaveData(data.cells);
            assistantManager.ApplySaveData(data.assistantTotal, data.assistants);
            taxManager.ApplySaveState(data.strikes, data.fineDebt, data.timeToNextTaxSeconds, data.taxAlertActive, data.payWindowRemainingSeconds);

            cellManager.RefreshAssistantVisuals(assistantManager);
            uiManager.RefreshFromExternalState();
        }

        void OnAssistantAssignmentsChanged()
        {
            cellManager?.RefreshAssistantVisuals(assistantManager);
        }

        void OnDestroy()
        {
            if (assistantManager != null)
                assistantManager.Changed -= OnAssistantAssignmentsChanged;
        }
    }
}

