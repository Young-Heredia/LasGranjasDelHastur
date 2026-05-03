using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone3.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone3
{
    [DisallowMultipleComponent]
    public partial class Zone3PrototypeGame : MonoBehaviour
    {
        [Header("Economy")]
        [SerializeField] bool sharedEconomyWithZone1 = true;

        const string PrestigeKey = "LasGranjas_Zone3_PrestigePoints";
        const int MaxAssistants = 30;

        class Zone3CellRuntime
        {
            public int id;
            public string displayName;
            public bool producesResidue;
            public bool unlocked;
            public bool producing;
            public bool ready;
            public bool corrupted;
            public float remainingSeconds;
            public int level = 1;
        }

        int _darkCoins = 320;
        int _astralResidue;
        int _voidInk;
        int _difficultyTier = 1;
        int _totalSold;
        int _prestigePoints;
        int _assistantsTotal = 1;
        int _assistantBuyCost = 130;
        int _nextCellCost = 200;
        int _recentProductionForTax;
        int _sharedLevel = 1;
        int _sharedXp;

        float _taxTimer = 480f;
        float _taxInterval = 480f;
        float _automationTick;
        float _runtime;
        bool _endNarrativeShown;

        readonly List<Zone3CellRuntime> _cells = new();
        readonly List<int> _assistantAssignedCellId = new();

        Zone3CellRuntime _selectedCell;
        Zone3CellManager _worldCells;

        Transform _z3TaxFxCanvasRoot;
        CanvasGroup _z3TaxFxRoot;
        Image _z3TaxFxPortrait;
        Image _z3TaxFxStrip;
        Coroutine _z3TaxFxRoutine;

        void Awake()
        {
            Zone3RuntimeScaffold.EnsureSceneScaffold();
            AudioManager.EnsureInstance();
            _worldCells = FindFirstObjectByType<Zone3CellManager>();
            if (_worldCells != null)
                _worldCells.SelectedSlotChanged += OnWorldSlotSelected;
            _prestigePoints = PlayerPrefs.GetInt(PrestigeKey, 0);
            BuildDefaultCells();
            EnsureAssistantCount(_assistantsTotal);
            if (sharedEconomyWithZone1)
                PullSharedProgressFromZone1Save();
            TryRestoreFromSaveIfRequested();
            if (sharedEconomyWithZone1)
                PushSharedProgressToZone1Save();
            BuildUi();
            RefreshUi();
            SyncWorldCellVisuals();
        }

        void Update()
        {
            _runtime += Time.deltaTime;
            if (_runtime >= 60f * _difficultyTier && _difficultyTier < 5)
                _difficultyTier++;

            _taxTimer -= Time.deltaTime;
            if (_taxTimer <= 0f)
            {
                ResolveTaxCycle();
                _taxTimer = _taxInterval;
            }

            UpdateCells(Time.deltaTime);
            RunAssistantAutomation(Time.deltaTime);
            RefreshUi();
            if (Time.frameCount % 12 == 0)
                SyncWorldCellVisuals();

            if (!_endNarrativeShown && _difficultyTier >= 3 && _darkCoins >= 1200 && _totalSold >= 120)
                ShowEndNarrative();
        }

        void OnDisable()
        {
            if (_worldCells != null)
                _worldCells.SelectedSlotChanged -= OnWorldSlotSelected;
            PushSharedProgressToZone1Save();
        }

        void OnWorldSlotSelected(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _cells.Count)
            {
                _selectedCell = null;
                RefreshSelectedCellPanel();
                return;
            }
            _selectedCell = _cells[slotIndex];
            RefreshSelectedCellPanel();
        }

        void SyncWorldCellVisuals()
        {
            if (_worldCells == null)
                return;

            for (var i = 0; i < _cells.Count; i++)
            {
                var c = _cells[i];
                var spritePath = GetCellSpritePath(c);
                var tint = c.corrupted
                    ? new Color(0.85f, 0.58f, 0.98f, 1f)
                    : c.ready
                        ? new Color(0.88f, 0.92f, 1f, 1f)
                        : c.unlocked
                            ? Color.white
                            : new Color(0.55f, 0.55f, 0.60f, 1f);

                _worldCells.ApplyVisual(i, spritePath, tint);
                _worldCells.SetSlotColliderEnabled(i, true);
            }
        }

        static string GetCellSpritePath(Zone3CellRuntime cell)
        {
            const string root = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/";
            if (cell == null)
                return root + "Cells/Base/Cell_Empty.png";

            return cell.displayName switch
            {
                "Huerto Lunar" => root + "Cells/Zone3/Zone3_Cell_LunarOrchard.png",
                "Molino de Cometas" => root + "Cells/Zone3/Zone3_Cell_CometMill.png",
                "Núcleo Planetario" => root + "Cells/Zone3/Zone3_Cell_PlanetaryCore.png",
                "Incubadora Estelar" => root + "Cells/Zone3/Zone3_Cell_StarIncubator.png",
                _ => root + "Cells/Base/Cell_Empty.png",
            };
        }

        void SellResidue()
        {
            if (_astralResidue <= 0)
                return;
            var amount = _astralResidue;
            _astralResidue = 0;
            _darkCoins += amount * (4 + _difficultyTier);
            _totalSold += amount;
            _recentProductionForTax += amount;
            GrantSharedXp(Mathf.Max(1, amount / 3));
            PushSharedProgressToZone1Save();
            AudioManager.Instance?.PlayZone3Sell();
        }

        void SellInk()
        {
            if (_voidInk <= 0)
                return;
            var amount = _voidInk;
            _voidInk = 0;
            _darkCoins += amount * (7 + _difficultyTier * 2);
            _totalSold += amount;
            _recentProductionForTax += amount;
            GrantSharedXp(Mathf.Max(1, amount / 2));
            PushSharedProgressToZone1Save();
            AudioManager.Instance?.PlayZone3Sell();
        }

        void ResolveTaxCycle()
        {
            var purchasedCells = CountPurchasedCellsForTax();
            var rate = 0.20f + _difficultyTier * 0.03f;
            var activity = Mathf.Clamp(0.25f + purchasedCells * 0.12f, 0.25f, 2.0f);
            var taxAmount = Mathf.CeilToInt((_darkCoins * rate + _recentProductionForTax * 0.18f) * activity);
            _recentProductionForTax = 0;

            if (_darkCoins >= taxAmount)
            {
                _darkCoins -= taxAmount;
                _txtHint.text = $"Impuesto celestial pagado: -{taxAmount} monedas.";
                AudioManager.Instance?.PlayZone3TaxPay();
                PlayZone3TaxCollectorPresentation(shortFlash: true);
                PushSharedProgressToZone1Save();
                return;
            }

            GlobalTaxLedger.RegisterStrikeFailure(GameOverOrigin.Celestial);
            var strikeCount = GlobalTaxLedger.GetStrikes();
            _darkCoins = Mathf.FloorToInt(_darkCoins * 0.25f);
            TryCorruptRandomUnlockedCell();
            _txtHint.text = $"No alcanzó para pagar. Multa {strikeCount}/3 y corrupción aplicada.";
            AudioManager.Instance?.PlayZone3TaxAlert();
            PlayZone3TaxCollectorPresentation(shortFlash: false);
            PushSharedProgressToZone1Save();
        }

        int CountPurchasedCellsForTax()
        {
            var n = 0;
            for (var i = 0; i < _cells.Count; i++)
            {
                var c = _cells[i];
                if (c != null && c.unlocked)
                    n++;
            }
            return n;
        }

        void ShowEndNarrative()
        {
            _endNarrativeShown = true;
            if (_endPanel != null)
                _endPanel.SetActive(true);
            AudioManager.Instance?.PlayZone3EndNarrative();
        }

        void ApplyPrestige()
        {
            _prestigePoints++;
            PlayerPrefs.SetInt(PrestigeKey, _prestigePoints);
            PlayerPrefs.Save();

            _darkCoins = 280 + _prestigePoints * 40;
            _astralResidue = 0;
            _voidInk = 0;
            _difficultyTier = 1;
            _taxTimer = _taxInterval;
            _runtime = 0f;
            _endNarrativeShown = false;
            _totalSold = 0;
            _recentProductionForTax = 0;
            if (_endPanel != null)
                _endPanel.SetActive(false);
            _txtHint.text = $"Prestigio aplicado. Puntos totales: {_prestigePoints}.";
            PushSharedProgressToZone1Save();
            AudioManager.Instance?.PlayZone3Prestige();
        }

        void PullSharedProgressFromZone1Save()
        {
            if (!sharedEconomyWithZone1 || SaveManager.Instance == null || SaveManager.Instance.CachedData == null)
                return;

            var zone1 = EnsureZone1SharedDataContainer();
            _darkCoins = Mathf.Max(0, zone1.darkCoins);
            _sharedLevel = Mathf.Max(1, zone1.level);
            _sharedXp = Mathf.Max(0, zone1.xp);
        }

        void PushSharedProgressToZone1Save()
        {
            if (!sharedEconomyWithZone1 || SaveManager.Instance == null || SaveManager.Instance.CachedData == null)
                return;

            var zone1 = EnsureZone1SharedDataContainer();
            zone1.darkCoins = Mathf.Max(0, _darkCoins);
            zone1.level = Mathf.Max(1, _sharedLevel);
            zone1.xp = Mathf.Max(0, _sharedXp);
            SaveManager.Instance.CachedData.zone1Available = true;
        }

        void TryRestoreFromSaveIfRequested()
        {
            if (SaveManager.Instance == null || !SaveManager.Instance.ShouldRestoreFromSave)
                return;

            var data = SaveManager.Instance.CachedData;
            if (data != null && data.zone3 != null && data.zone3.valid)
                ApplySaveData(data.zone3);

            SaveManager.Instance.MarkRestoreConsumed();
        }

        public Zone3SaveData CaptureSaveData()
        {
            return new Zone3SaveData
            {
                valid = true,
                darkCoins = _darkCoins,
                astralResidue = _astralResidue,
                voidInk = _voidInk,
                difficultyTier = _difficultyTier,
                totalSold = _totalSold,
                taxTimer = _taxTimer,
                runtimeSeconds = _runtime,
                prestigePoints = _prestigePoints,
                endNarrativeShown = _endNarrativeShown,
                strikes = GlobalTaxLedger.GetStrikes(),
                assistantsTotal = _assistantsTotal,
                nextCellCost = _nextCellCost,
                assistants = CaptureAssistantsSaveData(),
                cells = CaptureCellsSaveData(),
            };
        }

        public void ApplySaveData(Zone3SaveData data)
        {
            if (data == null || !data.valid)
                return;

            _darkCoins = Mathf.Max(0, data.darkCoins);
            _astralResidue = Mathf.Max(0, data.astralResidue);
            _voidInk = Mathf.Max(0, data.voidInk);
            _difficultyTier = Mathf.Clamp(data.difficultyTier, 1, 9);
            _totalSold = Mathf.Max(0, data.totalSold);
            _taxTimer = Mathf.Max(0f, data.taxTimer);
            _runtime = Mathf.Max(0f, data.runtimeSeconds);
            _prestigePoints = Mathf.Max(0, data.prestigePoints);
            _endNarrativeShown = data.endNarrativeShown;
            _nextCellCost = Mathf.Max(90, data.nextCellCost <= 0 ? _nextCellCost : data.nextCellCost);
            _assistantsTotal = Mathf.Clamp(data.assistantsTotal <= 0 ? _assistantsTotal : data.assistantsTotal, 1, MaxAssistants);
            EnsureAssistantCount(_assistantsTotal);
            ApplyAssistantsSaveData(data.assistants);
            ApplyCellsSaveData(data.cells);
            RebuildCellsListUi();
            PlayerPrefs.SetInt(PrestigeKey, _prestigePoints);
            PlayerPrefs.Save();
        }

        void BuildDefaultCells()
        {
            _cells.Clear();
            var names = new[]
            {
                ("Huerto Lunar", true),
                ("Molino de Cometas", false),
                ("Núcleo Planetario", true),
                ("Incubadora Estelar", false),
            };

            for (var i = 0; i < 12; i++)
            {
                var template = names[i % names.Length];
                _cells.Add(new Zone3CellRuntime
                {
                    id = i,
                    displayName = template.Item1,
                    producesResidue = template.Item2,
                    unlocked = i < 2,
                });
            }

            _selectedCell = _cells.Count > 0 ? _cells[0] : null;
        }

        void UpdateCells(float deltaTime)
        {
            foreach (var cell in _cells)
            {
                if (!cell.unlocked || !cell.producing)
                    continue;

                cell.remainingSeconds = Mathf.Max(0f, cell.remainingSeconds - deltaTime);
                if (cell.remainingSeconds > 0f)
                    continue;

                cell.producing = false;
                cell.ready = true;
            }
        }

        void RunAssistantAutomation(float deltaTime)
        {
            _automationTick += deltaTime;
            if (_automationTick < 0.35f)
                return;
            _automationTick = 0f;

            foreach (var cell in _cells)
            {
                var assigned = GetAssignedAssistantsOnCell(cell.id);
                if (assigned <= 0 || !cell.unlocked || cell.corrupted)
                    continue;

                if (cell.ready)
                    CollectCell(cell);
                else if (!cell.producing)
                    StartCellProduction(cell);
            }
        }

        void StartSelectedCellProduction() => StartCellProduction(_selectedCell);
        void CollectSelectedCell() => CollectCell(_selectedCell);

        void UpgradeSelectedCell()
        {
            if (_selectedCell == null || !_selectedCell.unlocked)
                return;
            var cost = 70 * _selectedCell.level;
            if (_darkCoins < cost)
                return;

            _darkCoins -= cost;
            _selectedCell.level++;
            AudioManager.Instance?.PlayZone3TierUp();
        }

        void ToggleAssistantOnSelectedCell()
        {
            if (_selectedCell == null || !_selectedCell.unlocked)
                return;

            for (var i = 0; i < _assistantAssignedCellId.Count; i++)
            {
                if (_assistantAssignedCellId[i] == _selectedCell.id)
                {
                    _assistantAssignedCellId[i] = -1;
                    return;
                }
            }

            for (var i = 0; i < _assistantAssignedCellId.Count; i++)
            {
                if (_assistantAssignedCellId[i] >= 0)
                    continue;
                _assistantAssignedCellId[i] = _selectedCell.id;
                return;
            }
        }

        void BuyAssistant()
        {
            if (_assistantsTotal >= MaxAssistants || _darkCoins < _assistantBuyCost)
                return;

            _darkCoins -= _assistantBuyCost;
            _assistantsTotal++;
            _assistantBuyCost = Mathf.Min(9999, _assistantBuyCost + 55);
            EnsureAssistantCount(_assistantsTotal);
            GrantSharedXp(2);
            PushSharedProgressToZone1Save();
            AudioManager.Instance?.PlayZone3Action();
        }

        void BuyNextCell()
        {
            if (_selectedCell == null || _selectedCell.unlocked)
                return;
            if (_darkCoins < _nextCellCost)
                return;

            _darkCoins -= _nextCellCost;
            _selectedCell.unlocked = true;
            _selectedCell.level = 1;
            _selectedCell.corrupted = false;
            _nextCellCost += 90;
            SyncWorldCellVisuals();
            RebuildCellsListUi();
            GrantSharedXp(3);
            PushSharedProgressToZone1Save();
            AudioManager.Instance?.PlayZone3Action();
        }

        void StartCellProduction(Zone3CellRuntime cell)
        {
            if (cell == null || !cell.unlocked || cell.producing || cell.ready || cell.corrupted)
                return;

            var assistantBoost = Mathf.Clamp(GetAssignedAssistantsOnCell(cell.id), 0, 4) * 0.28f;
            var speed = 1f + (_difficultyTier - 1) * 0.12f + assistantBoost;
            cell.remainingSeconds = Mathf.Max(1.1f, (7.2f - cell.level * 0.45f) / speed);
            cell.producing = true;
            AudioManager.Instance?.PlayZone3Action();
        }

        void CollectCell(Zone3CellRuntime cell)
        {
            if (cell == null || !cell.ready)
                return;

            cell.ready = false;
            var amount = 4 + cell.level + GetAssignedAssistantsOnCell(cell.id);
            if (cell.producesResidue)
            {
                _astralResidue += amount;
                AudioManager.Instance?.PlayZone3ExtractResidue();
            }
            else
            {
                _voidInk += amount;
                AudioManager.Instance?.PlayZone3CondenseInk();
            }
            GrantSharedXp(1 + Mathf.Max(0, cell.level - 1));
        }

        void TryCorruptRandomUnlockedCell()
        {
            var candidates = new List<Zone3CellRuntime>();
            foreach (var cell in _cells)
            {
                if (cell.unlocked && !cell.corrupted)
                    candidates.Add(cell);
            }

            if (candidates.Count == 0)
                return;

            var index = Random.Range(0, candidates.Count);
            candidates[index].corrupted = true;
            candidates[index].producing = false;
            candidates[index].ready = false;
            AudioManager.Instance?.PlayZone3CosmicWarning();
        }

        int CountAssignedAssistants()
        {
            var count = 0;
            for (var i = 0; i < _assistantAssignedCellId.Count; i++)
            {
                if (_assistantAssignedCellId[i] >= 0)
                    count++;
            }
            return count;
        }

        int GetAssignedAssistantsOnCell(int cellId)
        {
            var count = 0;
            for (var i = 0; i < _assistantAssignedCellId.Count; i++)
            {
                if (_assistantAssignedCellId[i] == cellId)
                    count++;
            }
            return count;
        }

        void EnsureAssistantCount(int total)
        {
            total = Mathf.Clamp(total, 1, MaxAssistants);
            while (_assistantAssignedCellId.Count < total)
                _assistantAssignedCellId.Add(-1);
            while (_assistantAssignedCellId.Count > total)
                _assistantAssignedCellId.RemoveAt(_assistantAssignedCellId.Count - 1);
        }

        void GrantSharedXp(int amount)
        {
            amount = Mathf.Max(0, amount);
            _sharedXp += amount;
            while (_sharedXp >= GetXpToNextLevel(_sharedLevel))
            {
                _sharedXp -= GetXpToNextLevel(_sharedLevel);
                _sharedLevel++;
            }
        }

        static int GetXpToNextLevel(int level)
        {
            return Mathf.Max(30, 50 + Mathf.Max(0, level - 1) * 25);
        }

        Zone1SaveData EnsureZone1SharedDataContainer()
        {
            var data = SaveManager.Instance.CachedData;
            data.zone1 ??= new Zone1SaveData();
            var zone1 = data.zone1;
            if (!zone1.valid)
            {
                zone1.valid = true;
                zone1.darkCoins = Mathf.Max(0, _darkCoins);
                zone1.level = Mathf.Max(1, _sharedLevel);
                zone1.xp = Mathf.Max(0, _sharedXp);
            }
            return zone1;
        }

        List<AssistantSaveData> CaptureAssistantsSaveData()
        {
            var data = new List<AssistantSaveData>(_assistantAssignedCellId.Count);
            for (var i = 0; i < _assistantAssignedCellId.Count; i++)
            {
                data.Add(new AssistantSaveData
                {
                    assistantId = i,
                    assignedSlotIndex = _assistantAssignedCellId[i],
                });
            }
            return data;
        }

        void ApplyAssistantsSaveData(List<AssistantSaveData> data)
        {
            for (var i = 0; i < _assistantAssignedCellId.Count; i++)
                _assistantAssignedCellId[i] = -1;

            if (data == null)
                return;

            foreach (var saved in data)
            {
                if (saved == null || saved.assistantId < 0 || saved.assistantId >= _assistantAssignedCellId.Count)
                    continue;
                _assistantAssignedCellId[saved.assistantId] = saved.assignedSlotIndex;
            }
        }

        List<Zone3CellSaveData> CaptureCellsSaveData()
        {
            var data = new List<Zone3CellSaveData>(_cells.Count);
            foreach (var cell in _cells)
            {
                data.Add(new Zone3CellSaveData
                {
                    cellId = cell.id,
                    displayName = cell.displayName,
                    unlocked = cell.unlocked,
                    level = cell.level,
                    producing = cell.producing,
                    ready = cell.ready,
                    corrupted = cell.corrupted,
                    remainingSeconds = cell.remainingSeconds,
                    assignedAssistants = GetAssignedAssistantsOnCell(cell.id),
                });
            }
            return data;
        }

        void ApplyCellsSaveData(List<Zone3CellSaveData> data)
        {
            if (data == null || data.Count == 0)
                return;

            foreach (var saved in data)
            {
                if (saved == null)
                    continue;
                var cell = _cells.Find(c => c.id == saved.cellId);
                if (cell == null)
                    continue;
                cell.unlocked = saved.unlocked;
                cell.level = Mathf.Max(1, saved.level);
                cell.producing = saved.producing;
                cell.ready = saved.ready;
                cell.corrupted = saved.corrupted;
                cell.remainingSeconds = Mathf.Max(0f, saved.remainingSeconds);
            }
        }

    }
}
