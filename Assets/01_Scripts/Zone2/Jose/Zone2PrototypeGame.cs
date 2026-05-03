using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone2.Jose.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone2.Jose
{
    [DisallowMultipleComponent]
    public partial class Zone2PrototypeGame : MonoBehaviour
    {
        public const int Zone2GridCellCount = 30;
        const int MaxAssistants = 30;

        class Zone2CellRuntime
        {
            public int id;
            public string displayName;
            public bool producesSupplies;
            public bool unlocked;
            public bool producing;
            public bool ready;
            public bool corrupted;
            public float remainingSeconds;
            public int level = 1;
        }

        struct Zone2AssistantState
        {
            public int cellId; // -1 = sin asignar
            public Zone2DistrictType type;
            public int level; // 1..Z2RosterMaxAssistantLevel
        }

        public const int Z2RosterMaxAssistantLevel = 10;
        int _reassignAssistantIndex = -1; // reasignar: tocar celda

        [Header("Economy")]
        [SerializeField] private bool sharedEconomyWithZone1 = true;
        [Tooltip("Zona 2: cobro cada 10 minutos.")]
        [SerializeField, Min(1f)] private float taxIntervalSeconds = 600f;
        [SerializeField, Min(1)] private int initialUnlockedCells = 2;

        int _darkCoins = 260;
        int _citySupplies;
        int _arcaneBlueprints;
        int _difficultyTier = 1;
        int _totalSold;
        int _nextCellCost = 120;
        int _assistantBuyCost = 90;
        int _assistantsTotal = 1;
        int _recentProductionForTax;
        int _sharedLevel = 1;
        int _sharedXp;
        float _taxTimer;
        float _automationTick;
        float _runtime;

        readonly List<Zone2CellRuntime> _cells = new();
        readonly List<Zone2AssistantState> _zone2Assistants = new();

        Zone2CellRuntime _selectedCell;
        Zone2CellManager _worldCells;

        Transform _z2TaxFxCanvasRoot;
        CanvasGroup _z2TaxFxRoot;
        Image _z2TaxFxPortrait;
        Image _z2TaxFxStrip;
        Coroutine _z2TaxFxRoutine;

        void Awake()
        {
            Zone2RuntimeScaffold.EnsureSceneScaffold();
            AudioManager.EnsureInstance();
            _worldCells = FindFirstObjectByType<Zone2CellManager>();
            if (_worldCells != null)
                _worldCells.SelectedSlotChanged += OnWorldSlotSelected;
            _taxTimer = taxIntervalSeconds;
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
            if (_runtime >= 70f * _difficultyTier && _difficultyTier < 5)
                _difficultyTier++;

            _taxTimer -= Time.deltaTime;
            if (_taxTimer <= 0f)
            {
                ResolveTaxCycle();
                _taxTimer = taxIntervalSeconds;
            }

            UpdateCells(Time.deltaTime);
            RunAssistantAutomation(Time.deltaTime);
            RefreshUi();
            if (Time.frameCount % 12 == 0)
                SyncWorldCellVisuals();
        }

        void OnDisable()
        {
            if (_worldCells != null)
                _worldCells.SelectedSlotChanged -= OnWorldSlotSelected;
            PushSharedProgressToZone1Save();
        }

        void OnWorldSlotSelected(int slotIndex)
        {
            if (TryCompleteReassignToCellIndex(slotIndex))
            {
                if (slotIndex >= 0 && slotIndex < _cells.Count)
                    _selectedCell = _cells[slotIndex];
                RefreshSelectedCellPanel();
                return;
            }
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
                var district = (Zone2DistrictType)(c.id % 4);
                _worldCells.ApplyDistrictVisual(
                    i,
                    district,
                    GetZone2VisualState(c),
                    Zone2CellLevelRules.ClampLevel(c.level),
                    c.unlocked);
                // Como Zona 1: celdas bloqueadas también se pueden seleccionar (para comprar).
                _worldCells.SetSlotColliderEnabled(i, true);
            }
        }

        static Zone2CellVisualState GetZone2VisualState(Zone2CellRuntime c)
        {
            if (c == null)
                return Zone2CellVisualState.Idle;
            if (!c.unlocked)
                return Zone2CellVisualState.Locked;
            if (c.corrupted)
                return Zone2CellVisualState.Corrupted;
            if (c.ready)
                return Zone2CellVisualState.Ready;
            if (c.producing)
                return Zone2CellVisualState.Producing;
            return Zone2CellVisualState.Idle;
        }

        void SellSupplies()
        {
            if (_citySupplies <= 0)
                return;
            var amount = _citySupplies;
            _citySupplies = 0;
            _darkCoins += amount * (5 + _difficultyTier);
            _totalSold += amount;
            _recentProductionForTax += amount;
            GrantSharedXp(Mathf.Max(1, amount / 3));
            PushSharedProgressToZone1Save();
            AudioManager.Instance?.PlayZone2Sell();
        }

        void SellBlueprints()
        {
            if (_arcaneBlueprints <= 0)
                return;
            var amount = _arcaneBlueprints;
            _arcaneBlueprints = 0;
            _darkCoins += amount * (8 + _difficultyTier);
            _totalSold += amount;
            _recentProductionForTax += amount;
            GrantSharedXp(Mathf.Max(1, amount / 2));
            PushSharedProgressToZone1Save();
            AudioManager.Instance?.PlayZone2Sell();
        }

        void ResolveTaxCycle()
        {
            var rate = 0.16f + _difficultyTier * 0.02f;
            var tax = Mathf.CeilToInt(_darkCoins * rate + _recentProductionForTax * 0.12f);
            _recentProductionForTax = 0;

            if (_darkCoins >= tax)
            {
                _darkCoins -= tax;
                _txtHint.text = $"Impuesto urbano pagado: -{tax} monedas.";
                AudioManager.Instance?.PlayZone2TaxPay();
                PlayZone2TaxCollectorPresentation(shortFlash: true);
                PushSharedProgressToZone1Save();
                return;
            }

            GlobalTaxLedger.RegisterStrikeFailure(GameOverOrigin.CondensedCities);
            var strikeCount = GlobalTaxLedger.GetStrikes();
            _darkCoins = Mathf.FloorToInt(_darkCoins * 0.25f);
            TryCorruptRandomUnlockedCell();
            _txtHint.text = $"No alcanzó para pagar. Multa {strikeCount}/3 y corrupción aplicada.";
            AudioManager.Instance?.PlayZone2TaxAlert();
            PlayZone2TaxCollectorPresentation(shortFlash: false);
            PushSharedProgressToZone1Save();
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
            if (data != null && data.zone2 != null && data.zone2.valid)
                ApplySaveData(data.zone2);

            SaveManager.Instance.MarkRestoreConsumed();
        }

        public Zone2SaveData CaptureSaveData()
        {
            return new Zone2SaveData
            {
                valid = true,
                darkCoins = _darkCoins,
                citySupplies = _citySupplies,
                arcaneBlueprints = _arcaneBlueprints,
                difficultyTier = _difficultyTier,
                totalSold = _totalSold,
                taxTimer = _taxTimer,
                runtimeSeconds = _runtime,
                strikes = GlobalTaxLedger.GetStrikes(),
                assistantsTotal = _assistantsTotal,
                nextCellCost = _nextCellCost,
                assistants = CaptureAssistantsSaveData(),
                cells = CaptureCellsSaveData(),
            };
        }

        public void ApplySaveData(Zone2SaveData data)
        {
            if (data == null || !data.valid)
                return;

            _darkCoins = Mathf.Max(0, data.darkCoins);
            _citySupplies = Mathf.Max(0, data.citySupplies);
            _arcaneBlueprints = Mathf.Max(0, data.arcaneBlueprints);
            _difficultyTier = Mathf.Clamp(data.difficultyTier, 1, 9);
            _totalSold = Mathf.Max(0, data.totalSold);
            _taxTimer = Mathf.Max(0f, data.taxTimer);
            _runtime = Mathf.Max(0f, data.runtimeSeconds);
            _nextCellCost = Mathf.Max(60, data.nextCellCost <= 0 ? _nextCellCost : data.nextCellCost);
            _assistantsTotal = Mathf.Clamp(data.assistantsTotal <= 0 ? _assistantsTotal : data.assistantsTotal, 1, MaxAssistants);
            EnsureAssistantCount(_assistantsTotal);
            ApplyAssistantsSaveData(data.assistants);
            ApplyCellsSaveData(data.cells);
            RebuildCellsListUi();
        }

        void BuildDefaultCells()
        {
            _cells.Clear();
            // Cuatro arquetipos (Huerto Lunar, Molino, Núcleo, Incubadora): id % 4 → sprite y nombre.
            // Cuatro arquetipos de producción: alterna suministros / planos.
            var supplyPattern = new[] { true, false, true, false, true, true, false, false, true, false };

            for (var i = 0; i < Zone2GridCellCount; i++)
            {
                var district = (Zone2DistrictType)(i % 4);
                _cells.Add(new Zone2CellRuntime
                {
                    id = i,
                    displayName = $"{Zone2DistrictPaths.GetDisplayName(district)} · {i + 1:00}",
                    producesSupplies = supplyPattern[i % supplyPattern.Length],
                    unlocked = i < initialUnlockedCells,
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
            if (!Zone2CellLevelRules.CanUpgrade(_selectedCell.level))
                return;
            var cost = Zone2CellLevelRules.NextUpgradeCost(_selectedCell.level);
            if (cost <= 0 || _darkCoins < cost)
                return;

            _darkCoins -= cost;
            _selectedCell.level = Zone2CellLevelRules.ClampLevel(_selectedCell.level + 1);
            SyncWorldCellVisuals();
            PushSharedProgressToZone1Save();
            AudioManager.Instance?.PlayZone2TierUp();
        }

        void ToggleAssistantOnSelectedCell()
        {
            _reassignAssistantIndex = -1;
            if (_selectedCell == null || !_selectedCell.unlocked)
                return;

            for (var i = 0; i < _zone2Assistants.Count; i++)
            {
                if (_zone2Assistants[i].cellId == _selectedCell.id)
                {
                    var a = _zone2Assistants[i];
                    a.cellId = -1;
                    _zone2Assistants[i] = a;
                    return;
                }
            }

            for (var i = 0; i < _zone2Assistants.Count; i++)
            {
                if (_zone2Assistants[i].cellId >= 0)
                    continue;
                var a = _zone2Assistants[i];
                a.cellId = _selectedCell.id;
                _zone2Assistants[i] = a;
                return;
            }
        }

        void BuyAssistant()
        {
            if (_assistantsTotal >= MaxAssistants || _darkCoins < _assistantBuyCost)
                return;

            _darkCoins -= _assistantBuyCost;
            _assistantsTotal++;
            _assistantBuyCost = Mathf.Min(9999, _assistantBuyCost + 35);
            EnsureAssistantCount(_assistantsTotal);
            GrantSharedXp(2);
            PushSharedProgressToZone1Save();
            AudioManager.Instance?.PlayZone2Action();
        }

        void BuyNextCell()
        {
            if (_selectedCell == null || _selectedCell.unlocked)
                return;
            if (_darkCoins < _nextCellCost)
                return;

            _darkCoins -= _nextCellCost;
            _selectedCell.unlocked = true;
            _selectedCell.level = Zone2CellLevelRules.MinLevel;
            _selectedCell.corrupted = false;
            _nextCellCost += 55;
            SyncWorldCellVisuals();
            RebuildCellsListUi();
            GrantSharedXp(3);
            PushSharedProgressToZone1Save();
            AudioManager.Instance?.PlayZone2Action();
        }

        void StartCellProduction(Zone2CellRuntime cell)
        {
            if (cell == null || !cell.unlocked || cell.producing || cell.ready || cell.corrupted)
                return;

            var n = GetAssignedAssistantsOnCell(cell.id);
            var sumLv = GetAssistantLevelSumOnCell(cell.id);
            var levelExtra = Mathf.Max(0, sumLv - n);
            var assistantBoost = Mathf.Clamp(n * 0.25f + levelExtra * 0.04f, 0f, 1.2f);
            var speed = 1f + (_difficultyTier - 1) * 0.1f + assistantBoost;
            var baseT = Zone2CellLevelRules.BaseProductionSeconds(Zone2CellLevelRules.ClampLevel(cell.level));
            cell.remainingSeconds = Mathf.Max(0.4f, baseT / speed);
            cell.producing = true;
            AudioManager.Instance?.PlayZone2Action();
        }

        void CollectCell(Zone2CellRuntime cell)
        {
            if (cell == null || !cell.ready)
                return;

            cell.ready = false;
            var amount = Zone2CellLevelRules.CollectAmount(
                Zone2CellLevelRules.ClampLevel(cell.level),
                GetAssignedAssistantsOnCell(cell.id),
                GetAssistantLevelSumOnCell(cell.id));
            if (cell.producesSupplies)
            {
                _citySupplies += amount;
                AudioManager.Instance?.PlayZone2ProduceSupplies();
            }
            else
            {
                _arcaneBlueprints += amount;
                AudioManager.Instance?.PlayZone2ProduceBlueprints();
            }
            GrantSharedXp(1 + Mathf.Max(0, cell.level - 1));
        }

        void TryCorruptRandomUnlockedCell()
        {
            var candidates = new List<Zone2CellRuntime>();
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
        }

        int CountAssignedAssistants()
        {
            var count = 0;
            for (var i = 0; i < _zone2Assistants.Count; i++)
            {
                if (_zone2Assistants[i].cellId >= 0)
                    count++;
            }
            return count;
        }

        int GetAssignedAssistantsOnCell(int cellId)
        {
            var count = 0;
            for (var i = 0; i < _zone2Assistants.Count; i++)
            {
                if (_zone2Assistants[i].cellId == cellId)
                    count++;
            }
            return count;
        }

        int GetAssistantLevelSumOnCell(int cellId)
        {
            var sum = 0;
            for (var i = 0; i < _zone2Assistants.Count; i++)
            {
                if (_zone2Assistants[i].cellId == cellId)
                    sum += Mathf.Max(1, _zone2Assistants[i].level);
            }
            return sum;
        }

        int NextAssistantUpgradeCost(int currentLevel) =>
            30 + Mathf.Max(0, currentLevel) * 20;

        string Z2NameForCellId(int cellId)
        {
            if (cellId < 0 || cellId >= _cells.Count)
                return "—";
            var c = _cells[cellId];
            return c == null ? "—" : c.displayName;
        }

        void UpgradeZone2AssistantByIndex(int index)
        {
            if (index < 0 || index >= _zone2Assistants.Count)
                return;
            var a = _zone2Assistants[index];
            if (a.level >= Z2RosterMaxAssistantLevel)
                return;
            var cost = NextAssistantUpgradeCost(a.level);
            if (cost > 0 && _darkCoins < cost)
                return;
            _darkCoins -= cost;
            a.level = Mathf.Min(Z2RosterMaxAssistantLevel, a.level + 1);
            _zone2Assistants[index] = a;
            GrantSharedXp(1);
            PushSharedProgressToZone1Save();
            AudioManager.Instance?.PlayZone2Action();
        }

        void BeginReassignAssistantByIndex(int index)
        {
            if (index < 0 || index >= _zone2Assistants.Count)
            {
                _reassignAssistantIndex = -1;
                return;
            }
            _reassignAssistantIndex = index;
        }

        void RetireUnassignAssistantByIndex(int index)
        {
            if (index < 0 || index >= _zone2Assistants.Count)
                return;
            var a = _zone2Assistants[index];
            a.cellId = -1;
            _zone2Assistants[index] = a;
            if (_reassignAssistantIndex == index)
                _reassignAssistantIndex = -1;
            PushSharedProgressToZone1Save();
            AudioManager.Instance?.PlayZone2Action();
        }

        bool TryCompleteReassignToCellIndex(int cellIndex)
        {
            if (_reassignAssistantIndex < 0)
                return false;
            if (cellIndex < 0 || cellIndex >= _cells.Count)
            {
                _reassignAssistantIndex = -1;
                return true;
            }
            var c = _cells[cellIndex];
            if (c == null || !c.unlocked)
            {
                if (_txtHint != null)
                    _txtHint.text = "Reasignar: elige una celda desbloqueada.";
                return true;
            }
            var a = _zone2Assistants[_reassignAssistantIndex];
            if (a.cellId == c.id)
            {
                _reassignAssistantIndex = -1;
                if (_txtHint != null)
                    _txtHint.text = "Reasignar cancelada (misma celda).";
                return true;
            }
            a.cellId = c.id;
            _zone2Assistants[_reassignAssistantIndex] = a;
            _reassignAssistantIndex = -1;
            PushSharedProgressToZone1Save();
            AudioManager.Instance?.PlayZone2Action();
            if (_txtHint != null)
                _txtHint.text = $"Asistente reasignado a {c.displayName}.";
            return true;
        }

        void EnsureAssistantCount(int total)
        {
            total = Mathf.Clamp(total, 1, MaxAssistants);
            while (_zone2Assistants.Count < total)
            {
                _zone2Assistants.Add(new Zone2AssistantState
                {
                    cellId = -1,
                    type = (Zone2DistrictType)Random.Range(0, 4),
                    level = 1,
                });
            }
            while (_zone2Assistants.Count > total)
                _zone2Assistants.RemoveAt(_zone2Assistants.Count - 1);
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
            var data = new List<AssistantSaveData>(_zone2Assistants.Count);
            for (var i = 0; i < _zone2Assistants.Count; i++)
            {
                var a = _zone2Assistants[i];
                data.Add(new AssistantSaveData
                {
                    assistantId = i,
                    assignedSlotIndex = a.cellId,
                    assistantType = (int)a.type,
                    assistantLevel = a.level,
                });
            }
            return data;
        }

        void ApplyAssistantsSaveData(List<AssistantSaveData> data)
        {
            for (var i = 0; i < _zone2Assistants.Count; i++)
            {
                var a = _zone2Assistants[i];
                a.cellId = -1;
                _zone2Assistants[i] = a;
            }

            if (data == null)
                return;

            foreach (var saved in data)
            {
                if (saved == null || saved.assistantId < 0 || saved.assistantId >= _zone2Assistants.Count)
                    continue;
                var a = _zone2Assistants[saved.assistantId];
                a.cellId = saved.assignedSlotIndex;
                if (saved.assistantType >= 0 && saved.assistantType < 4)
                    a.type = (Zone2DistrictType)saved.assistantType;
                else
                    a.type = (Zone2DistrictType)(saved.assistantId % 4);
                a.level = saved.assistantLevel > 0
                    ? Mathf.Clamp(saved.assistantLevel, 1, Z2RosterMaxAssistantLevel)
                    : 1;
                _zone2Assistants[saved.assistantId] = a;
            }
        }

        List<Zone2CellSaveData> CaptureCellsSaveData()
        {
            var data = new List<Zone2CellSaveData>(_cells.Count);
            foreach (var cell in _cells)
            {
                data.Add(new Zone2CellSaveData
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

        void ApplyCellsSaveData(List<Zone2CellSaveData> data)
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
                cell.level = Zone2CellLevelRules.ClampLevel(saved.level);
                cell.producing = saved.producing;
                cell.ready = saved.ready;
                cell.corrupted = saved.corrupted;
                cell.remainingSeconds = Mathf.Max(0f, saved.remainingSeconds);
            }
        }

    }
}
