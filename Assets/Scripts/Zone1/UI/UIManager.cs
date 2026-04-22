using System;
using System.Collections.Generic;
using LasGranjasDelHastur.Zone1.Cells;
using LasGranjasDelHastur.Core;
using TMPro;
using LasGranjasDelHastur;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone1.UI
{
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string zoneSelectionSceneName = "ZoneSelection";

        ResourceManager _resources;
        ProgressionManager _progression;
        CellManager _cells;
        AssistantManager _assistants;
        BuyerManager _buyers;
        TaxManager _tax;

        Canvas _canvas;

        // HUD
        TextMeshProUGUI _txtMoney;
        TextMeshProUGUI _txtWeakSouls;
        TextMeshProUGUI _txtEnergy;
        TextMeshProUGUI _txtLevel;
        Image _xpFill;
        TextMeshProUGUI _txtTaxTimer;
        TextMeshProUGUI _txtStrikes;
        TextMeshProUGUI _txtAssistants;

        Button _btnSales;
        Button _btnBack;

        // Panels
        GameObject _cellPanel;
        GameObject _salesPanel;
        GameObject _taxPanel;
        GameObject _hoverPanel;

        // Cell panel bindings
        TextMeshProUGUI _cellTitle;
        TextMeshProUGUI _cellBody;
        Button _cellProduceBtn;
        Button _cellCollectBtn;
        Button _cellUpgradeBtn;
        Button _cellBuyBtn;
        Button _cellCleanseBtn;
        Button _cellAssistantBtn;

        // Sales bindings
        RectTransform _salesListRoot;
        readonly List<GameObject> _saleRows = new();

        // Tax bindings
        TextMeshProUGUI _taxTitle;
        TextMeshProUGUI _taxBody;
        Button _taxPayBtn;
        Image _taxPortrait;
        TextMeshProUGUI _txtZoneLabel;

        // Hover bindings
        TextMeshProUGUI _hoverText;
        TextMeshProUGUI _txtActionHint;

        FarmCell _boundCell;
        float _uiSelfHealTimer;
        bool _eventsWired;

        public void Initialize(ResourceManager resources, ProgressionManager progression, CellManager cells, AssistantManager assistants, BuyerManager buyers, TaxManager tax)
        {
            _resources = resources;
            _progression = progression;
            _cells = cells;
            _assistants = assistants;
            _buyers = buyers;
            _tax = tax;

            BuildUI();
            WireEvents();
            RefreshAll();
        }

        public void RefreshFromExternalState()
        {
            RefreshAll();
        }

        void Update()
        {
            _uiSelfHealTimer += Time.unscaledDeltaTime;
            if (_uiSelfHealTimer >= 2f)
            {
                _uiSelfHealTimer = 0f;
                SelfHealUiIfNeeded();
            }

            if (InputAdapter.RightMouseDownThisFrame())
            {
                CloseCellPanel();
                CloseSalesPanel();
            }

            if (_tax != null && _txtTaxTimer != null)
                _txtTaxTimer.text = $"Impuesto: {FormatTime(_tax.IsAlertActive ? _tax.PayWindowRemainingSeconds : _tax.TimeToNextTaxSeconds)}";
        }

        void SelfHealUiIfNeeded()
        {
            // Ensure only one top-level UI root exists.
            GetOrCreateSingleUiRoot();

            // If any placeholder panel was somehow re-enabled without proper bindings, close it.
            // Runtime panels are controlled by this manager and keep their references.
            if (_cellPanel == null || _salesPanel == null || _taxPanel == null || _hoverPanel == null)
                return;

            // Keep valid runtime behavior: only close suspicious states.
            if (_boundCell == null && _cellPanel.activeSelf)
                _cellPanel.SetActive(false);
            if (_tax != null && !_tax.IsAlertActive && _taxPanel.activeSelf)
                _taxPanel.SetActive(false);
        }

        void WireEvents()
        {
            if (_eventsWired)
                return;

            if (_resources != null) _resources.Changed += RefreshHUD;
            if (_progression != null) _progression.Changed += RefreshHUD;

            if (_cells != null)
            {
                _cells.SelectedCellChanged += OnCellSelected;
                _cells.CellsChanged += RefreshCellPanel;
            }
            if (_assistants != null)
                _assistants.Changed += OnAssistantsChanged;

            if (_buyers != null) _buyers.Changed += RefreshSalesPanel;
            if (_tax != null)
            {
                _tax.Changed += RefreshTaxPanel;
                _tax.AlertOpened += OpenTaxPanel;
                _tax.AlertClosed += CloseTaxPanel;
            }

            Zone1UIHoverBus.HoverChanged += OnHoverChanged;

            _btnSales.onClick.AddListener(() =>
            {
                if (_salesPanel.activeSelf) CloseSalesPanel();
                else OpenSalesPanel();
            });

            _btnBack.onClick.AddListener(() =>
            {
                SaveManager.Instance?.SaveNow();
                SceneManager.LoadScene(zoneSelectionSceneName);
            });

            _eventsWired = true;
        }

        void UnwireEvents()
        {
            if (!_eventsWired)
                return;

            if (_resources != null) _resources.Changed -= RefreshHUD;
            if (_progression != null) _progression.Changed -= RefreshHUD;

            if (_cells != null)
            {
                _cells.SelectedCellChanged -= OnCellSelected;
                _cells.CellsChanged -= RefreshCellPanel;
            }

            if (_buyers != null) _buyers.Changed -= RefreshSalesPanel;
            if (_assistants != null) _assistants.Changed -= OnAssistantsChanged;
            if (_tax != null)
            {
                _tax.Changed -= RefreshTaxPanel;
                _tax.AlertOpened -= OpenTaxPanel;
                _tax.AlertClosed -= CloseTaxPanel;
            }

            Zone1UIHoverBus.HoverChanged -= OnHoverChanged;
            _eventsWired = false;
        }

        void OnDestroy()
        {
            UnwireEvents();
        }

        void RefreshAll()
        {
            RefreshHUD();
            RefreshCellPanel();
            RefreshSalesPanel();
            RefreshTaxPanel();
        }

        void OnAssistantsChanged()
        {
            RefreshHUD();
            RefreshCellPanel();
        }

        void RefreshHUD()
        {
            if (_resources == null || _progression == null)
                return;

            _txtMoney.text = $"{_resources.Get(ResourceType.DarkCoins)}";
            _txtMoney.text = $"Monedas oscuras: {_resources.Get(ResourceType.DarkCoins)}";
            _txtWeakSouls.text = $"Almas débiles: {_resources.Get(ResourceType.WeakSouls)}";
            _txtEnergy.text = $"Energía pura: {_resources.Get(ResourceType.PureEnergy)}";
            _txtLevel.text = $"Nivel {_progression.Level}";
            _xpFill.fillAmount = _progression.XpProgress01();
            if (_txtAssistants != null && _assistants != null)
                _txtAssistants.text = $"Asistentes: {_assistants.AvailableAssistants}/{_assistants.TotalAssistants}";

            if (_tax != null)
                _txtStrikes.text = $"Multas: {_tax.Strikes}/3";
        }

        void OnCellSelected(FarmCell cell)
        {
            _boundCell = cell;
            if (cell == null)
            {
                CloseCellPanel();
                return;
            }

            OpenCellPanel();
            RefreshCellPanel();

            if (AudioManager.Instance != null && AudioManager.Instance.zone1CellClick != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.zone1CellClick);
        }

        void RefreshCellPanel()
        {
            if (_cellPanel == null || !_cellPanel.activeSelf)
                return;
            if (_boundCell == null)
                return;

            _cellTitle.text = _boundCell.DisplayName;

            var resourceName = _boundCell.ProducesResource switch
            {
                ResourceType.WeakSouls => "Almas débiles",
                ResourceType.PureEnergy => "Energía pura",
                ResourceType.MemoryShards => "Fragmentos de recuerdo",
                ResourceType.UnstableSouls => "Almas inestables",
                _ => _boundCell.ProducesResource.ToString()
            };

            var state = _boundCell.State.ToString();
            var corrupt = _boundCell.IsCorrupted ? "Sí" : "No";
            var prod = _boundCell.State == CellState.Producing ? $" ({FormatTime(_boundCell.ProducingRemainingSeconds)})" : "";
            var resourceAmount = _resources.Get(_boundCell.ProducesResource);
            var resourceCap = _resources.GetCapacity(_boundCell.ProducesResource);
            var storageLine = _resources.HasFiniteCapacity(_boundCell.ProducesResource)
                ? $"{resourceAmount}/{resourceCap}"
                : $"{resourceAmount}";
            var storageFull = _resources.IsAtCapacity(_boundCell.ProducesResource) ? " (lleno)" : "";
            var assistantCount = _assistants != null ? _assistants.GetAssistantCountOnCell(_boundCell) : 0;

            _cellBody.text =
                $"Nivel: {_boundCell.Level}\n" +
                $"Recurso: {resourceName}\n" +
                $"Tiempo: {Mathf.Max(0.1f, _boundCell.ProductionSeconds):0.0}s\n" +
                $"Por ciclo: {_boundCell.ProductionAmount}\n" +
                $"Almacén: {storageLine}{storageFull}\n" +
                $"Estado: {state}{prod}\n" +
                $"Corrupta: {corrupt}\n" +
                $"Asistente: {assistantCount} (máx 1)\n" +
                $"Costo compra: {_boundCell.PurchaseCostDarkCoins}\n" +
                $"Costo mejora: {_boundCell.UpgradeCostDarkCoins}";

            _cellProduceBtn.interactable = _boundCell.CanProduce(_resources);
            _cellCollectBtn.interactable = _boundCell.CanCollect(_resources);
            _cellUpgradeBtn.interactable = _boundCell.CanUpgrade(_resources);
            _cellBuyBtn.interactable = _boundCell.CanBuy(_resources);
            _cellCleanseBtn.interactable = _boundCell.CanCleanse(_resources);
            _cellAssistantBtn.interactable = _assistants != null &&
                (_assistants.HasAssistantOnCell(_boundCell) || _assistants.CanAssignToCell(_boundCell));

            _cellProduceBtn.onClick.RemoveAllListeners();
            _cellProduceBtn.onClick.AddListener(() =>
            {
                if (_boundCell.TryStartProduction(_resources))
                    _cells.ApplyVisual(_boundCell);
            });

            _cellCollectBtn.onClick.RemoveAllListeners();
            _cellCollectBtn.onClick.AddListener(() =>
            {
                if (_boundCell.TryCollect(_resources, _progression, out var t, out var amt))
                {
                    _cells.ApplyVisual(_boundCell);
                    if (AudioManager.Instance != null && AudioManager.Instance.zone1Collect != null)
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.zone1Collect);
                }
            });

            _cellUpgradeBtn.onClick.RemoveAllListeners();
            _cellUpgradeBtn.onClick.AddListener(() =>
            {
                if (_boundCell.TryUpgrade(_resources, _progression))
                    _cells.ApplyVisual(_boundCell);
            });

            _cellBuyBtn.onClick.RemoveAllListeners();
            _cellBuyBtn.onClick.AddListener(() =>
            {
                if (_boundCell.TryBuy(_resources))
                {
                    _cells.ApplyVisual(_boundCell);
                    if (AudioManager.Instance != null && AudioManager.Instance.zone1Buy != null)
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.zone1Buy);
                }
            });

            _cellCleanseBtn.onClick.RemoveAllListeners();
            _cellCleanseBtn.onClick.AddListener(() =>
            {
                if (_boundCell.TryCleanse(_resources))
                    _cells.ApplyVisual(_boundCell);
            });

            _cellAssistantBtn.onClick.RemoveAllListeners();
            if (_assistants != null)
            {
                var hasAssistant = _assistants.HasAssistantOnCell(_boundCell);
                var txt = _cellAssistantBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                    txt.text = hasAssistant ? "Quitar Asist." : "Asignar Asist.";
                _cellAssistantBtn.onClick.AddListener(() =>
                {
                    if (_assistants.HasAssistantOnCell(_boundCell))
                        _assistants.TryUnassignFromCell(_boundCell);
                    else
                        _assistants.TryAssignToCell(_boundCell);
                });
            }
        }

        void OpenCellPanel()
        {
            _cellPanel.SetActive(true);
            CloseSalesPanel();
        }

        void CloseCellPanel()
        {
            if (_cellPanel != null)
                _cellPanel.SetActive(false);
            _cells?.ClearSelection(notify: false);
            _boundCell = null;
        }

        void OpenSalesPanel()
        {
            _salesPanel.SetActive(true);
            RefreshSalesPanel();
        }

        void CloseSalesPanel()
        {
            if (_salesPanel != null)
                _salesPanel.SetActive(false);
        }

        void RefreshSalesPanel()
        {
            if (_salesPanel == null || !_salesPanel.activeSelf)
                return;
            if (_buyers == null || _resources == null)
                return;

            foreach (var row in _saleRows)
                Destroy(row);
            _saleRows.Clear();

            foreach (var buyer in _buyers.Buyers)
            {
                var row = CreateBuyerRow(_salesListRoot, buyer);
                _saleRows.Add(row);
            }
        }

        GameObject CreateBuyerRow(RectTransform parent, BuyerDefinition buyer)
        {
            var row = new GameObject($"BuyerRow_{buyer.buyerName}");
            row.transform.SetParent(parent, false);

            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childControlHeight = true;
            h.childControlWidth = false;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleLeft;

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 48f;
            le.flexibleWidth = 1f;

            var portrait = new GameObject("Portrait");
            portrait.transform.SetParent(row.transform, false);
            var portraitImg = portrait.AddComponent<Image>();
            portraitImg.color = Color.white;
            var portraitLE = portrait.AddComponent<LayoutElement>();
            portraitLE.preferredWidth = 40f;
            portraitLE.preferredHeight = 40f;
            var portraitPath = buyer.buyerName switch
            {
                "Los Profundos" => "Assets/Sprites/Zone1/Portraits/zone1_buyer_deepone_portrait.png",
                "Yekuvian" => "Assets/Sprites/Zone1/Portraits/zone1_buyer_yekuvian_portrait.png",
                "Ángeles Caídos" => "Assets/Sprites/Zone1/Portraits/zone1_buyer_fallenangel_portrait.png",
                _ => null
            };
            if (!string.IsNullOrEmpty(portraitPath))
            {
                var p = Zone1ArtProvider.LoadSprite(portraitPath);
                if (p != null)
                    portraitImg.sprite = p;
            }

            var currentPrice = _buyers != null ? _buyers.GetCurrentPrice(buyer) : buyer.basePricePerUnit;
            var left = CreateTMP(row.transform, $"{buyer.buyerName} · {ResourceLabel(buyer.buysResource)} · {currentPrice}/u", 14, TextAlignmentOptions.Left);
            var leftLE = left.gameObject.AddComponent<LayoutElement>();
            leftLE.preferredWidth = 420f;
            leftLE.minWidth = 360f;
            leftLE.flexibleWidth = 1f;
            left.textWrappingMode = TextWrappingModes.NoWrap;
            left.overflowMode = TextOverflowModes.Overflow;

            var available = _resources.Get(buyer.buysResource);
            var mid = CreateTMP(row.transform, $"Disp: {available}", 14, TextAlignmentOptions.Center);
            var midLE = mid.gameObject.AddComponent<LayoutElement>();
            midLE.preferredWidth = 95f;
            midLE.minWidth = 90f;

            var actions = new GameObject("Actions");
            actions.transform.SetParent(row.transform, false);
            var actionsLayout = actions.AddComponent<HorizontalLayoutGroup>();
            actionsLayout.spacing = 8f;
            actionsLayout.childControlHeight = true;
            actionsLayout.childControlWidth = true;
            actionsLayout.childForceExpandHeight = false;
            actionsLayout.childForceExpandWidth = false;
            var actionsLE = actions.AddComponent<LayoutElement>();
            actionsLE.preferredWidth = 200f;
            actionsLE.minWidth = 200f;

            var btn1 = CreateButton(actions.transform, "x1", 96f, 34f, 15);
            btn1.onClick.AddListener(() =>
            {
                if (_buyers.TrySell(buyer, 1))
                {
                    if (AudioManager.Instance != null && AudioManager.Instance.zone1Sell != null)
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.zone1Sell);
                    RefreshHUD();
                    RefreshSalesPanel();
                }
            });

            var btnAll = CreateButton(actions.transform, "MAX", 96f, 34f, 15);
            btnAll.onClick.AddListener(() =>
            {
                var amt = _resources.Get(buyer.buysResource);
                if (amt <= 0)
                    return;
                if (_buyers.TrySell(buyer, amt))
                {
                    if (AudioManager.Instance != null && AudioManager.Instance.zone1Sell != null)
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.zone1Sell);
                    RefreshHUD();
                    RefreshSalesPanel();
                }
            });

            // Keep intent clear: when there is no stock, selling buttons are visibly disabled.
            var hasStock = available > 0;
            btn1.interactable = hasStock;
            btnAll.interactable = hasStock;

            // Fade the whole row when buyer has no stock to sell.
            var rowCg = row.AddComponent<CanvasGroup>();
            rowCg.alpha = hasStock ? 1f : 0.55f;

            return row;
        }

        void OpenTaxPanel()
        {
            if (_taxPanel == null || _tax == null)
                return;
            _taxPanel.SetActive(true);

            if (AudioManager.Instance != null && AudioManager.Instance.zone1TaxAlert != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.zone1TaxAlert);

            RefreshTaxPanel();
        }

        void CloseTaxPanel()
        {
            if (_taxPanel != null)
                _taxPanel.SetActive(false);
        }

        void RefreshTaxPanel()
        {
            if (_taxPanel == null || _tax == null)
                return;
            _txtStrikes.text = $"Multas: {_tax.Strikes}/3";

            if (!_tax.IsAlertActive)
                return;

            _taxTitle.text = "El recaudador se aproxima…";
            var amount = _tax.CalculateTaxAmount();
            _taxBody.text =
                $"Recaudador: {_tax.CollectorName}\n" +
                $"Monto: {amount}\n" +
                $"Tiempo para pagar: {FormatTime(_tax.PayWindowRemainingSeconds)}\n\n" +
                $"Si no pagas:\n- Pierdes 75% del dinero\n- +1 multa\n- Puede corromper celdas\n- Game Over a 3 multas";

            _taxPayBtn.onClick.RemoveAllListeners();
            _taxPayBtn.onClick.AddListener(() =>
            {
                if (_tax.TryPay())
                {
                    if (AudioManager.Instance != null && AudioManager.Instance.zone1TaxPay != null)
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.zone1TaxPay);
                    CloseTaxPanel();
                    RefreshHUD();
                }
            });
        }

        void OnHoverChanged(FarmCell cell)
        {
            if (_hoverPanel == null)
                return;
            if (cell == null)
            {
                _hoverPanel.SetActive(false);
                return;
            }

            _hoverPanel.SetActive(true);
            var line1 = cell.DisplayName;
            var line2 = cell.State == CellState.Producing ? $"Produciendo ({FormatTime(cell.ProducingRemainingSeconds)})" : cell.State.ToString();
            var line3 = cell.IsCorrupted ? "Corrupta" : "";
            _hoverText.text = string.IsNullOrEmpty(line3) ? $"{line1}\n{line2}" : $"{line1}\n{line2}\n{line3}";
        }

        void BuildUI()
        {
            EnsureEventSystem();

            var root = GetOrCreateSingleUiRoot();

            // Rebuild runtime UI cleanly, even if editor placeholders exist.
            for (var i = root.transform.childCount - 1; i >= 0; i--)
            {
                var child = root.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            _canvas = root.GetComponent<Canvas>();
            if (_canvas == null)
                _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.6f;

            if (root.GetComponent<GraphicRaycaster>() == null)
                root.AddComponent<GraphicRaycaster>();

            // HUD
            var hud = new GameObject("HUDCanvas");
            hud.transform.SetParent(root.transform, false);
            var hudRt = hud.AddComponent<RectTransform>();
            hudRt.anchorMin = new Vector2(0, 1);
            hudRt.anchorMax = new Vector2(1, 1);
            hudRt.pivot = new Vector2(0.5f, 1f);
            hudRt.sizeDelta = new Vector2(0, 152);
            hudRt.anchoredPosition = new Vector2(0, -2);

            var hudBg = hud.AddComponent<Image>();
            hudBg.color = new Color(0.05f, 0.05f, 0.06f, 0.92f);
            var hudSprite = Zone1ArtProvider.LoadSprite("Assets/Sprites/Zone1/UI/zone1_ui_hud_bar.png");
            if (hudSprite != null)
            {
                hudBg.sprite = hudSprite;
                hudBg.type = Image.Type.Sliced;
                hudBg.color = Color.white;
            }

            var hudLayout = hud.AddComponent<HorizontalLayoutGroup>();
            hudLayout.padding = new RectOffset(18, 18, 12, 12);
            hudLayout.spacing = 16f;
            hudLayout.childAlignment = TextAnchor.MiddleLeft;
            hudLayout.childForceExpandHeight = true;
            hudLayout.childForceExpandWidth = false;

            var colLeft = CreateVerticalGroup(hud.transform, 390);
            _txtMoney = CreateHUDStatRow(colLeft, "Assets/Sprites/Zone1/Icons/zone1_icon_darkcoin.png", "Monedas oscuras: 0", 24);
            _txtWeakSouls = CreateHUDStatRow(colLeft, "Assets/Sprites/Zone1/Icons/zone1_icon_soulweak.png", "Almas débiles: 0", 20);
            _txtEnergy = CreateHUDStatRow(colLeft, "Assets/Sprites/Zone1/Icons/zone1_icon_pureenergy.png", "Energía pura: 0", 20);

            var colMid = CreateVerticalGroup(hud.transform, 260);
            _txtLevel = CreateHUDStatRow(colMid, "Assets/Sprites/Zone1/Icons/zone1_icon_level.png", "Nivel 1", 22);
            CreateTMP(colMid, "Experiencia", 16, TextAlignmentOptions.Left);
            _xpFill = CreateProgressBar(colMid, 230, 18);

            var colTax = CreateVerticalGroup(hud.transform, 300);
            _txtZoneLabel = CreateTMP(colTax, "Zona 1 - Calabozos", 22, TextAlignmentOptions.Left);
            _txtTaxTimer = CreateHUDStatRow(colTax, "Assets/Sprites/Zone1/Icons/zone1_icon_tax.png", "Impuesto: 00:00", 20);
            _txtStrikes = CreateHUDStatRow(colTax, "Assets/Sprites/Zone1/Icons/zone1_icon_alert.png", "Multas: 0/3", 20);
            _txtAssistants = CreateHUDStatRow(colTax, "Assets/Sprites/Zone1/Icons/zone1_icon_level.png", "Asistentes: 0/0", 18);

            var colBtns = CreateVerticalGroup(hud.transform, 240);
            _btnSales = CreateButton(colBtns, "Ventas", 210f, 42f);
            _btnBack = CreateButton(colBtns, "Volver a Zonas", 210f, 42f);
            var btnsLE = colBtns.gameObject.GetComponent<LayoutElement>();
            if (btnsLE != null)
            {
                btnsLE.minWidth = 200f;
                btnsLE.preferredWidth = 220f;
            }

            // Cell panel
            _cellPanel = CreatePanel(root.transform, "CellInfoPanel", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(390, 440));
            BuildCellPanel(_cellPanel.transform);
            _cellPanel.SetActive(false);

            // Sales panel
            _salesPanel = CreatePanel(root.transform, "SalesPanel", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(980, 560));
            BuildSalesPanel(_salesPanel.transform);
            _salesPanel.SetActive(false);

            // Tax alert
            _taxPanel = CreatePanel(root.transform, "TaxAlertPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(560, 390));
            BuildTaxPanel(_taxPanel.transform);
            _taxPanel.SetActive(false);

            // Hover
            _hoverPanel = CreatePanel(root.transform, "HoverInfoPanel", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(320, 80));
            _hoverText = CreateTMP(_hoverPanel.transform, "", 16, TextAlignmentOptions.Center);
            _hoverPanel.SetActive(false);

            var hint = CreatePanel(root.transform, "ActionHintPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(980f, 34f));
            _txtActionHint = CreateTMP(hint.transform, "Tip: Click en una celda para abrir panel y usar Producir / Recolectar / Mejorar.", 15, TextAlignmentOptions.Center);
            _txtActionHint.rectTransform.anchorMin = Vector2.zero;
            _txtActionHint.rectTransform.anchorMax = Vector2.one;
            _txtActionHint.rectTransform.offsetMin = new Vector2(8f, 2f);
            _txtActionHint.rectTransform.offsetMax = new Vector2(-8f, -2f);
            _txtActionHint.textWrappingMode = TextWrappingModes.NoWrap;
            _txtActionHint.overflowMode = TextOverflowModes.Overflow;
        }

        static GameObject GetOrCreateSingleUiRoot()
        {
            GameObject kept = null;
            var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in all)
            {
                if (t == null || t.parent != null)
                    continue;
                if (t.name != "UI")
                    continue;
                if (kept == null)
                {
                    kept = t.gameObject;
                    continue;
                }

                // Remove duplicates to avoid stacked invisible panels blocking input.
                if (Application.isPlaying)
                    Destroy(t.gameObject);
                else
                    DestroyImmediate(t.gameObject);
            }

            if (kept == null)
                kept = new GameObject("UI");
            return kept;
        }

        void BuildCellPanel(Transform root)
        {
            var v = root.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 12, 12, 12);
            v.spacing = 8f;
            v.childAlignment = TextAnchor.UpperLeft;

            _cellTitle = CreateTMP(root, "Celda", 20, TextAlignmentOptions.Left);
            _cellBody = CreateTMP(root, "-", 16, TextAlignmentOptions.Left);
            _cellBody.textWrappingMode = TextWrappingModes.Normal;

            var btnRow1 = CreateHorizontalGroup(root);
            _cellProduceBtn = CreateButton(btnRow1, "Producir");
            _cellCollectBtn = CreateButton(btnRow1, "Recolectar");

            var btnRow2 = CreateHorizontalGroup(root);
            _cellUpgradeBtn = CreateButton(btnRow2, "Mejorar");
            _cellBuyBtn = CreateButton(btnRow2, "Comprar");

            var btnRow3 = CreateHorizontalGroup(root);
            _cellCleanseBtn = CreateButton(btnRow3, "Limpiar");
            var close = CreateButton(btnRow3, "Cerrar");
            close.onClick.AddListener(() => CloseCellPanel());

            var btnRow4 = CreateHorizontalGroup(root);
            _cellAssistantBtn = CreateButton(btnRow4, "Asignar Asist.");
        }

        void BuildSalesPanel(Transform root)
        {
            var v = root.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 12, 12, 12);
            v.spacing = 8f;

            CreateTMP(root, "Ventas (compradores)", 20, TextAlignmentOptions.Left);
            var legend = CreateTMP(root, "x1 = vender 1 unidad   |   MAX = vender todo disponible", 14, TextAlignmentOptions.Left);
            legend.color = new Color(0.88f, 0.86f, 0.72f, 0.9f);

            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(root, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.sizeDelta = new Vector2(0, 390);
            var scrollLe = scrollGo.AddComponent<LayoutElement>();
            scrollLe.preferredHeight = 390f;
            scrollLe.flexibleHeight = 1f;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = new Color(0, 0, 0, 0.15f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            _salesListRoot = content.AddComponent<RectTransform>();
            _salesListRoot.anchorMin = new Vector2(0f, 1f);
            _salesListRoot.anchorMax = new Vector2(1f, 1f);
            _salesListRoot.pivot = new Vector2(0.5f, 1f);
            _salesListRoot.anchoredPosition = Vector2.zero;
            _salesListRoot.sizeDelta = new Vector2(0f, 0f);
            var contentV = content.AddComponent<VerticalLayoutGroup>();
            contentV.spacing = 6f;
            contentV.childControlHeight = true;
            contentV.childControlWidth = true;
            contentV.childForceExpandWidth = true;
            contentV.childForceExpandHeight = false;
            contentV.childAlignment = TextAnchor.UpperLeft;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = _salesListRoot;
            scroll.horizontal = false;

            var close = CreateButton(root, "Cerrar");
            close.onClick.AddListener(() => CloseSalesPanel());
        }

        void BuildTaxPanel(Transform root)
        {
            var v = root.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 12, 12, 12);
            v.spacing = 8f;

            _taxTitle = CreateTMP(root, "El recaudador se aproxima…", 22, TextAlignmentOptions.Left);
            var portraitGo = new GameObject("CollectorPortrait");
            portraitGo.transform.SetParent(root, false);
            _taxPortrait = portraitGo.AddComponent<Image>();
            var portraitLe = portraitGo.AddComponent<LayoutElement>();
            portraitLe.preferredWidth = 84f;
            portraitLe.preferredHeight = 84f;
            var taxPortraitSprite = Zone1ArtProvider.LoadSprite("Assets/Sprites/Zone1/Portraits/zone1_tax_cthulhu_portrait.png");
            if (taxPortraitSprite != null)
                _taxPortrait.sprite = taxPortraitSprite;
            _taxBody = CreateTMP(root, "-", 16, TextAlignmentOptions.Left);
            _taxBody.textWrappingMode = TextWrappingModes.Normal;
            _taxBody.rectTransform.sizeDelta = new Vector2(0, 220);

            var row = CreateHorizontalGroup(root);
            _taxPayBtn = CreateButton(row, "Pagar");
            var close = CreateButton(row, "Cerrar");
            close.onClick.AddListener(() => CloseTaxPanel());
        }

        static void EnsureEventSystem()
        {
            EventSystem es;
            if (EventSystem.current != null)
            {
                es = EventSystem.current;
            }
            else
            {
                var go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
            }
            var bridge = es.GetComponent<EventSystemInputModuleBridge>();
            if (bridge == null)
                bridge = es.gameObject.AddComponent<EventSystemInputModuleBridge>();
            bridge.EnsureCorrectInputModule();
        }

        static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, anchorMin.y == anchorMax.y ? anchorMin.y : 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.06f, 0.06f, 0.07f, 0.95f);

            var spritePath = name switch
            {
                "CellInfoPanel" => "Assets/Sprites/Zone1/UI/zone1_ui_panel_cell.png",
                "SalesPanel" => "Assets/Sprites/Zone1/UI/zone1_ui_panel_sales.png",
                "TaxAlertPanel" => "Assets/Sprites/Zone1/UI/zone1_ui_panel_tax.png",
                "HoverInfoPanel" => "Assets/Sprites/Zone1/UI/zone1_ui_panel_cell.png",
                _ => null
            };
            if (!string.IsNullOrEmpty(spritePath))
            {
                var panelSprite = Zone1ArtProvider.LoadSprite(spritePath);
                if (panelSprite != null)
                {
                    img.sprite = panelSprite;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
            }

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.8f, 0.75f, 0.45f, 0.35f);
            outline.effectDistance = new Vector2(1, -1);

            return go;
        }

        static Transform CreateVerticalGroup(Transform parent, float preferredWidth)
        {
            var go = new GameObject("Col");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(preferredWidth, 0);
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4f;
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = false;
            go.AddComponent<LayoutElement>().preferredWidth = preferredWidth;
            return go.transform;
        }

        static Transform CreateHorizontalGroup(Transform parent)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = false;
            return go.transform;
        }

        static TextMeshProUGUI CreateHUDStatRow(Transform parent, string iconPath, string text, int fontSize)
        {
            var row = new GameObject("HUDStatRow");
            row.transform.SetParent(parent, false);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlHeight = true;
            h.childControlWidth = false;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = false;
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = Mathf.Max(22f, fontSize + 6f);

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(row.transform, false);
            var icon = iconGo.AddComponent<Image>();
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 24f;
            iconLe.preferredHeight = 24f;
            var iconSprite = Zone1ArtProvider.LoadSprite(iconPath);
            if (iconSprite != null)
                icon.sprite = iconSprite;
            icon.color = Color.white;

            var label = CreateTMP(row.transform, text, fontSize, TextAlignmentOptions.Left);
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        static TextMeshProUGUI CreateTMP(Transform parent, string value, int fontSize, TextAlignmentOptions align, string label = null)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            if (fontSize >= 20)
                tmp.color = new Color(0.92f, 0.83f, 0.60f, 1f);      // Titles / key values
            else if (fontSize <= 14)
                tmp.color = new Color(0.77f, 0.79f, 0.85f, 1f);      // Secondary labels
            else
                tmp.color = new Color(0.93f, 0.93f, 0.93f, 1f);      // Body/value text
            tmp.text = label != null ? $"{label}{value}" : value;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.enableAutoSizing = false;
            return tmp;
        }

        static Image CreateProgressBar(Transform parent, float width, float height)
        {
            var bgGo = new GameObject("XPBar");
            bgGo.transform.SetParent(parent, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(width, height);
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.08f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(1, 1);
            fillRt.offsetMin = new Vector2(0, 0);
            fillRt.offsetMax = new Vector2(0, 0);
            var fill = fillGo.AddComponent<Image>();
            fill.color = new Color(0.8f, 0.75f, 0.45f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;

            return fill;
        }

        static Button CreateButton(Transform parent, string text, float preferredWidth = 180f, float preferredHeight = 34f, int fontSize = 16)
        {
            var go = new GameObject($"Button_{text}");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            var btnSprite = Zone1ArtProvider.LoadSprite("Assets/Sprites/Zone1/UI/zone1_ui_button_base.png");
            if (btnSprite != null)
            {
                img.sprite = btnSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.18f, 0.18f, 0.22f, 1f);
            colors.pressedColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            btn.colors = colors;

            var txt = CreateTMP(go.transform, text, fontSize, TextAlignmentOptions.Center);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero;
            txt.rectTransform.offsetMax = Vector2.zero;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.preferredWidth = preferredWidth;
            le.flexibleWidth = preferredWidth <= 130f ? 0f : 1f;

            // Optional: wire existing BasicUIAudio if AudioManager is present later.
            if (go.GetComponent<BasicUIAudio>() == null)
            {
                var uiAudio = go.AddComponent<BasicUIAudio>();
                if (AudioManager.Instance != null)
                {
                    uiAudio.hoverClip = AudioManager.Instance.uiHover;
                    uiAudio.clickClip = AudioManager.Instance.uiClick;
                    uiAudio.useAudioManagerFirst = true;
                }
            }

            return btn;
        }

        static string ResourceLabel(ResourceType type)
        {
            return type switch
            {
                ResourceType.WeakSouls => "Almas débiles",
                ResourceType.PureEnergy => "Energía pura",
                ResourceType.MemoryShards => "Frag. recuerdo",
                ResourceType.UnstableSouls => "Almas inestables",
                ResourceType.DarkCoins => "Monedas oscuras",
                _ => type.ToString()
            };
        }

        static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0, seconds);
            var s = Mathf.CeilToInt(seconds);
            var m = s / 60;
            var r = s % 60;
            return $"{m:00}:{r:00}";
        }
    }
}

