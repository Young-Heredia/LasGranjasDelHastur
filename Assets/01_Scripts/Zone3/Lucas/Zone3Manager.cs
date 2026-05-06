using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone1.Gacha;
using LasGranjasDelHastur.Zone1.UI;
using UnityEngine;
using LasGranjasDelHastur.Camera;

namespace LasGranjasDelHastur.Zone3
{
    /// <summary>
    /// Zona 3 usando el stack de managers de Zona 1 para reutilizar exactamente su UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Zone3Manager : MonoBehaviour
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

            Zone3RuntimeScaffold.EnsureSceneScaffold();

            AutoWireReferences();

            if (resourceManager == null || progressionManager == null || cellManager == null || assistantManager == null || buyerManager == null || taxManager == null || uiManager == null)
                return;

            var slots = GameObject.Find("WorldRoot")?.transform.Find("CellSlotsRoot");
            if (slots != null && cellManager.transform.parent != slots)
                cellManager.transform.SetParent(slots, worldPositionStays: false);

            var camController = FindFirstObjectByType<CameraController2D>();
            camController?.SetBounds(new Vector2(-30f, -18f), new Vector2(30f, 18f));

            cellManager.Initialize(resourceManager, progressionManager);
            assistantManager.Initialize(cellManager, resourceManager, progressionManager);
            buyerManager.Initialize(resourceManager, progressionManager);
            taxManager.Initialize(resourceManager, cellManager);
            uiManager.Initialize(resourceManager, progressionManager, cellManager, assistantManager, buyerManager, taxManager);

            if (gachaController == null)
                gachaController = gameObject.AddComponent<Zone1GachaController>();
            gachaController.Setup(resourceManager, uiManager, zone1GachaPanelPrefab);

            cellManager.RefreshAssistantVisuals(assistantManager);

            _initialized = true;
        }
    }
}

