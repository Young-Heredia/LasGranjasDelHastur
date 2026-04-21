using LasGranjasDelHastur.Camera;
using LasGranjasDelHastur.Zone1.UI;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    public class Zone1Manager : MonoBehaviour
    {
        [Header("Scene Wiring (runtime bootstrap fills these)")]
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private ProgressionManager progressionManager;
        [SerializeField] private CellManager cellManager;
        [SerializeField] private BuyerManager buyerManager;
        [SerializeField] private TaxManager taxManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private Zone1ArtTuner artTuner;

        [Header("Camera")]
        [SerializeField] private CameraController2D cameraController;

        public ResourceManager Resources => resourceManager;
        public ProgressionManager Progression => progressionManager;
        public CellManager Cells => cellManager;
        public BuyerManager Buyers => buyerManager;
        public TaxManager Taxes => taxManager;
        bool _initialized;

        [ContextMenu("Auto Wire References")]
        public void AutoWireReferences()
        {
            if (resourceManager == null) resourceManager = FindFirstObjectByType<ResourceManager>();
            if (progressionManager == null) progressionManager = FindFirstObjectByType<ProgressionManager>();
            if (cellManager == null) cellManager = FindFirstObjectByType<CellManager>();
            if (buyerManager == null) buyerManager = FindFirstObjectByType<BuyerManager>();
            if (taxManager == null) taxManager = FindFirstObjectByType<TaxManager>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
            if (cameraController == null) cameraController = FindFirstObjectByType<CameraController2D>();
            if (artTuner == null) artTuner = FindFirstObjectByType<Zone1ArtTuner>();
        }

        void Awake()
        {
            TryInitialize();
        }

        void Start()
        {
            TryInitialize();
        }

        void Update()
        {
            if (_initialized)
                return;
            TryInitialize();
        }

        void TryInitialize()
        {
            if (_initialized)
                return;

            AutoWireReferences();

            if (resourceManager == null || progressionManager == null || cellManager == null || buyerManager == null || taxManager == null || uiManager == null)
            {
                return;
            }

            cellManager.Initialize(resourceManager, progressionManager);
            buyerManager.Initialize(resourceManager, progressionManager);
            taxManager.Initialize(resourceManager, cellManager);
            uiManager.Initialize(resourceManager, progressionManager, cellManager, buyerManager, taxManager);

            // Camera bounds tuned for the runtime placeholder grid size.
            if (cameraController != null)
                cameraController.SetBounds(new Vector2(-10f, -8f), new Vector2(10f, 6f));

            artTuner?.Apply();

            _initialized = true;
        }
    }
}

