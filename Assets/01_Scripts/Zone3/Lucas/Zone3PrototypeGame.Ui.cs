using LasGranjasDelHastur;
using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone3
{
    /// <summary>UI y paneles narrativos para Zona 3 (separado del gameplay).</summary>
    public partial class Zone3PrototypeGame
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
        GameObject _endPanel;

        void BuildUi()
        {
            if (TryBindSceneUi())
                return;

            var root = new GameObject("Zone3PrototypeUI");
            root.transform.SetParent(transform, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.65f;
            root.AddComponent<GraphicRaycaster>();

            var backdrop = CreateImage(root.transform, "Backdrop", new Color(0.03f, 0.02f, 0.08f, 0.05f));
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

            var hud = CreateImage(panel.transform, "HUDTop", Color.white);
            var hudRt = hud.rectTransform;
            hudRt.anchorMin = new Vector2(0f, 1f);
            hudRt.anchorMax = new Vector2(1f, 1f);
            hudRt.pivot = new Vector2(0.5f, 1f);
            hudRt.sizeDelta = new Vector2(0f, 148f);
            hudRt.anchoredPosition = Vector2.zero;

            var hudFallback = new Color(0.06f, 0.05f, 0.11f, 0.88f);
            ZonePrototypeUiChrome.MountTwoRowHud(
                hud.transform,
                hud,
                "Zona 3 - Cuerpos Celestes (Azathoth exige tributo)",
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

            var gridPanel = CreateImage(panel.transform, "CellGridPanel", Color.white);
            ApplyZ3TaxGridPanel(gridPanel);
            var gridRt = gridPanel.rectTransform;
            gridRt.anchorMin = new Vector2(0.5f, 0.5f);
            gridRt.anchorMax = new Vector2(0.5f, 0.5f);
            gridRt.pivot = new Vector2(0.5f, 0.5f);
            gridRt.sizeDelta = new Vector2(760f, 470f);
            gridRt.anchoredPosition = new Vector2(0f, -12f);
            _cellGridRoot = BuildCellGrid(gridPanel.transform, 4, 3, new Vector2(140f, 112f), new Vector2(10f, 10f));

            var detailsPanel = CreateImage(panel.transform, "CellDetails", Color.white);
            ZonePrototypeUiChrome.ApplyHybridSidePanel(
                detailsPanel,
                Ui_PanelCellInfo,
                ZonePrototypeUiChrome.Zone1PanelCellPath,
                new Color(0.12f, 0.11f, 0.20f, 0.92f));
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

            var salesPanel = CreateImage(panel.transform, "SalesPanel", Color.white);
            ZonePrototypeUiChrome.ApplyHybridSidePanel(
                salesPanel,
                Ui_PanelSales,
                ZonePrototypeUiChrome.Zone1PanelSalesPath,
                new Color(0.14f, 0.10f, 0.16f, 0.92f));
            var sprt = salesPanel.rectTransform;
            sprt.anchorMin = new Vector2(1f, 0.5f);
            sprt.anchorMax = new Vector2(1f, 0.5f);
            sprt.pivot = new Vector2(1f, 0.5f);
            sprt.anchoredPosition = new Vector2(-16f, -40f);
            sprt.sizeDelta = new Vector2(320f, 400f);
            _salesPanelGo = salesPanel.gameObject;

            var salesBody = ZonePrototypeUiChrome.EnsurePanelBody(salesPanel.transform);
            _txtSales = ZonePrototypeUiChrome.AddBodyLabel(salesBody, "Sales", "", 16, 118f, TextAlignmentOptions.TopLeft);
            _txtSales.textWrappingMode = TextWrappingModes.Normal;
            var btnSellResidue = ZonePrototypeUiChrome.AddBodyButton(salesBody, "Vender Residuo", 40f);
            var btnSellInk = ZonePrototypeUiChrome.AddBodyButton(salesBody, "Vender Tinta", 40f);
            btnSellResidue.onClick.AddListener(SellResidue);
            btnSellInk.onClick.AddListener(SellInk);
            _txtBuySection = ZonePrototypeUiChrome.AddBodyLabel(salesBody, "BuySection", "", 15, 56f, TextAlignmentOptions.TopLeft);
            _txtBuySection.textWrappingMode = TextWrappingModes.Normal;
            var btnBuyAssistant = ZonePrototypeUiChrome.AddBodyButton(salesBody, "Comprar Asistente", 40f);
            btnBuyAssistant.onClick.AddListener(BuyAssistant);
            ApplyButtonSkin(btnSellResidue, false);
            ApplyButtonSkin(btnSellInk, false);
            ApplyButtonSkin(btnBuyAssistant, false);

            var hintPanel = CreateImage(panel.transform, "HintPanel", new Color(0.08f, 0.06f, 0.12f, 0.86f));
            var hintRt = hintPanel.rectTransform;
            hintRt.anchorMin = new Vector2(0.5f, 0f);
            hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 10f);
            hintRt.sizeDelta = new Vector2(760f, 42f);
            _txtHint = CreateLabel(hintPanel.transform, "Hint", "", 14, Vector2.zero, new Vector2(740f, 28f));

            _btnSalesToggle.onClick.AddListener(() =>
            {
                if (_salesPanelGo == null) return;
                _salesPanelGo.SetActive(!_salesPanelGo.activeSelf);
            });

            btnBackZones.onClick.AddListener(() =>
            {
                PushSharedProgressToZone1Save();
                SaveManager.Instance?.SaveNow();
                AudioManager.Instance?.PlayZone3BackToZones();
                SceneManager.LoadScene("ZoneSelection");
            });

            BuildEndPanel(root.transform);
            RebuildCellsListUi();

            _z3TaxFxCanvasRoot = root.transform;
            EnsureZ3TaxFlashHost(_z3TaxFxCanvasRoot);

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

            var hudRtBind = hud.GetComponent<RectTransform>();
            if (hudRtBind != null)
                hudRtBind.sizeDelta = new Vector2(hudRtBind.sizeDelta.x, 148f);

            ZonePrototypeUiChrome.MountTwoRowHud(
                hud.transform,
                hud,
                "Zona 3 - Cuerpos Celestes (Azathoth exige tributo)",
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
                AudioManager.Instance?.PlayZone3BackToZones();
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
            var btnSellResidue = ZonePrototypeUiChrome.AddBodyButton(salesBody, "Vender Residuo", 40f);
            var btnSellInk = ZonePrototypeUiChrome.AddBodyButton(salesBody, "Vender Tinta", 40f);
            btnSellResidue.onClick.AddListener(SellResidue);
            btnSellInk.onClick.AddListener(SellInk);

            _txtBuySection = ZonePrototypeUiChrome.AddBodyLabel(salesBody, "BuySection", "", 15, 56f, TextAlignmentOptions.TopLeft);
            _txtBuySection.textWrappingMode = TextWrappingModes.Normal;
            var btnBuyAssistant = ZonePrototypeUiChrome.AddBodyButton(salesBody, "Comprar Asistente", 40f);
            btnBuyAssistant.onClick.AddListener(BuyAssistant);
            ApplyButtonSkin(btnSellResidue, false);
            ApplyButtonSkin(btnSellInk, false);
            ApplyButtonSkin(btnBuyAssistant, false);

            _txtHint = CreateLabel(hint.transform, "Hint", "", 14, Vector2.zero, new Vector2(740f, 28f));

            _cellGridRoot = null;
            _cellButtons.Clear();

            // Panel final narrativo lo seguimos construyendo a runtime.
            BuildEndPanel(uiRoot.transform);

            if (_salesPanelGo != null) _salesPanelGo.SetActive(false);
            if (_detailsPanelGo != null) _detailsPanelGo.SetActive(false);

            _z3TaxFxCanvasRoot = uiRoot.transform;
            EnsureZ3TaxFlashHost(_z3TaxFxCanvasRoot);

            return true;
        }

        void BuildEndPanel(Transform parent)
        {
            _endPanel = new GameObject("EndPanel");
            _endPanel.transform.SetParent(parent, false);
            var bg = _endPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.8f);
            var rt = _endPanel.GetComponent<RectTransform>();
            Stretch(rt);

            var panel = CreateImage(_endPanel.transform, "Narrative", new Color(0.12f, 0.10f, 0.16f, 0.95f));
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(920f, 380f);

            CreateLabel(panel.transform, "Title", "Final Narrativo Alcanzado", 34, new Vector2(0f, 130f), new Vector2(820f, 60f));
            CreateLabel(panel.transform, "Body",
                "El portal celestial responde a tu granja ritual.\nPuedes continuar en modo infinito o reiniciar con prestigio.",
                22, new Vector2(0f, 52f), new Vector2(840f, 110f));

            var btnEndless = CreateButton(panel.transform, "Modo infinito", new Vector2(-170f, -120f), new Vector2(260f, 54f));
            btnEndless.onClick.AddListener(() =>
            {
                _endPanel.SetActive(false);
                _txtHint.text = "Modo infinito activo: la dificultad seguirá escalando.";
            });

            var btnPrestige = CreateButton(panel.transform, "Prestigio +1", new Vector2(170f, -120f), new Vector2(260f, 54f));
            btnPrestige.onClick.AddListener(ApplyPrestige);

            _endPanel.SetActive(false);
        }

        void RefreshUi()
        {
            if (_txtResources != null)
                _txtResources.text = $"Monedas: {_darkCoins}  |  Polvo de cometa: {_astralResidue}  |  Energía estelar: {_voidInk}";
            if (_txtTax != null)
                _txtTax.text = $"Impuesto: {Mathf.Max(0f, _taxTimer):0.0}s  |  Multas globales: {GlobalTaxLedger.GetStrikes()}/3";
            if (_txtDifficulty != null)
                _txtDifficulty.text = $"Nivel {_sharedLevel} (XP {_sharedXp}) | Tier {_difficultyTier} | Prestigio {_prestigePoints}";
            if (_txtHint != null && string.IsNullOrEmpty(_txtHint.text))
                _txtHint.text = "Economía compartida: monedas, nivel y multas fiscales globales entre zonas.";

            var assigned = CountAssignedAssistants();
            if (_txtSales != null)
            {
                _txtSales.text =
                    $"Panel de Ventas\n\n" +
                    $"Polvo de cometa: {_astralResidue}\n" +
                    $"Energía estelar: {_voidInk}\n" +
                    $"Asistentes: {assigned}/{_assistantsTotal}";
            }

            if (_txtBuySection != null)
                _txtBuySection.text = $"Compras\nAsistente: {_assistantBuyCost} monedas";

            RefreshSelectedCellPanel();
            RefreshCellGridVisuals();
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
                            ? new Color(0.90f, 0.86f, 0.98f, 0.95f)
                            : new Color(0.74f, 0.78f, 0.98f, 0.92f);
                }

                if (img != null)
                {
                    img.color = !cell.unlocked
                        ? new Color(0.10f, 0.10f, 0.10f, 0.9f)
                        : cell.corrupted
                            ? new Color(0.35f, 0.10f, 0.22f, 0.92f)
                            : cell.ready
                                ? new Color(0.20f, 0.34f, 0.18f, 0.92f)
                                : cell.producing
                                    ? new Color(0.22f, 0.22f, 0.40f, 0.92f)
                                    : new Color(0.18f, 0.16f, 0.24f, 0.92f);
                }

                if (outline != null)
                {
                    outline.effectColor = isSelected
                        ? new Color(0.92f, 0.86f, 0.40f, 0.98f)
                        : new Color(0.36f, 0.34f, 0.46f, 0.9f);
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
            _txtSelectedCell.text =
                $"{_selectedCell.displayName}\n" +
                $"Estado: {state}\n" +
                $"Nivel: {_selectedCell.level}\n" +
                $"Asistentes: {assigned}";

            var canOperate = _selectedCell.unlocked && !_selectedCell.corrupted;
            _btnProduce.interactable = canOperate && !_selectedCell.producing && !_selectedCell.ready;
            _btnCollect.interactable = canOperate && _selectedCell.ready;
            _btnUpgrade.interactable = canOperate && _darkCoins >= 70 * _selectedCell.level;
            _btnAssignAssistant.interactable = _selectedCell.unlocked;
            _btnBuyCell.interactable = !_selectedCell.unlocked && _darkCoins >= _nextCellCost;
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

        static void ApplyZ3TaxGridPanel(Image gridPanel)
        {
            if (gridPanel == null)
                return;
            var sprite = TryLoadSprite(TaxCollectorArtPaths.Z3Panel);
            if (sprite != null)
            {
                gridPanel.sprite = sprite;
                gridPanel.color = Color.white;
                gridPanel.type = Image.Type.Simple;
                gridPanel.preserveAspect = false;
                return;
            }

            ApplyUiSprite(gridPanel, Ui_PanelTaxAlert, fallbackTint: new Color(0.08f, 0.07f, 0.14f, 0.66f));
        }

        void PlayZone3TaxCollectorPresentation(bool shortFlash)
        {
            EnsureZ3TaxFlashHost(_z3TaxFxCanvasRoot != null ? _z3TaxFxCanvasRoot : transform);
            if (_z3TaxFxRoot == null)
                return;
            if (_z3TaxFxRoutine != null)
            {
                StopCoroutine(_z3TaxFxRoutine);
                _z3TaxFxRoutine = null;
            }

            _z3TaxFxRoutine = StartCoroutine(Zone3TaxFxRoutine(shortFlash));
        }

        void EnsureZ3TaxFlashHost(Transform canvasRoot)
        {
            if (canvasRoot == null)
                return;

            var existing = canvasRoot.Find("Z3TaxCollectorFx");
            if (existing != null)
            {
                _z3TaxFxCanvasRoot = canvasRoot;
                _z3TaxFxRoot = existing.GetComponent<CanvasGroup>();
                _z3TaxFxPortrait = existing.Find("Portrait")?.GetComponent<Image>();
                _z3TaxFxStrip = existing.Find("ArrivalStrip")?.GetComponent<Image>();
                if (_z3TaxFxRoot == null)
                {
                    _z3TaxFxRoot = existing.gameObject.AddComponent<CanvasGroup>();
                    _z3TaxFxRoot.alpha = 0f;
                    _z3TaxFxRoot.blocksRaycasts = false;
                }

                return;
            }

            _z3TaxFxCanvasRoot = canvasRoot;
            var host = new GameObject("Z3TaxCollectorFx");
            host.transform.SetParent(canvasRoot, false);
            var rt = host.AddComponent<RectTransform>();
            Stretch(rt);
            _z3TaxFxRoot = host.AddComponent<CanvasGroup>();
            _z3TaxFxRoot.alpha = 0f;
            _z3TaxFxRoot.blocksRaycasts = false;
            _z3TaxFxRoot.interactable = false;

            var dim = CreateImage(host.transform, "Dim", new Color(0.05f, 0.02f, 0.12f, 0.58f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = false;

            var portraitGo = new GameObject("Portrait");
            portraitGo.transform.SetParent(host.transform, false);
            var prt = portraitGo.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(300f, 300f);
            prt.anchoredPosition = new Vector2(0f, 28f);
            _z3TaxFxPortrait = portraitGo.AddComponent<Image>();
            _z3TaxFxPortrait.preserveAspect = true;
            _z3TaxFxPortrait.raycastTarget = false;

            var stripGo = new GameObject("ArrivalStrip");
            stripGo.transform.SetParent(host.transform, false);
            var srt = stripGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(380f, 96f);
            srt.anchoredPosition = new Vector2(0f, -160f);
            _z3TaxFxStrip = stripGo.AddComponent<Image>();
            _z3TaxFxStrip.preserveAspect = true;
            _z3TaxFxStrip.raycastTarget = false;
        }

        IEnumerator Zone3TaxFxRoutine(bool shortFlash)
        {
            if (_z3TaxFxRoot == null)
                yield break;

            if (_z3TaxFxPortrait != null)
            {
                var p = TryLoadSprite(TaxCollectorArtPaths.Z3Seal);
                _z3TaxFxPortrait.sprite = p;
                if (p != null)
                    _z3TaxFxPortrait.color = shortFlash ? Color.white : new Color(1f, 0.65f, 0.85f, 1f);
                else
                    _z3TaxFxPortrait.color = new Color(1f, 1f, 1f, 0f);
            }

            var frames = Zone1ArtProvider.LoadHorizontalStrip(TaxCollectorArtPaths.ArrivalSheet4Frames, TaxCollectorArtPaths.ArrivalFrameCount);
            var frameIdx = 0;
            var frameAcc = 0f;
            const float frameDt = 0.1f;
            var hold = shortFlash ? 0.78f : 1.22f;
            const float fadeIn = 0.12f;
            const float fadeOut = 0.22f;
            var peakAlpha = shortFlash ? 0.9f : 0.98f;

            for (var t = 0f; t < fadeIn; t += Time.unscaledDeltaTime)
            {
                _z3TaxFxRoot.alpha = Mathf.Clamp01(t / fadeIn) * peakAlpha;
                yield return null;
            }

            _z3TaxFxRoot.alpha = peakAlpha;
            var elapsed = 0f;
            while (elapsed < hold)
            {
                elapsed += Time.unscaledDeltaTime;
                if (frames != null && frames.Length > 0 && _z3TaxFxStrip != null)
                {
                    frameAcc += Time.unscaledDeltaTime;
                    while (frameAcc >= frameDt)
                    {
                        frameAcc -= frameDt;
                        frameIdx = (frameIdx + 1) % frames.Length;
                    }

                    _z3TaxFxStrip.sprite = frames[frameIdx];
                    _z3TaxFxStrip.color = Color.white;
                }

                yield return null;
            }

            var startAlpha = _z3TaxFxRoot.alpha;
            for (var t = 0f; t < fadeOut; t += Time.unscaledDeltaTime)
            {
                _z3TaxFxRoot.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(t / fadeOut));
                yield return null;
            }

            _z3TaxFxRoot.alpha = 0f;
            if (_z3TaxFxStrip != null)
            {
                _z3TaxFxStrip.sprite = null;
                _z3TaxFxStrip.color = new Color(1f, 1f, 1f, 0f);
            }

            _z3TaxFxRoutine = null;
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
            if (string.IsNullOrEmpty(assetPath))
                return null;

#if UNITY_EDITOR
            var editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (editorSprite != null)
                return editorSprite;
#endif
            var res = TaxCollectorArtPaths.ToResourcesLoadPath(assetPath);
            if (!string.IsNullOrEmpty(res))
            {
                var loaded = Resources.Load<Sprite>(res);
                if (loaded != null)
                    return loaded;
            }

            return Zone1ArtProvider.LoadSprite(assetPath);
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
            bg.color = new Color(0.17f, 0.20f, 0.28f, 1f);
            var btn = go.AddComponent<Button>();

            var lbl = CreateLabel(go.transform, "Text", text, 22, Vector2.zero, rectSize);
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
