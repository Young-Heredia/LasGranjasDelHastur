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
        TextMeshProUGUI _txtMemoryShards;
        TextMeshProUGUI _txtUnstableSouls;
        TextMeshProUGUI _txtLevel;
        Image _xpFill;
        TextMeshProUGUI _txtTaxTimer;
        TextMeshProUGUI _txtStrikes;
        TextMeshProUGUI _txtAssistants;

        Button _btnSales;
        RectTransform _btnSalesRt;
        Vector3 _salesPulseBaseScale = Vector3.one;
        float _salesPulsePhase;
        Button _btnBack;
        Button _btnPayEarly;

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
        TextMeshProUGUI _salesAssistantsInfo;
        Button _salesBuyAssistantBtn;

        // Tax bindings
        TextMeshProUGUI _taxTitle;
        TextMeshProUGUI _taxBody;
        Button _taxPayBtn;
        Image _taxPortrait;
        TextMeshProUGUI _txtZoneLabel;

        // Hover bindings
        TextMeshProUGUI _hoverText;
        TextMeshProUGUI _txtActionHint;

        struct HudDeltaPopup
        {
            public TextMeshProUGUI tmp;
            public int accumDelta;
            public float remaining;
            public float maxRemaining;
        }

        RectTransform _rtRowMoney;
        RectTransform _rtRowWeakSouls;
        RectTransform _rtRowEnergy;
        RectTransform _rtRowMemory;
        RectTransform _rtRowUnstable;

        HudDeltaPopup _popMoney;
        HudDeltaPopup _popWeakSouls;
        HudDeltaPopup _popEnergy;
        HudDeltaPopup _popMemory;
        HudDeltaPopup _popUnstable;

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

            UpdateHudDeltaPopups(Time.unscaledDeltaTime);

            if (_tax != null && _txtTaxTimer != null)
                _txtTaxTimer.text = TmpSafeGlyphs($"Impuesto: {FormatTime(_tax.IsAlertActive ? _tax.PayWindowRemainingSeconds : _tax.TimeToNextTaxSeconds)}");

            UpdateSalesButtonAttentionPulse();
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
            if (_resources != null) _resources.ResourceChanged += OnResourceChanged;
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
                _tax.GameOverReached += OnTaxGameOverReached;
            }

            Zone1UIHoverBus.HoverChanged += OnHoverChanged;

            _btnSales.onClick.AddListener(() =>
            {
                if (_salesPanel.activeSelf) CloseSalesPanel();
                else OpenSalesPanel();
            });

            if (_btnPayEarly != null)
            {
                _btnPayEarly.onClick.AddListener(() =>
                {
                    if (_tax != null && _tax.TryOpenTaxAlertEarly())
                    {
                        RefreshHUD();
                        RefreshTaxPanel();
                    }
                    else
                    {
                        AudioManager.Instance?.PlayZoneLocked();
                    }
                });
            }

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
            if (_resources != null) _resources.ResourceChanged -= OnResourceChanged;
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
                _tax.GameOverReached -= OnTaxGameOverReached;
            }

            Zone1UIHoverBus.HoverChanged -= OnHoverChanged;
            _eventsWired = false;
        }

        void OnResourceChanged(ResourceType type, int newValue, int delta)
        {
            if (delta == 0)
                return;

            switch (type)
            {
                case ResourceType.DarkCoins:
                    EnsureHudRowRefs();
                    BumpHudPopup(ref _popMoney, _rtRowMoney, delta);
                    break;
                case ResourceType.WeakSouls:
                    EnsureHudRowRefs();
                    BumpHudPopup(ref _popWeakSouls, _rtRowWeakSouls, delta);
                    break;
                case ResourceType.PureEnergy:
                    EnsureHudRowRefs();
                    BumpHudPopup(ref _popEnergy, _rtRowEnergy, delta);
                    break;
                case ResourceType.MemoryShards:
                    EnsureHudRowRefs();
                    BumpHudPopup(ref _popMemory, _rtRowMemory, delta);
                    break;
                case ResourceType.UnstableSouls:
                    EnsureHudRowRefs();
                    BumpHudPopup(ref _popUnstable, _rtRowUnstable, delta);
                    break;
            }
        }

        void EnsureHudRowRefs()
        {
            if (_rtRowMoney == null && _txtMoney != null)
                _rtRowMoney = _txtMoney.transform.parent as RectTransform;
            if (_rtRowWeakSouls == null && _txtWeakSouls != null)
                _rtRowWeakSouls = _txtWeakSouls.transform.parent as RectTransform;
            if (_rtRowEnergy == null && _txtEnergy != null)
                _rtRowEnergy = _txtEnergy.transform.parent as RectTransform;
            if (_rtRowMemory == null && _txtMemoryShards != null)
                _rtRowMemory = _txtMemoryShards.transform.parent as RectTransform;
            if (_rtRowUnstable == null && _txtUnstableSouls != null)
                _rtRowUnstable = _txtUnstableSouls.transform.parent as RectTransform;
        }

        static void BumpHudPopup(ref HudDeltaPopup popup, RectTransform rowRt, int delta)
        {
            if (rowRt == null)
                return;

            if (popup.tmp == null)
                popup.tmp = CreateHudDeltaLabel(rowRt);

            popup.accumDelta += delta;

            var baseDuration = 1.0f;
            var bump = 0.18f;
            var maxDuration = 2.2f;
            popup.remaining = Mathf.Clamp(Mathf.Max(popup.remaining, baseDuration) + bump, 0f, maxDuration);
            popup.maxRemaining = Mathf.Max(popup.maxRemaining, popup.remaining);
            popup.tmp.gameObject.SetActive(true);
        }

        static TextMeshProUGUI CreateHudDeltaLabel(RectTransform rowRt)
        {
            var go = new GameObject("DeltaPopup");
            go.transform.SetParent(rowRt, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(10f, 0f);
            rt.sizeDelta = new Vector2(120f, 28f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.color = new Color(0.75f, 1f, 0.75f, 0f);
            return tmp;
        }

        void UpdateHudDeltaPopups(float dt)
        {
            UpdateHudPopup(ref _popMoney, dt);
            UpdateHudPopup(ref _popWeakSouls, dt);
            UpdateHudPopup(ref _popEnergy, dt);
            UpdateHudPopup(ref _popMemory, dt);
            UpdateHudPopup(ref _popUnstable, dt);
        }

        static void UpdateHudPopup(ref HudDeltaPopup popup, float dt)
        {
            if (popup.tmp == null)
                return;

            if (popup.remaining <= 0f || popup.accumDelta == 0)
            {
                popup.remaining = 0f;
                popup.maxRemaining = 0f;
                popup.accumDelta = 0;
                popup.tmp.text = "";
                popup.tmp.color = new Color(popup.tmp.color.r, popup.tmp.color.g, popup.tmp.color.b, 0f);
                popup.tmp.gameObject.SetActive(false);
                return;
            }

            popup.remaining -= dt;
            if (popup.remaining < 0f)
                popup.remaining = 0f;

            var sign = popup.accumDelta >= 0 ? "+" : "-";
            var abs = Mathf.Abs(popup.accumDelta);
            popup.tmp.text = TmpSafeGlyphs($"{sign}{abs}");
            var baseColor = popup.accumDelta >= 0
                ? new Color(0.66f, 1f, 0.66f, 1f)
                : new Color(1f, 0.62f, 0.62f, 1f);

            var denom = Mathf.Max(0.01f, popup.maxRemaining);
            var t = popup.remaining / denom;
            var alpha = Mathf.Clamp01(t);
            popup.tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
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

            EnsureHudRowRefs();
            _txtMoney.text = $"Monedas oscuras: {_resources.Get(ResourceType.DarkCoins)}";
            _txtWeakSouls.text = $"Almas débiles: {_resources.Get(ResourceType.WeakSouls)}";
            _txtEnergy.text = $"Energía pura: {_resources.Get(ResourceType.PureEnergy)}";
            if (_txtMemoryShards != null)
                _txtMemoryShards.text = $"Fragmentos de recuerdo: {_resources.Get(ResourceType.MemoryShards)}";
            if (_txtUnstableSouls != null)
                _txtUnstableSouls.text = $"Almas inestables: {_resources.Get(ResourceType.UnstableSouls)}";
            _txtLevel.text = $"Nivel {_progression.Level}";
            _xpFill.fillAmount = _progression.XpProgress01();
            if (_txtAssistants != null && _assistants != null)
                _txtAssistants.text = $"Asistentes: {_assistants.AvailableAssistants}/{_assistants.TotalAssistants}";

            if (_tax != null)
                _txtStrikes.text = $"Multas globales: {_tax.Strikes}/3";

            if (_btnPayEarly != null && _tax != null)
                _btnPayEarly.interactable = !_tax.IsAlertActive;
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

            _cellTitle.text = TmpSafeGlyphs(_boundCell.DisplayName);

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
            var cleanseCost = _boundCell.CleanseCostDarkCoinsPublic;
            var prodAmt = _boundCell.ProductionAmount;
            var prodSec = Mathf.Max(0.1f, _boundCell.ProductionSeconds);
            var collectXpPreview = Mathf.Max(2, prodAmt * 2);

            var purchaseLine = "Costo compra: — (solo en celdas bloqueadas)";
            var hasPurchasePreview = false;
            var previewBuyForBtn = 0;
            if (_boundCell.State == CellState.Blocked && _cells != null &&
                _cells.TryGetBlockedPurchasePreview(_boundCell, out _, out var previewBuy, out var previewName))
            {
                hasPurchasePreview = true;
                previewBuyForBtn = previewBuy;
                purchaseLine = $"Costo compra: {previewBuy} monedas -> {previewName}";
            }

            _cellBody.text = TmpSafeGlyphs(
                $"Nivel: {_boundCell.Level}\n" +
                $"Recurso: {resourceName}\n" +
                $"Tiempo: {Mathf.Max(0.1f, _boundCell.ProductionSeconds):0.0}s\n" +
                $"Por ciclo: {_boundCell.ProductionAmount}\n" +
                $"Almacén: {storageLine}{storageFull}\n" +
                $"Estado: {state}{prod}\n" +
                $"Corrupta: {corrupt}\n" +
                $"Asistente: {assistantCount} (máx 1)\n" +
                $"{purchaseLine}\n" +
                $"Producir: 0 monedas oscuras · obtienes +{prodAmt} {resourceName} al terminar (~{prodSec:0.#}s)\n" +
                $"Recolectar: 0 monedas oscuras · recibes +{prodAmt} {resourceName} y +{collectXpPreview} XP\n" +
                $"Mejora: {_boundCell.UpgradeCostDarkCoins} monedas\n" +
                $"Limpieza: {cleanseCost} monedas (si está corrupta)");

            _cellProduceBtn.interactable = _boundCell.CanProduce(_resources);
            _cellCollectBtn.interactable = _boundCell.CanCollect(_resources);
            _cellUpgradeBtn.interactable = _boundCell.CanUpgrade(_resources);
            _cellBuyBtn.interactable = _boundCell.State == CellState.Blocked
                ? (_cells != null && _cells.CanPurchaseBlockedSlot(_boundCell))
                : _boundCell.CanBuy(_resources);
            _cellCleanseBtn.interactable = _boundCell.CanCleanse(_resources);
            _cellAssistantBtn.interactable = _assistants != null &&
                (_assistants.HasAssistantOnCell(_boundCell) || _assistants.CanAssignToCell(_boundCell));

            SetButtonLabel(_cellProduceBtn, $"Producir (+{prodAmt})");
            SetButtonLabel(_cellCollectBtn, $"Recolectar (+{prodAmt})");
            SetButtonLabel(_cellUpgradeBtn, $"Mejorar ({_boundCell.UpgradeCostDarkCoins})");
            SetButtonLabel(
                _cellBuyBtn,
                _boundCell.State == CellState.Blocked
                    ? (hasPurchasePreview ? $"Comprar ({previewBuyForBtn})" : "Comprar (—)")
                    : "Comprar (—)");
            SetButtonLabel(_cellCleanseBtn, $"Limpiar ({cleanseCost})");

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
                if (_cells != null && _cells.TryPurchaseCell(_boundCell))
                {
                    _cells.ApplyVisual(_boundCell);
                    if (AudioManager.Instance != null && AudioManager.Instance.zone1Buy != null)
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.zone1Buy);
                    RefreshCellPanel();
                    RefreshHUD();
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
                    txt.text = TmpSafeGlyphs(hasAssistant ? "Quitar asistente" : "Asignar asistente");
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

            if (_salesAssistantsInfo != null && _assistants != null)
                _salesAssistantsInfo.text = TmpSafeGlyphs(
                    $"Compras\n" +
                    $"Asistentes disponibles: {_assistants.AvailableAssistants}/{_assistants.TotalAssistants}\n" +
                    $"Costo siguiente asistente: {_assistants.NextAssistantCost}");

            if (_salesBuyAssistantBtn != null && _assistants != null)
                _salesBuyAssistantBtn.interactable = _assistants.TotalAssistants < 30 &&
                    _resources != null &&
                    _resources.Get(ResourceType.DarkCoins) >= _assistants.NextAssistantCost;
        }

        GameObject CreateBuyerRow(RectTransform parent, BuyerDefinition buyer)
        {
            var row = new GameObject($"BuyerRow_{buyer.buyerName}");
            row.transform.SetParent(parent, false);

            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.padding = new RectOffset(2, 6, 2, 2);
            h.childControlHeight = true;
            h.childControlWidth = true;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleLeft;

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 68f;
            le.minHeight = 64f;
            le.flexibleWidth = 1f;

            var portrait = new GameObject("Portrait");
            portrait.transform.SetParent(row.transform, false);
            var portraitImg = portrait.AddComponent<Image>();
            portraitImg.color = Color.white;
            portraitImg.preserveAspect = true;
            var portraitLE = portrait.AddComponent<LayoutElement>();
            portraitLE.preferredWidth = 64f;
            portraitLE.preferredHeight = 64f;
            var portraitPath = buyer.buyerName switch
            {
                "Los Profundos" => Zone1UiSpritePaths.BuyerDeepOnePortrait,
                "Yekuvian" or "Yithianos" => Zone1UiSpritePaths.BuyerYekuvianPortrait,
                "Custodios del Eco" => Zone1UiSpritePaths.AssistantHoundTindalosPortrait,
                "Ángeles Caídos" => Zone1UiSpritePaths.BuyerFallenAngelPortrait,
                _ => null
            };
            if (!string.IsNullOrEmpty(portraitPath))
            {
                var p = Zone1ArtProvider.LoadSprite(portraitPath);
                if (p != null)
                    portraitImg.sprite = p;
            }

            var currentPrice = _buyers != null ? _buyers.GetCurrentPrice(buyer) : buyer.basePricePerUnit;
            var left = CreateTMP(row.transform, $"{buyer.buyerName} · {ResourceLabel(buyer.buysResource)} · {currentPrice}/u", 15, TextAlignmentOptions.Left);
            var leftLE = left.gameObject.AddComponent<LayoutElement>();
            leftLE.preferredWidth = 220f;
            leftLE.minWidth = 120f;
            leftLE.flexibleWidth = 1f;
            left.textWrappingMode = TextWrappingModes.Normal;
            left.overflowMode = TextOverflowModes.Ellipsis;

            var available = _resources.Get(buyer.buysResource);
            var mid = CreateTMP(row.transform, $"Disp: {available}", 15, TextAlignmentOptions.Right);
            var midLE = mid.gameObject.AddComponent<LayoutElement>();
            midLE.preferredWidth = 124f;
            midLE.minWidth = 118f;
            midLE.flexibleWidth = 0f;

            var actions = new GameObject("Actions");
            actions.transform.SetParent(row.transform, false);
            var actionsLayout = actions.AddComponent<HorizontalLayoutGroup>();
            actionsLayout.spacing = 10f;
            actionsLayout.childControlHeight = true;
            actionsLayout.childControlWidth = true;
            actionsLayout.childForceExpandHeight = false;
            actionsLayout.childForceExpandWidth = false;
            var actionsLE = actions.AddComponent<LayoutElement>();
            actionsLE.preferredWidth = 224f;
            actionsLE.minWidth = 218f;
            actionsLE.flexibleWidth = 0f;

            var btn1 = CreateButton(actions.transform, "x1", 108f, 38f, 16);
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

            var btnAll = CreateButton(actions.transform, "MAX", 108f, 38f, 16);
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

            // Cerrar sin pagar durante una alerta activa cuenta como impago (+multa, etc.).
            if (_tax != null && _tax.IsAlertActive)
                _tax.RefuseTaxPayment();
        }

        void RefreshTaxPanel()
        {
            if (_taxPanel == null || _tax == null)
                return;
            _txtStrikes.text = $"Multas globales: {_tax.Strikes}/3";

            if (_btnPayEarly != null)
                _btnPayEarly.interactable = !_tax.IsAlertActive;

            if (!_tax.IsAlertActive)
                return;

            _taxTitle.text = "El recaudador se aproxima…";
            var amount = _tax.CalculateTaxAmount();
            var pension = _tax.CalculateLatePaymentPension();
            var debt = _tax.FineDebt;
            var extras = "";
            if (debt > 0)
                extras += $"\n• Deuda acumulada: {debt}";
            if (pension > 0)
                extras += $"\n• Pensión pago tardío (multas): +{pension}";

            _taxBody.text = TmpSafeGlyphs(
                $"Recaudador: {_tax.CollectorName}\n" +
                $"Monto total: {amount}{extras}\n" +
                $"Tiempo para pagar: {FormatTime(_tax.PayWindowRemainingSeconds)}\n\n" +
                $"Si no pagas:\n- Pierdes 75% del dinero\n- +1 multa\n- Puede corromper celdas\n- Game Over a 3 multas");

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
            _hoverText.text = TmpSafeGlyphs(string.IsNullOrEmpty(line3) ? $"{line1}\n{line2}" : $"{line1}\n{line2}\n{line3}");
        }

        void OnTaxGameOverReached()
        {
            AudioManager.Instance?.PlayZone1StrikeGain();
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
            // Altura suficiente para columna Ventas + Pago + Volver sin recorte vertical.
            hudRt.sizeDelta = new Vector2(0, 252);
            hudRt.anchoredPosition = new Vector2(0, -2);

            var hudBg = hud.AddComponent<Image>();
            hudBg.color = new Color(0.05f, 0.05f, 0.06f, 0.92f);
            var hudSprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_hud_bar.png");
            if (hudSprite != null)
            {
                hudBg.sprite = hudSprite;
                hudBg.type = Image.Type.Sliced;
                hudBg.color = Color.white;
            }

            hud.AddComponent<RectMask2D>();

            var hudLayout = hud.AddComponent<HorizontalLayoutGroup>();
            hudLayout.padding = new RectOffset(14, 22, 12, 12);
            hudLayout.spacing = 12f;
            hudLayout.childAlignment = TextAnchor.MiddleLeft;
            hudLayout.childForceExpandHeight = true;
            hudLayout.childForceExpandWidth = false;

            var resBlock = new GameObject("HudResources");
            resBlock.transform.SetParent(hud.transform, false);
            var resBlockImg = resBlock.AddComponent<Image>();
            var resPanelSprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_panel_cell.png");
            if (resPanelSprite != null)
            {
                resBlockImg.sprite = resPanelSprite;
                resBlockImg.type = Image.Type.Sliced;
                resBlockImg.color = new Color(0.72f, 0.70f, 0.78f, 0.42f);
            }
            else
                resBlockImg.color = new Color(0.07f, 0.07f, 0.09f, 0.88f);

            var resBlockLe = resBlock.AddComponent<LayoutElement>();
            resBlockLe.preferredWidth = 548f;
            resBlockLe.flexibleWidth = 1f;
            resBlockLe.minWidth = 420f;

            var resBlockH = resBlock.AddComponent<HorizontalLayoutGroup>();
            resBlockH.padding = new RectOffset(10, 10, 6, 6);
            resBlockH.spacing = 10f;
            resBlockH.childAlignment = TextAnchor.MiddleLeft;
            resBlockH.childControlHeight = true;
            resBlockH.childControlWidth = true;
            resBlockH.childForceExpandHeight = true;
            resBlockH.childForceExpandWidth = false;

            var colResA = CreateVerticalGroup(resBlock.transform, 262);
            var colResB = CreateVerticalGroup(resBlock.transform, 262);
            _txtMoney = CreateHUDStatRow(colResA, Zone1UiSpritePaths.IconDarkCoin, "Monedas oscuras: 0", 22, 30f);
            _txtWeakSouls = CreateHUDStatRow(colResA, Zone1UiSpritePaths.IconSoulWeak, "Almas débiles: 0", 18, 28f);
            _txtEnergy = CreateHUDStatRow(colResA, Zone1UiSpritePaths.IconPureEnergy, "Energía pura: 0", 18, 28f);
            _txtMemoryShards = CreateHUDStatRow(colResB, Zone1UiSpritePaths.IconMemoryShard, "Fragmentos de recuerdo: 0", 17, 28f);
            _txtUnstableSouls = CreateHUDStatRow(colResB, Zone1UiSpritePaths.IconUnstableSoul, "Almas inestables: 0", 17, 28f);

            var colMid = CreateVerticalGroup(hud.transform, 212);
            _txtLevel = CreateHUDStatRow(colMid, Zone1UiSpritePaths.IconLevel, "Nivel 1", 22, 30f);
            CreateHUDStatRow(colMid, Zone1UiSpritePaths.IconXp, "Experiencia", 16, 26f);
            _xpFill = CreateProgressBar(colMid, 204, 20);

            var colTax = CreateVerticalGroup(hud.transform, 248);
            _txtZoneLabel = CreateHUDStatRow(colTax, Zone1UiSpritePaths.IconTax, "Zona 1 - Calabozos", 22, 28f);
            _txtTaxTimer = CreateHUDStatRow(colTax, Zone1UiSpritePaths.IconTime, "Impuesto: 00:00", 20, 28f);
            _txtStrikes = CreateHUDStatRow(colTax, Zone1UiSpritePaths.IconAlert, "Multas: 0/3", 20, 28f);
            _txtAssistants = CreateHUDStatRow(colTax, Zone1UiSpritePaths.AssistantHoundTindalosPortrait, "Asistentes: 0/0", 18, 38f, true);

            var hudSpacer = new GameObject("HudSpacer");
            hudSpacer.transform.SetParent(hud.transform, false);
            var hudSpacerLe = hudSpacer.AddComponent<LayoutElement>();
            hudSpacerLe.flexibleWidth = 1f;
            hudSpacerLe.minWidth = 4f;

            var colBtns = CreateVerticalGroup(hud.transform, 136);
            if (colBtns.TryGetComponent<VerticalLayoutGroup>(out var colBtnsV))
            {
                colBtnsV.spacing = 8f;
                colBtnsV.childForceExpandWidth = true;
                colBtnsV.childControlWidth = true;
            }

            _btnSales = CreateButton(colBtns, "Ventas", 122f, 118f, 21, layoutFlexibleWidth: false);
            _btnSalesRt = _btnSales != null ? _btnSales.GetComponent<RectTransform>() : null;
            _salesPulseBaseScale = _btnSalesRt != null ? _btnSalesRt.localScale : Vector3.one;
            _btnPayEarly = CreateButton(colBtns, "Pago adelantado", 126f, 38f, 15, layoutFlexibleWidth: false);
            _btnBack = CreateButton(colBtns, "Volver a Zonas", 126f, 40f, 15, layoutFlexibleWidth: false);
            var btnsLE = colBtns.gameObject.GetComponent<LayoutElement>();
            if (btnsLE != null)
            {
                btnsLE.minWidth = 128f;
                btnsLE.preferredWidth = 136f;
                btnsLE.flexibleWidth = 0f;
            }

            // Cell panel
            _cellPanel = CreatePanel(root.transform, "CellInfoPanel", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(628, 736));
            BuildCellPanel(_cellPanel.transform);
            _cellPanel.SetActive(false);

            // Sales panel
            _salesPanel = CreatePanel(root.transform, "SalesPanel", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(1040, 620));
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
            v.padding = new RectOffset(24, 24, 18, 20);
            v.spacing = 12f;
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            _cellTitle = CreateTMP(root, "Celda", 20, TextAlignmentOptions.Left, null, TextWrappingModes.Normal, TextOverflowModes.Overflow);
            _cellTitle.margin = new Vector4(4f, 2f, 12f, 2f);
            var titleLe = _cellTitle.gameObject.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1f;
            titleLe.minHeight = 26f;
            titleLe.preferredHeight = 32f;

            _cellBody = CreateTMP(root, "-", 15, TextAlignmentOptions.TopLeft, null, TextWrappingModes.Normal, TextOverflowModes.Overflow);
            _cellBody.margin = new Vector4(8f, 4f, 16f, 6f);
            _cellBody.lineSpacing = 20f;
            _cellBody.paragraphSpacing = 2f;
            var bodyLe = _cellBody.gameObject.AddComponent<LayoutElement>();
            bodyLe.flexibleWidth = 1f;
            bodyLe.flexibleHeight = 0f;
            bodyLe.minHeight = 180f;
            var bodySize = _cellBody.gameObject.AddComponent<ContentSizeFitter>();
            bodySize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            bodySize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var btnRow1 = CreateHorizontalGroup(root);
            ConfigureCellButtonRow(btnRow1);
            _cellProduceBtn = CreateIconButton(btnRow1, Zone1UiSpritePaths.IconProduce, "Producir", 268f, 56f, 15, true, 30f);
            _cellCollectBtn = CreateIconButton(btnRow1, Zone1UiSpritePaths.IconCollect, "Recolectar", 268f, 56f, 15, true, 30f);

            var btnRow2 = CreateHorizontalGroup(root);
            ConfigureCellButtonRow(btnRow2);
            _cellUpgradeBtn = CreateIconButton(btnRow2, Zone1UiSpritePaths.IconUpgrade, "Mejorar", 268f, 56f, 15, true, 30f);
            _cellBuyBtn = CreateIconButton(btnRow2, Zone1UiSpritePaths.IconBuy, "Comprar", 268f, 56f, 15, true, 30f);

            var btnRow3 = CreateHorizontalGroup(root);
            ConfigureCellButtonRow(btnRow3);
            _cellCleanseBtn = CreateIconButton(btnRow3, Zone1UiSpritePaths.IconCorruption, "Limpiar", 268f, 56f, 15, true, 30f);
            var close = CreateButton(btnRow3, "Cerrar", 268f, 56f, 15);
            close.onClick.AddListener(() => CloseCellPanel());

            var btnRow4 = CreateHorizontalGroup(root);
            ConfigureCellButtonRow(btnRow4);
            _cellAssistantBtn = CreateIconButton(btnRow4, Zone1UiSpritePaths.AssistantHoundTindalosPortrait, "Asignar asistente", 560f, 60f, 15, true, 38f, boostPortraitVisibility: true);
        }

        static void ConfigureCellButtonRow(Transform row)
        {
            if (!row.TryGetComponent<HorizontalLayoutGroup>(out var h))
                return;
            h.spacing = 12f;
            h.padding = new RectOffset(4, 4, 4, 4);
            h.childForceExpandWidth = true;
            h.childControlWidth = true;
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
            scrollRt.sizeDelta = new Vector2(0, 468);
            var scrollLe = scrollGo.AddComponent<LayoutElement>();
            scrollLe.preferredHeight = 468f;
            scrollLe.flexibleHeight = 1f;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = new Color(0, 0, 0, 0.08f);
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
            contentV.spacing = 10f;
            contentV.childControlHeight = true;
            contentV.childControlWidth = true;
            contentV.childForceExpandWidth = true;
            contentV.childForceExpandHeight = false;
            contentV.childAlignment = TextAnchor.UpperLeft;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = _salesListRoot;
            scroll.horizontal = false;

            var purchases = new GameObject("PurchasesSection");
            purchases.transform.SetParent(root, false);
            var purchasesLe = purchases.AddComponent<LayoutElement>();
            purchasesLe.preferredHeight = 112f;
            purchasesLe.minHeight = 108f;
            var purchasesV = purchases.AddComponent<VerticalLayoutGroup>();
            purchasesV.spacing = 6f;
            purchasesV.childAlignment = TextAnchor.UpperLeft;
            purchasesV.childControlHeight = true;
            purchasesV.childControlWidth = true;
            purchasesV.childForceExpandHeight = false;
            purchasesV.childForceExpandWidth = false;
            _salesAssistantsInfo = CreateTMP(purchases.transform, "Compras", 15, TextAlignmentOptions.Left);
            _salesAssistantsInfo.textWrappingMode = TextWrappingModes.Normal;

            var assistantShopRow = CreateHorizontalGroup(purchases.transform);
            if (assistantShopRow.TryGetComponent<HorizontalLayoutGroup>(out var asstRowLayout))
                asstRowLayout.childForceExpandWidth = false;
            var assistantPortraitGo = new GameObject("AssistantShopPortrait");
            assistantPortraitGo.transform.SetParent(assistantShopRow, false);
            var assistantPortraitImg = assistantPortraitGo.AddComponent<Image>();
            assistantPortraitImg.color = Color.white;
            assistantPortraitImg.preserveAspect = true;
            var assistantPortraitLe = assistantPortraitGo.AddComponent<LayoutElement>();
            assistantPortraitLe.preferredWidth = 72f;
            assistantPortraitLe.preferredHeight = 72f;
            var assistantShopSpr = Zone1ArtProvider.LoadSprite(Zone1UiSpritePaths.AssistantHoundTindalosPortrait);
            if (assistantShopSpr != null)
                assistantPortraitImg.sprite = assistantShopSpr;

            _salesBuyAssistantBtn = CreateButton(assistantShopRow, "Comprar Asistente", 272f, 40f, 16);
            _salesBuyAssistantBtn.onClick.AddListener(() =>
            {
                if (_assistants == null)
                    return;
                if (_assistants.TryBuyAssistant())
                {
                    if (AudioManager.Instance != null && AudioManager.Instance.zone1AssistantAssign != null)
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.zone1AssistantAssign);
                    RefreshHUD();
                    RefreshSalesPanel();
                }
            });

            var close = CreateButton(root, "Cerrar");
            close.onClick.AddListener(() => CloseSalesPanel());
        }

        void BuildTaxPanel(Transform root)
        {
            var v = root.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 12, 12, 12);
            v.spacing = 8f;

            _taxTitle = CreateTMP(root, "El recaudador se aproxima…", 22, TextAlignmentOptions.Left);

            var taxIconsRow = CreateHorizontalGroup(root);
            if (taxIconsRow.TryGetComponent<HorizontalLayoutGroup>(out var taxIconLayout))
                taxIconLayout.childForceExpandWidth = false;
            var portraitGo = new GameObject("CollectorPortrait");
            portraitGo.transform.SetParent(taxIconsRow, false);
            _taxPortrait = portraitGo.AddComponent<Image>();
            _taxPortrait.preserveAspect = true;
            var portraitLe = portraitGo.AddComponent<LayoutElement>();
            portraitLe.preferredWidth = 92f;
            portraitLe.preferredHeight = 92f;
            var taxPortraitSprite = Zone1ArtProvider.LoadSprite(Zone1UiSpritePaths.TaxCthulhuPortrait);
            if (taxPortraitSprite != null)
                _taxPortrait.sprite = taxPortraitSprite;

            var sealGo = new GameObject("CollectorSeal");
            sealGo.transform.SetParent(taxIconsRow, false);
            var sealImg = sealGo.AddComponent<Image>();
            sealImg.color = Color.white;
            sealImg.preserveAspect = true;
            var sealLe = sealGo.AddComponent<LayoutElement>();
            sealLe.preferredWidth = 80f;
            sealLe.preferredHeight = 80f;
            var sealSprite = Zone1ArtProvider.LoadSprite(Zone1UiSpritePaths.TaxCthulhuSeal);
            if (sealSprite != null)
                sealImg.sprite = sealSprite;
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
                "CellInfoPanel" => "Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_panel_cell.png",
                "SalesPanel" => "Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_panel_sales.png",
                "TaxAlertPanel" => "Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_panel_tax.png",
                "HoverInfoPanel" => "Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_panel_cell.png",
                "ActionHintPanel" => "Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_panel_cell.png",
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
            outline.effectColor = new Color(0.8f, 0.75f, 0.45f, 0.28f);
            outline.effectDistance = new Vector2(0.75f, -0.75f);

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

        static TextMeshProUGUI CreateHUDStatRow(Transform parent, string iconPath, string text, int fontSize, float iconSide = 30f, bool preserveIconAspect = false)
        {
            var row = new GameObject("HUDStatRow");
            row.transform.SetParent(parent, false);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12f;
            h.padding = new RectOffset(2, 10, 2, 2);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlHeight = true;
            h.childControlWidth = false;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = false;
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = Mathf.Max(26f, Mathf.Max(fontSize + 10f, iconSide + 6f));

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(row.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = preserveIconAspect;
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = iconSide;
            iconLe.preferredHeight = iconSide;
            iconLe.flexibleWidth = 0f;
            var iconSprite = Zone1ArtProvider.LoadSprite(iconPath);
            if (iconSprite != null)
                icon.sprite = iconSprite;
            icon.color = Color.white;

            var label = CreateTMP(row.transform, text, fontSize, TextAlignmentOptions.Left, null, TextWrappingModes.Normal, TextOverflowModes.Overflow);
            label.lineSpacing = 2f;
            label.margin = new Vector4(2f, 0f, 4f, 0f);
            var labelLe = label.gameObject.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;
            labelLe.minWidth = 60f;
            return label;
        }

        static TextMeshProUGUI CreateTMP(
            Transform parent,
            string value,
            int fontSize,
            TextAlignmentOptions align,
            string label = null,
            TextWrappingModes wrapMode = TextWrappingModes.NoWrap,
            TextOverflowModes overflowMode = TextOverflowModes.Ellipsis)
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
            tmp.text = TmpSafeGlyphs(label != null ? $"{label}{value}" : value);
            tmp.raycastTarget = false;
            tmp.textWrappingMode = wrapMode;
            tmp.overflowMode = overflowMode;
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

        void UpdateSalesButtonAttentionPulse()
        {
            if (_btnSalesRt == null)
                return;
            if (!ShouldPulseSalesButton())
            {
                _btnSalesRt.localScale = _salesPulseBaseScale;
                return;
            }

            _salesPulsePhase += Time.unscaledDeltaTime * Mathf.PI * 1.15f;
            var t = (Mathf.Sin(_salesPulsePhase) + 1f) * 0.5f;
            var scale = Mathf.Lerp(0.94f, 1.08f, t);
            _btnSalesRt.localScale = _salesPulseBaseScale * scale;
        }

        bool ShouldPulseSalesButton()
        {
            if (_resources == null)
                return false;

            if (HasAnySellableStock())
                return true;

            if (_cellPanel != null && _cellPanel.activeSelf && _boundCell != null)
            {
                if (_boundCell.State == CellState.Blocked && _cells != null &&
                    _cells.TryGetBlockedPurchasePreview(_boundCell, out _, out var buyCost, out _) &&
                    _resources.Get(ResourceType.DarkCoins) < buyCost)
                    return true;

                if (!_boundCell.IsCorrupted &&
                    (_boundCell.State == CellState.Available ||
                     _boundCell.State == CellState.ReadyToCollect ||
                     _boundCell.State == CellState.Producing) &&
                    _resources.Get(ResourceType.DarkCoins) < _boundCell.UpgradeCostDarkCoins)
                    return true;
            }

            return false;
        }

        bool HasAnySellableStock()
        {
            if (_buyers == null || _resources == null)
                return false;
            var buyers = _buyers.Buyers;
            if (buyers == null)
                return false;
            for (var i = 0; i < buyers.Count; i++)
            {
                var b = buyers[i];
                if (b == null)
                    continue;
                if (_resources.Get(b.buysResource) > 0)
                    return true;
            }
            return false;
        }

        static Button CreateIconButton(Transform parent, string iconPath, string text, float preferredWidth = 180f, float preferredHeight = 34f, int fontSize = 16, bool layoutFlexibleWidth = true, float iconSide = 28f, bool boostPortraitVisibility = false)
        {
            var go = new GameObject($"Button_{text}");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            var btnSprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_button_base.png");
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

            var row = new GameObject("LabelRow");
            row.transform.SetParent(go.transform, false);
            var rowRt = row.AddComponent<RectTransform>();
            rowRt.anchorMin = Vector2.zero;
            rowRt.anchorMax = Vector2.one;
            rowRt.offsetMin = new Vector2(12f, 5f);
            rowRt.offsetMax = new Vector2(-12f, -5f);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12f;
            h.padding = new RectOffset(2, 4, 0, 0);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlHeight = true;
            h.childControlWidth = false;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = false;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(row.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = iconSide;
            iconLe.preferredHeight = iconSide;
            iconLe.flexibleWidth = 0f;
            var iconSp = Zone1ArtProvider.LoadSprite(iconPath);
            if (iconSp != null)
                iconImg.sprite = iconSp;
            if (boostPortraitVisibility)
            {
                var io = iconGo.AddComponent<Outline>();
                io.effectColor = new Color(0.72f, 0.68f, 0.52f, 0.55f);
                io.effectDistance = new Vector2(0.6f, -0.6f);
            }

            var txt = CreateTMP(row.transform, text, fontSize, TextAlignmentOptions.Center, null, TextWrappingModes.Normal, TextOverflowModes.Overflow);
            txt.lineSpacing = 0f;
            var txtLe = txt.gameObject.AddComponent<LayoutElement>();
            txtLe.flexibleWidth = 1f;
            txtLe.minWidth = 40f;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.preferredWidth = preferredWidth;
            le.flexibleWidth = layoutFlexibleWidth && preferredWidth > 130f ? 1f : 0f;
            le.minHeight = preferredHeight;

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

        static Button CreateButton(Transform parent, string text, float preferredWidth = 180f, float preferredHeight = 34f, int fontSize = 16, bool layoutFlexibleWidth = true)
        {
            var go = new GameObject($"Button_{text}");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            var btnSprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_button_base.png");
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

            var txt = CreateTMP(go.transform, text, fontSize, TextAlignmentOptions.Center, null, TextWrappingModes.Normal, TextOverflowModes.Overflow);
            txt.lineSpacing = -2f;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero;
            txt.rectTransform.offsetMax = Vector2.zero;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight = preferredHeight;
            le.preferredWidth = preferredWidth;
            le.flexibleWidth = layoutFlexibleWidth && preferredWidth > 130f ? 1f : 0f;

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

        static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
                return;
            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = TmpSafeGlyphs(text);
        }

        /// <summary>
        /// NewRocker-Regular SDF (y similares) suelen no incluir flechas Unicode; TMPro spamea warnings si aparecen en texto dinámico.
        /// </summary>
        static string TmpSafeGlyphs(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return s
                .Replace("\u2192", "->")
                .Replace("\u2190", "<-")
                .Replace("\u2194", "<->");
        }

        static string ResourceLabel(ResourceType type)
        {
            return type switch
            {
                ResourceType.WeakSouls => "Almas débiles",
                ResourceType.PureEnergy => "Energía pura",
                ResourceType.MemoryShards => "Fragmentos de recuerdo",
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

