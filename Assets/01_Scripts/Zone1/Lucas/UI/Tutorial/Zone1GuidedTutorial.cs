using System;
using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1.Cells;
using LasGranjasDelHastur.Zone1.Gacha;
using LasGranjasDelHastur.Zone1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone1
{
    /// <summary>
    /// Tutorial guiado por paneles en la primera visita a Zona 1. Se guarda en <see cref="Zone1SaveData"/> y en PlayerPrefs.
    /// </summary>
    public sealed class Zone1GuidedTutorial : MonoBehaviour
    {
        public const string PlayerPrefsKey = "LasGranjas_Z1_GuidedTutorialDone_v1";

        static bool _persistedDone;

        CellManager _cells;
        UIManager _ui;
        AssistantManager _assistants;

        Canvas _rootCanvas;
        RectTransform _dimRt;
        Image _dimImg;
        RectTransform _panelRt;
        TextMeshProUGUI _titleTmp;
        TextMeshProUGUI _bodyTmp;
        Button _primaryBtn;
        TextMeshProUGUI _primaryLabel;
        Button _skipBtn;
        RectTransform _skipRt;
        GameObject _bannerRoot;
        RectTransform _bannerRt;
        TextMeshProUGUI _bannerTitle;
        TextMeshProUGUI _bannerBody;
        LayoutElement _bannerBodyLe;
        LayoutElement _bannerTitleLe;
        Image _bannerBgImg;

        Phase _phase = Phase.Inactive;
        bool _sawReadyOnSelectedCell;
        int _gachaPullBaseline;
        int _salesBaseline;
        int _assistantBuyBaseline;
        int _assignBaseline;

        const int TutorialCanvasSortDefault = 420;
        /// <summary>Por encima del overlay del gacha (240) para que el banner superior se lea; el centro sin UI sigue dejando clics al gacha.</summary>
        const int TutorialCanvasSortAboveGachaOverlay = 470;

        enum Phase
        {
            Inactive = 0,
            Welcome = 1,
            WaitCellClick = 2,
            WaitProduce = 3,
            WaitCollectCycle = 4,
            WaitVentasButton = 5,
            WaitSalesFirstSell = 6,
            WaitSalesBuyAssistant = 7,
            WaitCloseSalesPanel = 8,
            WaitAssignAssistant = 9,
            WaitGachaOpen = 10,
            WaitGachaPull = 11,
            WaitGachaClose = 12,
            WaitEasterEgg = 13,
            FinishModal = 14,
        }

        enum BannerRegion
        {
            Bottom,
            /// <summary>Columna izquierda media-baja: deja libre el botón Tienda y el panel a la derecha.</summary>
            SalesSideStrip,
            /// <summary>Banda inferior izquierda: no tapa la X ni botones típicos del gacha centrado.</summary>
            GachaSideStrip,
            /// <summary>Banda derecha media: cuando el panel de celda está a la izquierda.</summary>
            AssignSideStrip,
        }

        public static bool IsDoneForPersistence() =>
            _persistedDone || PlayerPrefs.GetInt(PlayerPrefsKey, 0) == 1;

        /// <summary>
        /// Alinea el estado del tutorial con el guardado (y PlayerPrefs). Debe reflejar el disco; antes solo marcaba "hecho"
        /// y nunca limpiaba, lo que ocultaba el tutorial si había prefs/sesiones inconsistentes.
        /// </summary>
        public static void HydrateFromSave(bool guidedTutorialCompleted)
        {
            _persistedDone = guidedTutorialCompleted;
            if (guidedTutorialCompleted)
                PlayerPrefs.SetInt(PlayerPrefsKey, 1);
            else
                PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }

        public static void ClearStaticForNewGame()
        {
            _persistedDone = false;
        }

        /// <summary>Sincroniza flags tras cargar CachedData sin pasar necesariamente por <see cref="Zone1Manager.ApplySaveData"/>.</summary>
        public static void SyncHydrationFromCachedSave(SaveManager saveManager)
        {
            var z1 = saveManager?.CachedData?.zone1;
            HydrateFromSave(z1 != null && z1.guidedTutorialCompleted);
        }

        public static bool ShouldSuppress() =>
            _persistedDone || PlayerPrefs.GetInt(PlayerPrefsKey, 0) == 1;

        public static void TryBegin(MonoBehaviour host, CellManager cells, UIManager ui)
        {
            if (host == null || cells == null || ui == null)
                return;
            if (ShouldSuppress())
                return;
            if (!host.TryGetComponent<Zone1GuidedTutorial>(out var t))
                t = host.gameObject.AddComponent<Zone1GuidedTutorial>();
            t.EnsureStarted(cells, ui);
        }

        void EnsureStarted(CellManager cells, UIManager ui)
        {
            if (_phase != Phase.Inactive)
                return;
            _cells = cells;
            _ui = ui;
            _assistants = FindFirstObjectByType<AssistantManager>();
            BuildUi();
            Wire();
            SetPhase(Phase.Welcome);
        }

        void Wire()
        {
            _cells.SelectedCellChanged += OnSelectedCellChanged;
            _cells.CellsChanged += OnCellsChanged;
            Zone1CultistEasterEggController.EasterEggActivated += OnEasterEggActivated;
        }

        void OnDestroy()
        {
            if (_cells != null)
            {
                _cells.SelectedCellChanged -= OnSelectedCellChanged;
                _cells.CellsChanged -= OnCellsChanged;
            }

            Zone1CultistEasterEggController.EasterEggActivated -= OnEasterEggActivated;

            if (_rootCanvas != null)
                Destroy(_rootCanvas.gameObject);
        }

        void OnSelectedCellChanged(FarmCell cell)
        {
            if (_phase != Phase.WaitCellClick)
                return;
            if (cell != null && cell.State != CellState.Blocked)
                SetPhase(Phase.WaitProduce);
        }

        void OnCellsChanged()
        {
            var sel = _cells != null ? _cells.SelectedCell : null;
            if (_phase == Phase.WaitProduce && sel != null && sel.State == CellState.Producing)
            {
                _sawReadyOnSelectedCell = false;
                SetPhase(Phase.WaitCollectCycle);
                return;
            }

            if (_phase != Phase.WaitCollectCycle || sel == null)
                return;

            if (sel.State == CellState.ReadyToCollect)
                _sawReadyOnSelectedCell = true;

            if (_sawReadyOnSelectedCell && sel.State == CellState.Available)
                SetPhase(Phase.WaitVentasButton);
        }

        void OnEasterEggActivated()
        {
            if (_phase != Phase.WaitEasterEgg)
                return;
            SetPhase(Phase.FinishModal);
        }

        void Update()
        {
            switch (_phase)
            {
                case Phase.WaitVentasButton:
                    if (_ui != null && _ui.IsSalesPanelOpen)
                    {
                        _ui.EnsureShopTab(UIManager.ShopTabVentas);
                        SetPhase(Phase.WaitSalesFirstSell);
                    }
                    break;

                case Phase.WaitSalesFirstSell:
                    if (_ui != null && _ui.IsSalesPanelOpen)
                        _ui.EnsureShopTab(UIManager.ShopTabVentas);
                    if (_ui != null && _ui.SessionSalesCompletedCount > _salesBaseline)
                        SetPhase(Phase.WaitSalesBuyAssistant);
                    break;

                case Phase.WaitSalesBuyAssistant:
                    if (_ui != null && _ui.IsSalesPanelOpen)
                        _ui.EnsureShopTab(UIManager.ShopTabAsistentes);
                    if (_assistants != null &&
                        _assistants.TotalAssistants > _assistantBuyBaseline)
                        SetPhase(Phase.WaitCloseSalesPanel);
                    break;

                case Phase.WaitCloseSalesPanel:
                    if (_ui != null && !_ui.IsSalesPanelOpen)
                        SetPhase(Phase.WaitAssignAssistant);
                    break;

                case Phase.WaitAssignAssistant:
                    if (_assistants != null && _assistants.AssignedAssistants > _assignBaseline)
                        SetPhase(Phase.WaitGachaOpen);
                    break;

                case Phase.WaitGachaOpen:
                {
                    var g0 = Zone1GachaController.Instance;
                    if (g0 != null && g0.IsPanelOpen)
                        SetPhase(Phase.WaitGachaPull);
                    break;
                }

                case Phase.WaitGachaPull:
                {
                    var g1 = Zone1GachaController.Instance;
                    if (g1 != null && g1.CompletedPullsThisSession > _gachaPullBaseline)
                        SetPhase(Phase.WaitGachaClose);
                    break;
                }

                case Phase.WaitGachaClose:
                {
                    var g2 = Zone1GachaController.Instance;
                    if (g2 == null || !g2.IsPanelOpen)
                        SetPhase(Phase.WaitEasterEgg);
                    break;
                }
            }
        }

        void SetTutorialCanvasSortForGachaSteps(bool gachaPanelOpenWithInteraction)
        {
            if (_rootCanvas != null)
            {
                _rootCanvas.sortingOrder = gachaPanelOpenWithInteraction
                    ? TutorialCanvasSortAboveGachaOverlay
                    : TutorialCanvasSortDefault;
            }
        }

        void SetBannerRegion(BannerRegion region)
        {
            if (_bannerRt == null)
                return;

            float titleFont = 23f;
            float bodyFont = 18f;
            float bodyPreferred = 96f;
            float titlePreferred = 34f;

            switch (region)
            {
                case BannerRegion.Bottom:
                    _bannerRt.anchorMin = new Vector2(0.05f, 0.04f);
                    _bannerRt.anchorMax = new Vector2(0.95f, 0.28f);
                    titleFont = 23f;
                    bodyFont = 18f;
                    bodyPreferred = 96f;
                    titlePreferred = 34f;
                    break;

                case BannerRegion.SalesSideStrip:
                    _bannerRt.anchorMin = new Vector2(0.03f, 0.26f);
                    _bannerRt.anchorMax = new Vector2(0.46f, 0.54f);
                    titleFont = 25f;
                    bodyFont = 19f;
                    bodyPreferred = 168f;
                    titlePreferred = 36f;
                    break;

                case BannerRegion.GachaSideStrip:
                    _bannerRt.anchorMin = new Vector2(0.03f, 0.12f);
                    _bannerRt.anchorMax = new Vector2(0.44f, 0.38f);
                    titleFont = 25f;
                    bodyFont = 19f;
                    bodyPreferred = 120f;
                    titlePreferred = 36f;
                    break;

                case BannerRegion.AssignSideStrip:
                    _bannerRt.anchorMin = new Vector2(0.54f, 0.26f);
                    _bannerRt.anchorMax = new Vector2(0.98f, 0.54f);
                    titleFont = 25f;
                    bodyFont = 19f;
                    bodyPreferred = 152f;
                    titlePreferred = 36f;
                    break;
            }

            _bannerRt.offsetMin = Vector2.zero;
            _bannerRt.offsetMax = Vector2.zero;

            if (_bannerTitle != null)
                _bannerTitle.fontSize = titleFont;
            if (_bannerBody != null)
                _bannerBody.fontSize = bodyFont;
            if (_bannerBodyLe != null)
                _bannerBodyLe.preferredHeight = bodyPreferred;
            if (_bannerTitleLe != null)
                _bannerTitleLe.preferredHeight = titlePreferred;

            ConfigureBannerRaycasts();
        }

        /// <summary>
        /// El fondo del banner no debe interceptar clics: así funcionan x1 en Ventas, X del gacha, etc.
        /// Solo “Saltar tutorial” sigue bloqueando raycasts.
        /// </summary>
        void ConfigureBannerRaycasts()
        {
            if (_bannerBgImg != null)
                _bannerBgImg.raycastTarget = false;

            if (_bannerTitle != null)
                _bannerTitle.raycastTarget = false;
            if (_bannerBody != null)
                _bannerBody.raycastTarget = false;
        }

        void SetSkipButtonCorner(bool bottomRight)
        {
            if (_skipRt == null)
                return;
            if (bottomRight)
            {
                _skipRt.anchorMin = new Vector2(1f, 0f);
                _skipRt.anchorMax = new Vector2(1f, 0f);
                _skipRt.pivot = new Vector2(1f, 0f);
                _skipRt.anchoredPosition = new Vector2(-28f, 22f);
            }
            else
            {
                _skipRt.anchorMin = new Vector2(0f, 0f);
                _skipRt.anchorMax = new Vector2(0f, 0f);
                _skipRt.pivot = new Vector2(0f, 0f);
                _skipRt.anchoredPosition = new Vector2(28f, 22f);
            }
        }

        void ApplyBannerLayoutForPhase(Phase p)
        {
            switch (p)
            {
                case Phase.WaitVentasButton:
                case Phase.WaitSalesFirstSell:
                case Phase.WaitSalesBuyAssistant:
                case Phase.WaitCloseSalesPanel:
                    SetBannerRegion(BannerRegion.SalesSideStrip);
                    SetSkipButtonCorner(true);
                    break;

                case Phase.WaitAssignAssistant:
                    SetBannerRegion(BannerRegion.AssignSideStrip);
                    SetSkipButtonCorner(true);
                    break;

                case Phase.WaitGachaOpen:
                case Phase.WaitGachaPull:
                case Phase.WaitGachaClose:
                    SetBannerRegion(BannerRegion.GachaSideStrip);
                    SetSkipButtonCorner(true);
                    break;

                case Phase.WaitEasterEgg:
                    SetBannerRegion(BannerRegion.Bottom);
                    SetSkipButtonCorner(false);
                    break;

                default:
                    SetBannerRegion(BannerRegion.Bottom);
                    SetSkipButtonCorner(false);
                    break;
            }
        }

        static bool CanBuyNextAssistant(AssistantManager assistants)
        {
            if (assistants == null || assistants.TotalAssistants >= 30)
                return false;
            var rm = FindFirstObjectByType<ResourceManager>();
            return rm != null && rm.Get(ResourceType.DarkCoins) >= assistants.NextAssistantCost;
        }

        void SetPhase(Phase p)
        {
            _phase = p;
            switch (p)
            {
                case Phase.Welcome:
                    SetSkipButtonCorner(false);
                    SetModalLayout(centered: true);
                    SetDim(true, blockRaycasts: true);
                    SetBanner(false);
                    SetModalTexts(
                        UIManager.SafeGlyphs("Bienvenido a los Calabozos"),
                        UIManager.SafeGlyphs(
                            "Gestionas celdas en la granja: producen recursos que vendes a compradores.\n\n" +
                            "Usa monedas oscuras para producir y vigila el impuesto.\n\n" +
                            "Pulsa Continuar para un recorrido breve."));
                    SetPrimary(UIManager.SafeGlyphs("Continuar"), () => SetPhase(Phase.WaitCellClick));
                    SetSkipVisible(true);
                    break;

                case Phase.WaitCellClick:
                    ApplyBannerLayoutForPhase(p);
                    SetModalLayout(centered: false);
                    SetDim(false, blockRaycasts: false);
                    SetBanner(true);
                    SetBannerTexts(
                        UIManager.SafeGlyphs("Selecciona una celda"),
                        UIManager.SafeGlyphs(
                            "Haz clic en el mapa sobre una celda desbloqueada (no gris). Se abrirá el panel de la celda."));
                    SetPrimary(null, null);
                    SetSkipVisible(true);
                    break;

                case Phase.WaitProduce:
                    ApplyBannerLayoutForPhase(p);
                    SetBanner(true);
                    SetBannerTexts(
                        UIManager.SafeGlyphs("Producir"),
                        UIManager.SafeGlyphs(
                            "En el panel, pulsa Producir para gastar monedas oscuras e iniciar la producción."));
                    SetPrimary(null, null);
                    SetSkipVisible(true);
                    break;

                case Phase.WaitCollectCycle:
                    ApplyBannerLayoutForPhase(p);
                    SetBanner(true);
                    SetBannerTexts(
                        UIManager.SafeGlyphs("Recolectar"),
                        UIManager.SafeGlyphs(
                            "Cuando termine el tiempo, pulsa Recolectar en el panel para obtener recursos."));
                    SetPrimary(null, null);
                    SetSkipVisible(true);
                    break;

                case Phase.WaitVentasButton:
                    ApplyBannerLayoutForPhase(p);
                    SetTutorialCanvasSortForGachaSteps(false);
                    SetBanner(true);
                    SetBannerTexts(
                        UIManager.SafeGlyphs("Abrir la Tienda"),
                        UIManager.SafeGlyphs(
                            "Arriba a la derecha verás el botón **Tienda**. Púlsalo para abrir el panel principal.\n\n" +
                            "En la pestaña **Ventas** venderás recursos a cambio de monedas oscuras; en **Asistentes** podrás contratar sabuesos. Cierra con **Cerrar** al pie del panel."));
                    SetPrimary(null, null);
                    SetSkipVisible(true);
                    break;

                case Phase.WaitSalesFirstSell:
                    ApplyBannerLayoutForPhase(p);
                    SetTutorialCanvasSortForGachaSteps(false);
                    if (_ui != null)
                        _salesBaseline = _ui.SessionSalesCompletedCount;
                    SetBanner(true);
                    SetBannerTexts(
                        UIManager.SafeGlyphs("Vender a un comprador"),
                        UIManager.SafeGlyphs(
                            "En la pestaña **Ventas**, cada fila es un comprador con un recurso y precio por unidad (/u). A la derecha usa **x1** o **MAX**.\n\n" +
                            "La leyenda arriba de la lista resume x1, MAX y Oferta. Haz al menos una venta."));
                    SetPrimary(null, null);
                    SetSkipVisible(true);
                    break;

                case Phase.WaitSalesBuyAssistant:
                    ApplyBannerLayoutForPhase(p);
                    SetTutorialCanvasSortForGachaSteps(false);
                    if (_assistants != null)
                        _assistantBuyBaseline = _assistants.TotalAssistants;
                    if (!CanBuyNextAssistant(_assistants))
                    {
                        SetPhase(Phase.WaitCloseSalesPanel);
                        break;
                    }

                    SetBanner(true);
                    SetBannerTexts(
                        UIManager.SafeGlyphs("Comprar asistente"),
                        UIManager.SafeGlyphs(
                            "Abre la pestaña **Asistentes** en la Tienda (arriba del contenido). Pulsa **Comprar asistente** si tienes monedas suficientes; el precio aparece en el texto."));
                    SetPrimary(null, null);
                    SetSkipVisible(true);
                    break;

                case Phase.WaitCloseSalesPanel:
                    ApplyBannerLayoutForPhase(p);
                    SetTutorialCanvasSortForGachaSteps(false);
                    SetBanner(true);
                    SetBannerTexts(
                        UIManager.SafeGlyphs("Cerrar Tienda"),
                        UIManager.SafeGlyphs(
                            "Cuando termines, pulsa **Cerrar** al final del panel de la Tienda para volver al mapa.\n\n" +
                            "Si no pudiste comprar otro asistente por falta de monedas, no pasa nada: cierra igual y sigue."));
                    SetPrimary(null, null);
                    SetSkipVisible(true);
                    break;

                case Phase.WaitAssignAssistant:
                    ApplyBannerLayoutForPhase(p);
                    SetTutorialCanvasSortForGachaSteps(false);
                    if (_assistants != null)
                        _assignBaseline = _assistants.AssignedAssistants;
                    if (_assistants != null && _assistants.AvailableAssistants <= 0)
                    {
                        SetPhase(Phase.WaitGachaOpen);
                        break;
                    }

                    SetBanner(true);
                    SetBannerTexts(
                        UIManager.SafeGlyphs("Asignar asistente"),
                        UIManager.SafeGlyphs(
                            "Haz clic en una celda del mapa para abrir su panel (izquierda). Si tienes asistentes libres, pulsa **Asignar asistente** en ese panel para dejarlo trabajando en esa celda."));
                    SetPrimary(null, null);
                    SetSkipVisible(true);
                    break;

                case Phase.WaitGachaOpen:
                    ApplyBannerLayoutForPhase(p);
                    SetTutorialCanvasSortForGachaSteps(false);
                    _gachaPullBaseline = Zone1GachaController.Instance != null
                        ? Zone1GachaController.Instance.CompletedPullsThisSession
                        : 0;
                    SetBanner(true);
                    SetBannerTexts(
                        UIManager.SafeGlyphs("Gacha"),
                        UIManager.SafeGlyphs(
                            "En el mapa hay una fuente decorativa del gacha. Haz clic sobre ella para abrir la máquina de tiradas (gasta monedas oscuras)."));
                    SetPrimary(null, null);
                    SetSkipVisible(true);
                    break;

                case Phase.WaitGachaPull:
                    ApplyBannerLayoutForPhase(p);
                    SetTutorialCanvasSortForGachaSteps(true);
                    SetBanner(true);
                    SetBannerTexts(
                        UIManager.SafeGlyphs("Tu primera tirada"),
                        UIManager.SafeGlyphs(
                            "Pulsa Tirar x1 y espera a que termine la animación. Si no te alcanza el dinero, vende más en la Tienda (pestaña Ventas) y vuelve."));
                    SetPrimary(null, null);
                    SetSkipVisible(true);
                    break;

                case Phase.WaitGachaClose:
                    ApplyBannerLayoutForPhase(p);
                    SetTutorialCanvasSortForGachaSteps(true);
                    SetBanner(true);
                    SetBannerTexts(
                        UIManager.SafeGlyphs("Cerrar el gacha"),
                        UIManager.SafeGlyphs(
                            "Pulsa el botón de cerrar (X) del panel del gacha para volver al calabozo."));
                    SetPrimary(null, null);
                    SetSkipVisible(true);
                    break;

                case Phase.WaitEasterEgg:
                    ApplyBannerLayoutForPhase(p);
                    SetTutorialCanvasSortForGachaSteps(false);
                    SetBanner(true);
                    SetBannerTexts(
                        UIManager.SafeGlyphs("Secreto de los cultistas"),
                        UIManager.SafeGlyphs(
                            "Busca figuras amarillas (cultistas) alrededor del nivel. Haz clic varias veces sobre ellos hasta activar la pista musical secreta. La primera vez en la partida puedes recibir un bono de monedas."));
                    SetPrimary(null, null);
                    SetSkipVisible(true);
                    break;

                case Phase.FinishModal:
                    SetSkipButtonCorner(false);
                    SetTutorialCanvasSortForGachaSteps(false);
                    SetModalLayout(centered: true);
                    SetDim(true, blockRaycasts: true);
                    SetBanner(false);
                    SetModalTexts(
                        UIManager.SafeGlyphs("Tutorial completado"),
                        UIManager.SafeGlyphs(
                            "Resumen: celdas, producir, recolectar, ventas (vender, comprar asistente, cerrar), asignar asistente, gacha en la fuente y el secreto de los cultistas.\n\n¡Buena suerte!"));
                    SetPrimary(UIManager.SafeGlyphs("Cerrar"), CompleteTutorial);
                    SetSkipVisible(false);
                    break;

                case Phase.Inactive:
                    break;
            }
        }

        void CompleteTutorial()
        {
            _persistedDone = true;
            PlayerPrefs.SetInt(PlayerPrefsKey, 1);
            PlayerPrefs.Save();

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.CachedData.zone1 ??= new Zone1SaveData();
                SaveManager.Instance.CachedData.zone1.guidedTutorialCompleted = true;
                SaveManager.Instance.SaveNow();
            }

            if (_rootCanvas != null)
                Destroy(_rootCanvas.gameObject);
            _rootCanvas = null;
            _phase = Phase.Inactive;
            enabled = false;
        }

        void SkipTutorial() => CompleteTutorial();

        void BuildUi()
        {
            var go = new GameObject("Zone1GuidedTutorialRoot");
            go.transform.SetParent(transform, false);

            _rootCanvas = go.AddComponent<Canvas>();
            _rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _rootCanvas.sortingOrder = 420;
            _rootCanvas.overrideSorting = true;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.55f;
            go.AddComponent<GraphicRaycaster>();

            var dim = new GameObject("Dim");
            dim.transform.SetParent(go.transform, false);
            _dimRt = dim.AddComponent<RectTransform>();
            _dimRt.anchorMin = Vector2.zero;
            _dimRt.anchorMax = Vector2.one;
            _dimRt.offsetMin = Vector2.zero;
            _dimRt.offsetMax = Vector2.zero;
            _dimImg = dim.AddComponent<Image>();
            _dimImg.color = new Color(0.02f, 0.02f, 0.05f, 0.78f);
            _dimImg.raycastTarget = true;

            var panel = new GameObject("CenterPanel");
            panel.transform.SetParent(go.transform, false);
            _panelRt = panel.AddComponent<RectTransform>();
            _panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            _panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRt.pivot = new Vector2(0.5f, 0.5f);
            _panelRt.sizeDelta = new Vector2(720f, 420f);
            _panelRt.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.96f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            _titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            _titleTmp.fontSize = 28;
            _titleTmp.alignment = TextAlignmentOptions.Center;
            _titleTmp.color = new Color(0.94f, 0.86f, 0.58f, 1f);
            _titleTmp.raycastTarget = false;
            var titleRt = _titleTmp.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 0.72f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(24f, 0f);
            titleRt.offsetMax = new Vector2(-24f, -16f);

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(panel.transform, false);
            _bodyTmp = bodyGo.AddComponent<TextMeshProUGUI>();
            _bodyTmp.fontSize = 19;
            _bodyTmp.alignment = TextAlignmentOptions.TopLeft;
            _bodyTmp.color = new Color(0.9f, 0.9f, 0.92f, 1f);
            _bodyTmp.textWrappingMode = TextWrappingModes.Normal;
            _bodyTmp.raycastTarget = false;
            var bodyRt = _bodyTmp.rectTransform;
            bodyRt.anchorMin = new Vector2(0f, 0.22f);
            bodyRt.anchorMax = new Vector2(1f, 0.72f);
            bodyRt.offsetMin = new Vector2(28f, 8f);
            bodyRt.offsetMax = new Vector2(-28f, -8f);

            var btnGo = new GameObject("PrimaryBtn");
            btnGo.transform.SetParent(panel.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.sizeDelta = new Vector2(280f, 52f);
            btnRt.anchoredPosition = new Vector2(0f, 22f);
            btnGo.AddComponent<Image>().color = new Color(0.22f, 0.24f, 0.32f, 1f);
            _primaryBtn = btnGo.AddComponent<Button>();
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(btnGo.transform, false);
            _primaryLabel = lblGo.AddComponent<TextMeshProUGUI>();
            _primaryLabel.fontSize = 21;
            _primaryLabel.alignment = TextAlignmentOptions.Center;
            _primaryLabel.color = new Color(0.95f, 0.9f, 0.78f, 1f);
            _primaryLabel.raycastTarget = false;
            var lblRt = _primaryLabel.rectTransform;
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;

            var skipGo = new GameObject("SkipBtn");
            skipGo.transform.SetParent(go.transform, false);
            var skipRt = skipGo.AddComponent<RectTransform>();
            _skipRt = skipRt;
            skipRt.anchorMin = new Vector2(0f, 0f);
            skipRt.anchorMax = new Vector2(0f, 0f);
            skipRt.pivot = new Vector2(0f, 0f);
            skipRt.sizeDelta = new Vector2(220f, 44f);
            skipRt.anchoredPosition = new Vector2(28f, 22f);
            var skipBg = skipGo.AddComponent<Image>();
            skipBg.color = new Color(0.14f, 0.14f, 0.18f, 0.92f);
            skipBg.raycastTarget = true;
            _skipBtn = skipGo.AddComponent<Button>();
            var skipLblGo = new GameObject("SkipLabel");
            skipLblGo.transform.SetParent(skipGo.transform, false);
            var skipLbl = skipLblGo.AddComponent<TextMeshProUGUI>();
            skipLbl.text = UIManager.SafeGlyphs("Saltar tutorial");
            skipLbl.fontSize = 17;
            skipLbl.alignment = TextAlignmentOptions.Center;
            skipLbl.color = new Color(0.75f, 0.76f, 0.82f, 1f);
            skipLbl.raycastTarget = false;
            var skipLblRt = skipLbl.rectTransform;
            skipLblRt.anchorMin = Vector2.zero;
            skipLblRt.anchorMax = Vector2.one;
            skipLblRt.offsetMin = Vector2.zero;
            skipLblRt.offsetMax = Vector2.zero;
            _skipBtn.onClick.AddListener(SkipTutorial);

            _bannerRoot = new GameObject("BottomBanner");
            _bannerRoot.transform.SetParent(go.transform, false);
            var banRt = _bannerRoot.AddComponent<RectTransform>();
            banRt.anchorMin = new Vector2(0.05f, 0.04f);
            banRt.anchorMax = new Vector2(0.95f, 0.24f);
            banRt.offsetMin = Vector2.zero;
            banRt.offsetMax = Vector2.zero;
            _bannerRt = banRt;
            _bannerBgImg = _bannerRoot.AddComponent<Image>();
            _bannerBgImg.color = new Color(0.06f, 0.07f, 0.1f, 0.94f);
            _bannerBgImg.raycastTarget = false;
            var bannerCg = _bannerRoot.AddComponent<CanvasGroup>();
            bannerCg.blocksRaycasts = false;
            var v = _bannerRoot.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(18, 18, 12, 12);
            v.spacing = 6f;
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandWidth = true;

            var banTitleGo = new GameObject("BanTitle");
            banTitleGo.transform.SetParent(_bannerRoot.transform, false);
            _bannerTitle = banTitleGo.AddComponent<TextMeshProUGUI>();
            _bannerTitle.fontSize = 22;
            _bannerTitle.fontStyle = FontStyles.Bold;
            _bannerTitle.color = new Color(0.94f, 0.86f, 0.58f, 1f);
            _bannerTitle.raycastTarget = false;
            _bannerTitle.textWrappingMode = TextWrappingModes.Normal;
            _bannerTitleLe = banTitleGo.AddComponent<LayoutElement>();
            _bannerTitleLe.preferredHeight = 34f;

            var banBodyGo = new GameObject("BanBody");
            banBodyGo.transform.SetParent(_bannerRoot.transform, false);
            _bannerBody = banBodyGo.AddComponent<TextMeshProUGUI>();
            _bannerBody.fontSize = 17;
            _bannerBody.color = new Color(0.88f, 0.9f, 0.93f, 1f);
            _bannerBody.raycastTarget = false;
            _bannerBody.textWrappingMode = TextWrappingModes.Normal;
            _bannerBodyLe = banBodyGo.AddComponent<LayoutElement>();
            _bannerBodyLe.preferredHeight = 88f;

            _bannerRoot.SetActive(false);

            // Draw above banner/dim so "Saltar" stays clickable during bottom-banner steps.
            skipGo.transform.SetAsLastSibling();
        }

        void SetModalLayout(bool centered)
        {
            if (_panelRt == null)
                return;
            if (centered)
            {
                _panelRt.anchorMin = new Vector2(0.5f, 0.5f);
                _panelRt.anchorMax = new Vector2(0.5f, 0.5f);
                _panelRt.pivot = new Vector2(0.5f, 0.5f);
                _panelRt.sizeDelta = new Vector2(720f, 420f);
                _panelRt.anchoredPosition = Vector2.zero;
            }
            else
            {
                _panelRt.anchorMin = new Vector2(0.5f, 0.72f);
                _panelRt.anchorMax = new Vector2(0.5f, 0.72f);
                _panelRt.pivot = new Vector2(0.5f, 0f);
                _panelRt.sizeDelta = new Vector2(640f, 200f);
                _panelRt.anchoredPosition = Vector2.zero;
            }
        }

        void SetDim(bool on, bool blockRaycasts)
        {
            if (_dimRt != null)
                _dimRt.gameObject.SetActive(on);
            if (_dimImg != null)
                _dimImg.raycastTarget = blockRaycasts;
        }

        void SetBanner(bool on)
        {
            if (_bannerRoot != null)
                _bannerRoot.SetActive(on);
            if (_panelRt != null)
                _panelRt.gameObject.SetActive(!on);
        }

        void SetModalTexts(string title, string body)
        {
            if (_titleTmp != null)
                _titleTmp.text = title;
            if (_bodyTmp != null)
                _bodyTmp.text = body;
        }

        void SetBannerTexts(string title, string body)
        {
            if (_bannerTitle != null)
                _bannerTitle.text = title;
            if (_bannerBody != null)
                _bannerBody.text = body;
        }

        void SetPrimary(string label, Action onClick)
        {
            if (_primaryBtn == null)
                return;
            _primaryBtn.onClick.RemoveAllListeners();
            if (string.IsNullOrEmpty(label))
            {
                _primaryBtn.gameObject.SetActive(false);
                return;
            }

            _primaryBtn.gameObject.SetActive(true);
            _primaryLabel.text = label;
            if (onClick != null)
                _primaryBtn.onClick.AddListener(() => onClick());
        }

        void SetSkipVisible(bool on)
        {
            if (_skipBtn != null)
                _skipBtn.gameObject.SetActive(on);
        }
    }
}
