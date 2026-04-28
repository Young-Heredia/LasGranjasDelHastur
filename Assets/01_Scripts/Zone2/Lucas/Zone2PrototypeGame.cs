using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone2.Systems;
using System.Collections.Generic;
using UnityEngine;

namespace LasGranjasDelHastur.Zone2
{
    [DisallowMultipleComponent]
    public partial class Zone2PrototypeGame : MonoBehaviour
    {
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
        readonly List<int> _assistantAssignedCellId = new();

        Zone2CellRuntime _selectedCell;
        Zone2CellManager _worldCells;

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
                    ? new Color(0.75f, 0.55f, 0.90f, 1f)
                    : c.ready
                        ? new Color(1f, 0.96f, 0.80f, 1f)
                        : c.unlocked
                            ? Color.white
                            : new Color(0.55f, 0.55f, 0.60f, 1f);

                _worldCells.ApplyVisual(i, spritePath, tint);
                // Como Zona 1: celdas bloqueadas también se pueden seleccionar (para comprar).
                _worldCells.SetSlotColliderEnabled(i, true);
            }
        }

        static string GetCellSpritePath(Zone2CellRuntime cell)
        {
            const string root = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/";
            if (cell == null)
                return root + "Cells/Base/Cell_Empty.png";

            return cell.displayName switch
            {
                "Distrito Condensador" => root + "Cells/Zone2/Zone2_Cell_CityCondenser.png",
                "Torre de Cultistas" => root + "Cells/Zone2/Zone2_Cell_CultistTower.png",
                "Mercado Maldito" => root + "Cells/Zone2/Zone2_Cell_CursedMarket.png",
                "Archivo de Yith" => root + "Cells/Zone2/Zone2_Cell_YithArchive.png",
                _ => root + "Cells/Base/Cell_Empty.png",
            };
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
                PushSharedProgressToZone1Save();
                return;
            }

            GlobalTaxLedger.RegisterStrikeFailure(GameOverOrigin.CondensedCities);
            var strikeCount = GlobalTaxLedger.GetStrikes();
            _darkCoins = Mathf.FloorToInt(_darkCoins * 0.25f);
            TryCorruptRandomUnlockedCell();
            _txtHint.text = $"No alcanzó para pagar. Multa {strikeCount}/3 y corrupción aplicada.";
            AudioManager.Instance?.PlayZone2TaxAlert();
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
            var names = new[]
            {
                ("Distrito Condensador", true),
                ("Torre de Cultistas", false),
                ("Mercado Maldito", true),
                ("Archivo de Yith", false),
            };

            for (var i = 0; i < 12; i++)
            {
                var template = names[i % names.Length];
                _cells.Add(new Zone2CellRuntime
                {
                    id = i,
                    displayName = template.Item1,
                    producesSupplies = template.Item2,
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
            var cost = 40 * _selectedCell.level;
            if (_darkCoins < cost)
                return;

            _darkCoins -= cost;
            _selectedCell.level++;
            AudioManager.Instance?.PlayZone2TierUp();
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
            _selectedCell.level = 1;
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

            var assistantBoost = Mathf.Clamp(GetAssignedAssistantsOnCell(cell.id), 0, 4) * 0.25f;
            var speed = 1f + (_difficultyTier - 1) * 0.1f + assistantBoost;
            cell.remainingSeconds = Mathf.Max(1.2f, (6.0f - cell.level * 0.4f) / speed);
            cell.producing = true;
            AudioManager.Instance?.PlayZone2Action();
        }

        void CollectCell(Zone2CellRuntime cell)
        {
            if (cell == null || !cell.ready)
                return;

            cell.ready = false;
            var amount = 3 + cell.level + GetAssignedAssistantsOnCell(cell.id);
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
                cell.level = Mathf.Max(1, saved.level);
                cell.producing = saved.producing;
                cell.ready = saved.ready;
                cell.corrupted = saved.corrupted;
                cell.remainingSeconds = Mathf.Max(0f, saved.remainingSeconds);
            }
        }

    }
}
