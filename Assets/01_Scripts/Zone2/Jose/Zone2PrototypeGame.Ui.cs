using LasGranjasDelHastur.Core;
using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone2
{
    /// <summary>Presentación y UI en tiempo de ejecución para Zona 2 (separado del bucle de gameplay).</summary>
    public partial class Zone2PrototypeGame
    {
        const string PackRoot = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/";
        const string Ui_ButtonPrimary = PackRoot + "UI/Buttons/UI_Button_Primary.png";
        const string Ui_ButtonDanger = PackRoot + "UI/Buttons/UI_Button_Danger.png";
        const string Ui_PanelHud = PackRoot + "UI/Panels/UI_Panel_HUD.png";
        const string Ui_PanelTaxAlert = PackRoot + "UI/Panels/UI_Panel_TaxAlert.png";
        const string Ui_PanelCellInfo = PackRoot + "UI/Panels/UI_Panel_CellInfo.png";
        const string Ui_PanelSales = PackRoot + "UI/Panels/UI_Panel_Sales.png";

        TextMeshProUGUI _txtHeader;
        TextMeshProUGUI _txtResources;
        TextMeshProUGUI _txtTax;
        TextMeshProUGUI _txtDifficulty;
        TextMeshProUGUI _txtHint;
        TextMeshProUGUI _txtSelectedCell;
        TextMeshProUGUI _txtSales;
        TextMeshProUGUI _txtBuySection;
        Button _btnProduce;
        Button _btnCollect;
        Button _btnUpgrade;
        Button _btnBuyCell;
        Button _btnAssignAssistant;
        Button _btnSalesToggle;
        Button _btnCloseDetails;
        GameObject _detailsPanelGo;
        GameObject _salesPanelGo;
        RectTransform _cellGridRoot;
        readonly Dictionary<int, Button> _cellButtons = new();

        sealed class Z2AssistantRosterRow
        {
            public GameObject Root;
            public TextMeshProUGUI MainText;
            public Button Improve;
            public Button Reassign;
            public Button Unassign;
        }

        readonly List<Z2AssistantRosterRow> _z2RosterRows = new();
        TextMeshProUGUI _txtReassignModeBanner;
        bool _z2RosterWidgetBuilt;

        void BuildUi()
        {
            // Si la escena ya tiene UI (creada por Zone2EditorScaffold), la usamos como Zona 1 (Hierarchy/Inspector).
            if (TryBindSceneUi())
                return;

            var root = new GameObject("Zone2PrototypeUI");
            root.transform.SetParent(transform, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.65f;
            root.AddComponent<GraphicRaycaster>();

            var backdrop = CreateImage(root.transform, "Backdrop", new Color(0.04f, 0.08f, 0.10f, 0.05f));
            Stretch(backdrop.rectTransform);
            backdrop.raycastTarget = false;

            var panel = CreateImage(root.transform, "Panel", new Color(0f, 0f, 0f, 0f));
            var prt = panel.rectTransform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            prt.anchoredPosition = Vector2.zero;

            var hud = CreateImage(panel.transform, "HUDTop", new Color(1f, 1f, 1f, 1f));
            var hudRt = hud.rectTransform;
            hudRt.anchorMin = new Vector2(0f, 1f);
            hudRt.anchorMax = new Vector2(1f, 1f);
            hudRt.pivot = new Vector2(0.5f, 1f);
            hudRt.sizeDelta = new Vector2(0f, 148f);
            hudRt.anchoredPosition = Vector2.zero;

            var hudFallback = new Color(0.05f, 0.10f, 0.11f, 0.88f);
            ZonePrototypeUiChrome.MountTwoRowHud(
                hud.transform,
                hud,
                "Zona 2 - Ciudades Condensadas (Kthanid vigila los tributos)",
                Ui_PanelHud,
                hudFallback,
                148f,
                out _txtHeader,
                out _txtResources,
                out _txtTax,
                out _txtDifficulty,
                out _btnSalesToggle,
                out var btnBackZones);
            ZonePrototypeUiChrome.ApplyHybridPackOrZone1Button(_btnSalesToggle, false, Ui_ButtonPrimary, Ui_ButtonDanger);
            ZonePrototypeUiChrome.ApplyHybridPackOrZone1Button(btnBackZones, true, Ui_ButtonPrimary, Ui_ButtonDanger);

            var gridPanel = CreateImage(panel.transform, "CellGridPanel", new Color(1f, 1f, 1f, 1f));
            ApplyUiSprite(gridPanel, Ui_PanelTaxAlert, fallbackTint: new Color(0.05f, 0.09f, 0.11f, 0.68f));
            var gridRt = gridPanel.rectTransform;
            gridRt.anchorMin = new Vector2(0.5f, 0.5f);
            gridRt.anchorMax = new Vector2(0.5f, 0.5f);
            gridRt.pivot = new Vector2(0.5f, 0.5f);
            gridRt.sizeDelta = new Vector2(900f, 520f);
            gridRt.anchoredPosition = new Vector2(0f, -12f);
            // 6×5 = 30 celdas (misma lógica que el mundo y Zona 1).
            _cellGridRoot = BuildCellGrid(gridPanel.transform, 6, 5, new Vector2(120f, 70f), new Vector2(4f, 4f));

            var detailsPanel = CreateImage(panel.transform, "CellDetails", new Color(1f, 1f, 1f, 1f));
            ZonePrototypeUiChrome.ApplyHybridSidePanel(
                detailsPanel,
                Ui_PanelCellInfo,
                ZonePrototypeUiChrome.Zone1PanelCellPath,
                new Color(0.10f, 0.14f, 0.17f, 0.92f));
            var dprt = detailsPanel.rectTransform;
            dprt.anchorMin = new Vector2(0f, 0.5f);
            dprt.anchorMax = new Vector2(0f, 0.5f);
            dprt.pivot = new Vector2(0f, 0.5f);
            dprt.anchoredPosition = new Vector2(16f, -40f);
            dprt.sizeDelta = new Vector2(320f, 400f);
            _detailsPanelGo = detailsPanel.gameObject;

            var detailsBody = ZonePrototypeUiChrome.EnsurePanelBody(detailsPanel.transform);
            _txtSelectedCell = ZonePrototypeUiChrome.AddBodyLabel(detailsBody, "SelectedCell", "", 16, 112f, TextAlignmentOptions.TopLeft);
            _txtSelectedCell.textWrappingMode = TextWrappingModes.Normal;
            _btnProduce = ZonePrototypeUiChrome.AddBodyButton(detailsBody, "Iniciar Producción", 40f);
            _btnCollect = ZonePrototypeUiChrome.AddBodyButton(detailsBody, "Recolectar", 40f);
            _btnUpgrade = ZonePrototypeUiChrome.AddBodyButton(detailsBody, "Mejorar Celda", 40f);
            _btnAssignAssistant = ZonePrototypeUiChrome.AddBodyButton(detailsBody, "Asignar/Quitar Asistente", 40f);
            _btnBuyCell = ZonePrototypeUiChrome.AddBodyButton(detailsBody, "Comprar Siguiente Celda", 40f);
            _btnCloseDetails = ZonePrototypeUiChrome.AddBodyButton(detailsBody, "Cerrar", 38f);
            ApplyButtonSkin(_btnProduce, false);
            ApplyButtonSkin(_btnCollect, false);
            ApplyButtonSkin(_btnUpgrade, false);
            ApplyButtonSkin(_btnAssignAssistant, false);
            ApplyButtonSkin(_btnBuyCell, false);
            ApplyButtonSkin(_btnCloseDetails, true);

            _btnProduce.onClick.AddListener(StartSelectedCellProduction);
            _btnCollect.onClick.AddListener(CollectSelectedCell);
            _btnUpgrade.onClick.AddListener(UpgradeSelectedCell);
            _btnAssignAssistant.onClick.AddListener(ToggleAssistantOnSelectedCell);
            _btnBuyCell.onClick.AddListener(BuyNextCell);
            _btnCloseDetails.onClick.AddListener(() =>
            {
                _selectedCell = null;
                RefreshSelectedCellPanel();
                if (_detailsPanelGo != null) _detailsPanelGo.SetActive(false);
            });

            var salesPanel = CreateImage(panel.transform, "SalesPanel", new Color(1f, 1f, 1f, 1f));
            ZonePrototypeUiChrome.ApplyHybridSidePanel(
                salesPanel,
                Ui_PanelSales,
                ZonePrototypeUiChrome.Zone1PanelSalesPath,
                new Color(0.13f, 0.14f, 0.12f, 0.92f));
            var sprt = salesPanel.rectTransform;
            sprt.anchorMin = new Vector2(1f, 0.5f);
            sprt.anchorMax = new Vector2(1f, 0.5f);
            sprt.pivot = new Vector2(1f, 0.5f);
            sprt.anchoredPosition = new Vector2(-16f, -30f);
            sprt.sizeDelta = new Vector2(320f, 640f);
            _salesPanelGo = salesPanel.gameObject;

            var salesBody = ZonePrototypeUiChrome.EnsurePanelBody(salesPanel.transform);
            _txtSales = ZonePrototypeUiChrome.AddBodyLabel(salesBody, "Sales", "", 16, 118f, TextAlignmentOptions.TopLeft);
            _txtSales.textWrappingMode = TextWrappingModes.Normal;
            var btnSellA = ZonePrototypeUiChrome.AddBodyButton(salesBody, "Vender Suministros", 40f);
            var btnSellB = ZonePrototypeUiChrome.AddBodyButton(salesBody, "Vender Planos", 40f);
            btnSellA.onClick.AddListener(SellSupplies);
            btnSellB.onClick.AddListener(SellBlueprints);

            _txtBuySection = ZonePrototypeUiChrome.AddBodyLabel(salesBody, "BuySection", "", 15, 56f, TextAlignmentOptions.TopLeft);
            _txtBuySection.textWrappingMode = TextWrappingModes.Normal;
            var btnBuyAssistant = ZonePrototypeUiChrome.AddBodyButton(salesBody, "Comprar Asistente", 40f);
            btnBuyAssistant.onClick.AddListener(BuyAssistant);
            ApplyButtonSkin(btnSellA, false);
            ApplyButtonSkin(btnSellB, false);
            ApplyButtonSkin(btnBuyAssistant, false);

            BuildAssistantRosterSection(salesBody);

            var hintPanel = CreateImage(panel.transform, "HintPanel", new Color(0.07f, 0.09f, 0.10f, 0.86f));
            var hintRt = hintPanel.rectTransform;
            hintRt.anchorMin = new Vector2(0.5f, 0f);
            hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 10f);
            hintRt.sizeDelta = new Vector2(760f, 42f);
            _txtHint = CreateLabel(hintPanel.transform, "Hint", "", 14, new Vector2(0f, 0f), new Vector2(740f, 28f));

            _btnSalesToggle.onClick.AddListener(() =>
            {
                if (_salesPanelGo == null) return;
                _salesPanelGo.SetActive(!_salesPanelGo.activeSelf);
            });

            btnBackZones.onClick.AddListener(() =>
            {
                PushSharedProgressToZone1Save();
                SaveManager.Instance?.SaveNow();
                AudioManager.Instance?.PlayZone2BackToZones();
                SceneManager.LoadScene("ZoneSelection");
            });

            RebuildCellsListUi();

            if (_salesPanelGo != null) _salesPanelGo.SetActive(false);
            if (_detailsPanelGo != null) _detailsPanelGo.SetActive(false);
        }

        bool TryBindSceneUi()
        {
            var uiRoot = GameObject.Find("UI");
            if (uiRoot == null)
                return false;
            var canvas = uiRoot.GetComponent<Canvas>();
            if (canvas == null)
                return false;

            var hud = uiRoot.transform.Find("HUDCanvas")?.GetComponent<Image>();
            var details = uiRoot.transform.Find("CellInfoPanel")?.GetComponent<Image>();
            var sales = uiRoot.transform.Find("SalesPanel")?.GetComponent<Image>();
            var hint = uiRoot.transform.Find("HoverInfoPanel")?.GetComponent<Image>();
            if (hud == null || details == null || sales == null || hint == null)
                return false;

            ZonePrototypeUiChrome.ApplyHybridSidePanel(
                details,
                Ui_PanelCellInfo,
                ZonePrototypeUiChrome.Zone1PanelCellPath,
                details.color);
            ZonePrototypeUiChrome.ApplyHybridSidePanel(
                sales,
                Ui_PanelSales,
                ZonePrototypeUiChrome.Zone1PanelSalesPath,
                sales.color);

            _detailsPanelGo = details.gameObject;
            _salesPanelGo = sales.gameObject;

            var salesRt = sales.GetComponent<RectTransform>();
            if (salesRt != null)
            {
                salesRt.anchoredPosition = new Vector2(salesRt.anchoredPosition.x, -30f);
                salesRt.sizeDelta = new Vector2(320f, 640f);
            }

            var hudRt = hud.GetComponent<RectTransform>();
            if (hudRt != null)
                hudRt.sizeDelta = new Vector2(hudRt.sizeDelta.x, 148f);

            ZonePrototypeUiChrome.MountTwoRowHud(
                hud.transform,
                hud,
                "Zona 2 - Ciudades Condensadas (Kthanid vigila los tributos)",
                Ui_PanelHud,
                hud.color,
                148f,
                out _txtHeader,
                out _txtResources,
                out _txtTax,
                out _txtDifficulty,
                out _btnSalesToggle,
                out var btnBackZones);
            ZonePrototypeUiChrome.ApplyHybridPackOrZone1Button(_btnSalesToggle, false, Ui_ButtonPrimary, Ui_ButtonDanger);
            ZonePrototypeUiChrome.ApplyHybridPackOrZone1Button(btnBackZones, true, Ui_ButtonPrimary, Ui_ButtonDanger);

            _btnSalesToggle.onClick.AddListener(() =>
            {
                if (_salesPanelGo == null) return;
                _salesPanelGo.SetActive(!_salesPanelGo.activeSelf);
            });

            btnBackZones.onClick.AddListener(() =>
            {
                PushSharedProgressToZone1Save();
                SaveManager.Instance?.SaveNow();
                AudioManager.Instance?.PlayZone2BackToZones();
                SceneManager.LoadScene("ZoneSelection");
            });

            var detailsBody = ZonePrototypeUiChrome.EnsurePanelBody(details.transform);
            _txtSelectedCell = ZonePrototypeUiChrome.AddBodyLabel(detailsBody, "SelectedCell", "", 16, 112f, TextAlignmentOptions.TopLeft);
            _txtSelectedCell.textWrappingMode = TextWrappingModes.Normal;
            _btnProduce = ZonePrototypeUiChrome.AddBodyButton(detailsBody, "Iniciar Producción", 40f);
            _btnCollect = ZonePrototypeUiChrome.AddBodyButton(detailsBody, "Recolectar", 40f);
            _btnUpgrade = ZonePrototypeUiChrome.AddBodyButton(detailsBody, "Mejorar Celda", 40f);
            _btnAssignAssistant = ZonePrototypeUiChrome.AddBodyButton(detailsBody, "Asignar/Quitar Asistente", 40f);
            _btnBuyCell = ZonePrototypeUiChrome.AddBodyButton(detailsBody, "Comprar Siguiente Celda", 40f);
            _btnCloseDetails = ZonePrototypeUiChrome.AddBodyButton(detailsBody, "Cerrar", 38f);

            ApplyButtonSkin(_btnProduce, false);
            ApplyButtonSkin(_btnCollect, false);
            ApplyButtonSkin(_btnUpgrade, false);
            ApplyButtonSkin(_btnAssignAssistant, false);
            ApplyButtonSkin(_btnBuyCell, false);
            ApplyButtonSkin(_btnCloseDetails, true);

            _btnProduce.onClick.AddListener(StartSelectedCellProduction);
            _btnCollect.onClick.AddListener(CollectSelectedCell);
            _btnUpgrade.onClick.AddListener(UpgradeSelectedCell);
            _btnAssignAssistant.onClick.AddListener(ToggleAssistantOnSelectedCell);
            _btnBuyCell.onClick.AddListener(BuyNextCell);
            _btnCloseDetails.onClick.AddListener(() =>
            {
                _selectedCell = null;
                RefreshSelectedCellPanel();
                if (_detailsPanelGo != null) _detailsPanelGo.SetActive(false);
            });

            var salesBody = ZonePrototypeUiChrome.EnsurePanelBody(sales.transform);
            _txtSales = ZonePrototypeUiChrome.AddBodyLabel(salesBody, "Sales", "", 16, 118f, TextAlignmentOptions.TopLeft);
            _txtSales.textWrappingMode = TextWrappingModes.Normal;
            var btnSellA = ZonePrototypeUiChrome.AddBodyButton(salesBody, "Vender Suministros", 40f);
            var btnSellB = ZonePrototypeUiChrome.AddBodyButton(salesBody, "Vender Planos", 40f);
            btnSellA.onClick.AddListener(SellSupplies);
            btnSellB.onClick.AddListener(SellBlueprints);

            _txtBuySection = ZonePrototypeUiChrome.AddBodyLabel(salesBody, "BuySection", "", 15, 56f, TextAlignmentOptions.TopLeft);
            _txtBuySection.textWrappingMode = TextWrappingModes.Normal;
            var btnBuyAssistant = ZonePrototypeUiChrome.AddBodyButton(salesBody, "Comprar Asistente", 40f);
            btnBuyAssistant.onClick.AddListener(BuyAssistant);
            ApplyButtonSkin(btnSellA, false);
            ApplyButtonSkin(btnSellB, false);
            ApplyButtonSkin(btnBuyAssistant, false);

            BuildAssistantRosterSection(salesBody);

            // Hint panel
            _txtHint = CreateLabel(hint.transform, "Hint", "", 14, Vector2.zero, new Vector2(740f, 28f));

            // World grid no se construye aquí; lo manejamos desde gameplay.
            _cellGridRoot = null;
            _cellButtons.Clear();

            if (_salesPanelGo != null) _salesPanelGo.SetActive(false);
            if (_detailsPanelGo != null) _detailsPanelGo.SetActive(false);

            return true;
        }

        void RefreshUi()
        {
            if (_txtResources == null)
                return;

            _txtResources.text = $"Monedas: {_darkCoins}  |  Ciudades condensadas: {_citySupplies}  |  Conocimiento antiguo: {_arcaneBlueprints}";
            _txtTax.text = $"Impuesto: {Mathf.Max(0f, _taxTimer):0.0}s  |  Multas globales: {GlobalTaxLedger.GetStrikes()}/3";
            _txtDifficulty.text = $"Nivel {_sharedLevel} (XP {_sharedXp}) | Tier {_difficultyTier} | Vendido {_totalSold}";
            if (string.IsNullOrEmpty(_txtHint.text))
                _txtHint.text = sharedEconomyWithZone1
                    ? "Economía compartida activa: mismas monedas y nivel en las 3 zonas. Multas de impuestos también globales."
                    : "Economía aislada activa para pruebas.";

            var assigned = CountAssignedAssistants();
            _txtSales.text =
                $"Panel de Ventas\n\n" +
                $"Ciudades condensadas: {_citySupplies}\n" +
                $"Conocimiento antiguo: {_arcaneBlueprints}\n" +
                $"Asistentes: {assigned}/{_assistantsTotal}\n";
            _txtBuySection.text = $"Compras\nAsistente: {_assistantBuyCost} monedas";

            RefreshSelectedCellPanel();
            RefreshCellGridVisuals();
            RefreshZ2AssistantRoster();
        }

        void RebuildCellsListUi()
        {
            if (_cellGridRoot == null)
                return;

            _cellButtons.Clear();
            for (var i = _cellGridRoot.childCount - 1; i >= 0; i--)
                Destroy(_cellGridRoot.GetChild(i).gameObject);

            foreach (var cell in _cells)
            {
                var btn = CreateButton(_cellGridRoot, $"Celda {cell.id + 1}", Vector2.zero, new Vector2(100f, 74f));
                var text = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.fontSize = 26;
                    text.alignment = TextAlignmentOptions.Center;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.text = "o";
                }
                var outline = btn.gameObject.GetComponent<Outline>();
                if (outline == null)
                    outline = btn.gameObject.AddComponent<Outline>();
                outline.effectDistance = new Vector2(1.2f, -1.2f);
                outline.useGraphicAlpha = true;
                btn.onClick.AddListener(() =>
                {
                    if (TryCompleteReassignToCellIndex(cell.id))
                    {
                        _selectedCell = cell;
                        RefreshSelectedCellPanel();
                        if (_detailsPanelGo != null) _detailsPanelGo.SetActive(_selectedCell != null);
                        return;
                    }
                    _selectedCell = cell;
                    RefreshSelectedCellPanel();
                    if (_detailsPanelGo != null) _detailsPanelGo.SetActive(_selectedCell != null);
                });
                _cellButtons[cell.id] = btn;
            }
        }

        void RefreshCellGridVisuals()
        {
            foreach (var cell in _cells)
            {
                if (!_cellButtons.TryGetValue(cell.id, out var btn) || btn == null)
                    continue;

                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                var img = btn.GetComponent<Image>();
                var outline = btn.GetComponent<Outline>();
                var isSelected = _selectedCell != null && _selectedCell.id == cell.id;
                if (txt != null)
                {
                    txt.fontSize = 26;
                    txt.text = "o";
                    txt.color = !cell.unlocked
                        ? new Color(0.45f, 0.45f, 0.50f, 0.55f)
                        : cell.ready
                            ? new Color(0.78f, 0.95f, 0.88f, 0.95f)
                            : new Color(0.66f, 0.78f, 0.94f, 0.92f);
                }

                if (img != null)
                {
                    img.color = !cell.unlocked
                        ? new Color(0.10f, 0.10f, 0.10f, 0.9f)
                        : cell.corrupted
                            ? new Color(0.35f, 0.10f, 0.10f, 0.92f)
                            : cell.ready
                                ? new Color(0.12f, 0.36f, 0.18f, 0.92f)
                                : cell.producing
                                    ? new Color(0.16f, 0.22f, 0.34f, 0.92f)
                                    : new Color(0.18f, 0.18f, 0.20f, 0.92f);
                }

                if (outline != null)
                {
                    outline.effectColor = isSelected
                        ? new Color(0.92f, 0.86f, 0.40f, 0.98f)
                        : new Color(0.36f, 0.36f, 0.30f, 0.9f);
                }
            }
        }

        void RefreshSelectedCellPanel()
        {
            if (_txtSelectedCell == null)
                return;
            if (_selectedCell == null)
            {
                _txtSelectedCell.text = "Selecciona una celda.";
                return;
            }

            var state = _selectedCell.corrupted ? "Corrupta" :
                _selectedCell.ready ? "Lista" :
                _selectedCell.producing ? $"Produciendo ({_selectedCell.remainingSeconds:0.0}s)" :
                _selectedCell.unlocked ? "Libre" : "Bloqueada";
            var assigned = GetAssignedAssistantsOnCell(_selectedCell.id);
            var lv = Zone2CellLevelRules.ClampLevel(_selectedCell.level);
            var nextCost = Zone2CellLevelRules.NextUpgradeCost(lv);
            var sec = Zone2CellLevelRules.BaseProductionSeconds(lv);
            _txtSelectedCell.text =
                $"{_selectedCell.displayName}\n" +
                $"Estado: {state}\n" +
                $"Nivel: {lv} / {Zone2CellLevelRules.MaxLevel}\n" +
                (Zone2CellLevelRules.CanUpgrade(lv)
                    ? $"Sig. mejora: {nextCost} mon. | Ciclo base ~{sec:0.0}s\n"
                    : "Máximo nivel alcanzado.\n") +
                $"Asistentes: {assigned}";

            var canOperate = _selectedCell.unlocked && !_selectedCell.corrupted;
            _btnProduce.interactable = canOperate && !_selectedCell.producing && !_selectedCell.ready;
            _btnCollect.interactable = canOperate && _selectedCell.ready;
            _btnUpgrade.interactable = canOperate && Zone2CellLevelRules.CanUpgrade(lv) && _darkCoins >= nextCost;
            _btnAssignAssistant.interactable = _selectedCell.unlocked;
            // Como Zona 1: la compra es por celda seleccionada si está bloqueada.
            _btnBuyCell.interactable = !_selectedCell.unlocked && _darkCoins >= _nextCellCost;
        }

        void BuildAssistantRosterSection(Transform salesBody)
        {
            if (_z2RosterWidgetBuilt)
                return;
            _z2RosterWidgetBuilt = true;

            ZonePrototypeUiChrome.AddBodyLabel(
                salesBody, "RosterHeader", "Asistentes (lista)", 16, 28f, TextAlignmentOptions.TopLeft);
            _txtReassignModeBanner = ZonePrototypeUiChrome.AddBodyLabel(
                salesBody, "RosterReassignBanner", " ", 13, 28f, TextAlignmentOptions.TopLeft);
            _txtReassignModeBanner.color = new Color(0.95f, 0.82f, 0.45f, 1f);
            _txtReassignModeBanner.text = "";

            var scrollGo = new GameObject("RosterScroll");
            scrollGo.transform.SetParent(salesBody, false);
            var sLe = scrollGo.AddComponent<LayoutElement>();
            sLe.minHeight = 200f;
            sLe.preferredHeight = 240f;
            sLe.flexibleWidth = 1f;

            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.sizeDelta = new Vector2(0f, 240f);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vRt = viewport.AddComponent<RectTransform>();
            StretchZ2(vRt);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var vImg = viewport.AddComponent<Image>();
            vImg.color = new Color(0.04f, 0.05f, 0.08f, 0.2f);
            vImg.raycastTarget = true;

            var content = new GameObject("RosterContent");
            content.transform.SetParent(viewport.transform, false);
            var cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(0f, 0f);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(0, 0, 0, 4);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fit = content.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vRt;
            scroll.content = cRt;

            for (var i = 0; i < MaxAssistants; i++)
            {
                var row = new Z2AssistantRosterRow
                {
                    Root = new GameObject($"RosterEntry_{i}"),
                };
                row.Root.transform.SetParent(content.transform, false);
                var rLe = row.Root.AddComponent<LayoutElement>();
                rLe.minHeight = 100f;
                rLe.preferredHeight = 100f;
                rLe.flexibleWidth = 1f;

                var vRow = row.Root.AddComponent<VerticalLayoutGroup>();
                vRow.spacing = 4f;
                vRow.childControlWidth = true;
                vRow.childControlHeight = true;
                vRow.childForceExpandWidth = true;
                vRow.childAlignment = TextAnchor.UpperLeft;

                var tGo = new GameObject("Line");
                tGo.transform.SetParent(row.Root.transform, false);
                var tLe = tGo.AddComponent<LayoutElement>();
                tLe.preferredHeight = 40f;
                tLe.minHeight = 32f;
                tLe.flexibleWidth = 1f;
                row.MainText = tGo.AddComponent<TextMeshProUGUI>();
                row.MainText.fontSize = 12;
                row.MainText.alignment = TextAlignmentOptions.TopLeft;
                row.MainText.textWrappingMode = TextWrappingModes.Normal;
                row.MainText.color = new Color(0.9f, 0.9f, 0.95f, 1f);
                row.MainText.raycastTarget = false;
                var tRt = row.MainText.rectTransform;
                tRt.sizeDelta = new Vector2(0f, 40f);

                var btnRow = new GameObject("BtnRow");
                btnRow.transform.SetParent(row.Root.transform, false);
                var bLe = btnRow.AddComponent<LayoutElement>();
                bLe.preferredHeight = 34f;
                bLe.minHeight = 32f;
                bLe.flexibleWidth = 1f;
                var h = btnRow.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 4f;
                h.childAlignment = TextAnchor.MiddleCenter;
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = true;
                h.childForceExpandHeight = true;

                var capture = i;
                var bIm = ZonePrototypeUiChrome.AddBodyButton(btnRow.transform, "Mejorar", 32f);
                var bRe = ZonePrototypeUiChrome.AddBodyButton(btnRow.transform, "Reasignar", 32f);
                var bUn = ZonePrototypeUiChrome.AddBodyButton(btnRow.transform, "Retirar", 32f);
                bIm.GetComponentInChildren<TextMeshProUGUI>().fontSize = 12;
                bRe.GetComponentInChildren<TextMeshProUGUI>().fontSize = 12;
                bUn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 12;
                row.Improve = bIm;
                row.Reassign = bRe;
                row.Unassign = bUn;
                ApplyButtonSkin(bIm, false);
                ApplyButtonSkin(bRe, false);
                ApplyButtonSkin(bUn, true);

                bIm.onClick.AddListener(() => UpgradeZone2AssistantByIndex(capture));
                bRe.onClick.AddListener(() => BeginReassignAssistantByIndex(capture));
                bUn.onClick.AddListener(() => RetireUnassignAssistantByIndex(capture));

                _z2RosterRows.Add(row);
            }
        }

        void RefreshZ2AssistantRoster()
        {
            if (_z2RosterRows == null || _z2RosterRows.Count == 0)
                return;

            if (_txtReassignModeBanner != null)
            {
                if (_reassignAssistantIndex < 0)
                {
                    _txtReassignModeBanner.text = "";
                }
                else
                {
                    _txtReassignModeBanner.text =
                        $"Toca una celda desbloqueada en la rejilla o en el mapa 3D (asist. n.º {_reassignAssistantIndex + 1}).";
                }
            }

            var n = _zone2Assistants.Count;
            for (var i = 0; i < _z2RosterRows.Count; i++)
            {
                var row = _z2RosterRows[i];
                if (row?.Root == null)
                    continue;
                var show = i < n;
                row.Root.SetActive(show);
                if (!show)
                    continue;

                var a = _zone2Assistants[i];
                var typeName = Zone2DistrictPaths.GetDisplayName((Zone2DistrictType)((int)a.type % 4));
                var nextCost = NextAssistantUpgradeCost(a.level);
                var canUp = a.level < Z2RosterMaxAssistantLevel && _darkCoins >= nextCost;
                var cellName = a.cellId < 0 ? "Sin asignar" : Z2NameForCellId(a.cellId);
                if (row.MainText != null)
                {
                    row.MainText.text =
                        $"Asistente {i + 1}\n" +
                        $"Tipo: {typeName}  ·  Nivel: {a.level}\n" +
                        $"Celda: {cellName}\n" +
                        (a.level < Z2RosterMaxAssistantLevel
                            ? $"Mejora: {nextCost} m."
                            : "Nivel máximo.");
                }
                if (row.Improve != null)
                {
                    row.Improve.interactable = a.level < Z2RosterMaxAssistantLevel && canUp;
                }
                if (row.Reassign != null)
                {
                    row.Reassign.interactable = true;
                }
                if (row.Unassign != null)
                {
                    row.Unassign.interactable = a.cellId >= 0;
                }
            }
        }

        static void StretchZ2(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static RectTransform BuildCellGrid(Transform parent, int columns, int rows, Vector2 cellSize, Vector2 spacing)
        {
            var root = new GameObject("CellGrid");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(columns * cellSize.x + (columns - 1) * spacing.x, rows * cellSize.y + (rows - 1) * spacing.y);

            var grid = root.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.childAlignment = TextAnchor.MiddleCenter;
            return rt;
        }

        static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static void ApplyUiSprite(Image img, string assetPath, Color fallbackTint)
        {
            if (img == null)
                return;

            var sprite = TryLoadSprite(assetPath);
            if (sprite == null)
            {
                img.sprite = null;
                img.color = fallbackTint;
                return;
            }

            img.sprite = sprite;
            img.color = Color.white;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
        }

        static void ApplyButtonSkin(Button btn, bool danger)
        {
            if (btn == null)
                return;
            var img = btn.GetComponent<Image>();
            if (img == null)
                return;

            var sprite = TryLoadSprite(danger ? Ui_ButtonDanger : Ui_ButtonPrimary);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                return;
            }

            ZonePrototypeUiChrome.ApplyHybridPackOrZone1Button(btn, danger, Ui_ButtonPrimary, Ui_ButtonDanger);
        }

        static Sprite TryLoadSprite(string assetPath)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(assetPath))
                return null;
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
            return null;
#endif
        }

        static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, int size, Vector2 pos, Vector2 rectSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = rectSize;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        static Button CreateButton(Transform parent, string text, Vector2 pos, Vector2 rectSize)
        {
            var go = new GameObject($"Button_{text}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = rectSize;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.16f, 0.21f, 0.26f, 1f);
            var btn = go.AddComponent<Button>();
            var lbl = CreateLabel(go.transform, "Text", text, 20, Vector2.zero, rectSize);
            lbl.rectTransform.anchorMin = Vector2.zero;
            lbl.rectTransform.anchorMax = Vector2.one;
            lbl.rectTransform.offsetMin = Vector2.zero;
            lbl.rectTransform.offsetMax = Vector2.zero;
            return btn;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
