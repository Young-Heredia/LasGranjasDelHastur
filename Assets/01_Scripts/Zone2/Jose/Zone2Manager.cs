using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone1.Gacha;
using LasGranjasDelHastur.Zone1.UI;
using LasGranjasDelHastur.Zone2.Lucas;
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

        void Awake()
        {
            AudioManager.EnsureInstance();
            TryInitialize();
        }

        void Start()
        {
            TryInitialize();
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
    }
}

