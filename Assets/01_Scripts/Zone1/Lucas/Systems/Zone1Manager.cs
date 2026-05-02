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
        [Tooltip("Si está activo, **solo la primera entrada a Zona 1 en esta sesión de Play** invalida el snapshot de Z1 en disco (QA de economía). Cada recarga de escena ya no lo repite — antes al volver desde selección de zonas se borraba el guardado otra vez.")]
        [SerializeField] private bool debugIgnoreZone1DiskSaveOnce;

        /// <summary>
        /// Evita ejecutar el modo debug en cada carga de la escena (cada vez se crea un Zone1Manager nuevo con el mismo valor serializado).
        /// </summary>
        static bool s_zone1DebugIgnoreDiskAppliedThisPlaySession;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetZone1DebugIgnoreDiskGate()
        {
            s_zone1DebugIgnoreDiskAppliedThisPlaySession = false;
        }

        public ResourceManager Resources => resourceManager;
        public ProgressionManager Progression => progressionManager;
        public CellManager Cells => cellManager;
        public AssistantManager Assistants => assistantManager;
        public BuyerManager Buyers => buyerManager;
        public TaxManager Taxes => taxManager;
        public Zone1Config Config => zone1Config;
        bool _initialized;
        float _debugTimer;
        bool _autoRestoredFromDisk;

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
            TryRestoreFromSaveIfRequested();
            uiManager.Initialize(resourceManager, progressionManager, cellManager, assistantManager, buyerManager, taxManager);

            // Camera bounds + zoom tuned by config.
            if (cameraController != null)
            {
                cameraController.SetBounds(zone1Config.cameraMinBounds, zone1Config.cameraMaxBounds);
                cameraController.ApplyInitialOrthographicSize(zone1Config.cameraOrthographicSize);
            }

            cellManager.RefreshAssistantVisuals(assistantManager);
            artTuner?.Apply();

            // Tras recargar la escena, el bootstrap puede haber corrido antes de que el mundo estuviera listo; repetimos decor/cultistas en la escena activa.
            Zone1Bootstrap.EnsureZone1RuntimeDecor();

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
                zone1Config.maxStrikesBeforeGameOver,
                zone1Config.latePaymentPensionPerStrike);
        }

        void TryRestoreFromSaveIfRequested()
        {
            if (SaveManager.Instance == null)
                return;

            if (debugIgnoreZone1DiskSaveOnce && !s_zone1DebugIgnoreDiskAppliedThisPlaySession)
            {
                s_zone1DebugIgnoreDiskAppliedThisPlaySession = true;
                var d = SaveManager.Instance.CachedData;
                if (d == null)
                    return;
                d.zone1 = new Zone1SaveData { valid = false };
                d.zone1Available = false;
                GlobalTaxLedger.ClearStrikes();
                taxManager?.ResetLocalTaxUiState();
                SaveManager.Instance.WriteCachedDataNow();
                _autoRestoredFromDisk = true;
                if (SaveManager.Instance.ShouldRestoreFromSave)
                    SaveManager.Instance.MarkRestoreConsumed();
                return;
            }

            var data = SaveManager.Instance.CachedData;
            if (data == null || data.zone1 == null || !data.zone1.valid)
            {
                if (SaveManager.Instance.ShouldRestoreFromSave)
                    SaveManager.Instance.MarkRestoreConsumed();
                return;
            }

            // Normal flow: restore explicitly requested (Continue/new load).
            if (SaveManager.Instance.ShouldRestoreFromSave)
            {
                ApplySaveData(data.zone1);
                SaveManager.Instance.MarkRestoreConsumed();
                _autoRestoredFromDisk = true;
                return;
            }

            // Editor/runtime convenience: reloading Zone1 scene should keep last saved state.
            if (_autoRestoredFromDisk)
                return;
            ApplySaveData(data.zone1);
            _autoRestoredFromDisk = true;
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
                zone1EasterEggBonusClaimed = SaveManager.Instance != null &&
                                             SaveManager.Instance.CachedData != null &&
                                             SaveManager.Instance.CachedData.zone1 != null &&
                                             SaveManager.Instance.CachedData.zone1.zone1EasterEggBonusClaimed,
                level = progressionManager.Level,
                xp = progressionManager.Xp,
                strikes = GlobalTaxLedger.GetStrikes(),
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

            if (SaveManager.Instance != null && SaveManager.Instance.CachedData != null && SaveManager.Instance.CachedData.zone1 != null)
                SaveManager.Instance.CachedData.zone1.zone1EasterEggBonusClaimed = data.zone1EasterEggBonusClaimed;

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

