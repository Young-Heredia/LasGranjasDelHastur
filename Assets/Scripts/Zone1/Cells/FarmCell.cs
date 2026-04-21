using System;
using LasGranjasDelHastur.Zone1.Cells;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    public class FarmCell : MonoBehaviour
    {
        public event Action<FarmCell> Changed;

        [Header("Definition")]
        [SerializeField] private Zone1CellDefinition definition;

        [Header("State")]
        [SerializeField] private CellState state = CellState.Blocked;
        [SerializeField] private int level = 1;
        [SerializeField] private bool isCorrupted;
        [SerializeField] private float producingRemaining;

        [Header("Slot")]
        [SerializeField] private int slotIndex;

        public Zone1CellDefinition Definition => definition;
        public CellState State => state;
        public int Level => level;
        public int SlotIndex => slotIndex;
        public bool IsCorrupted => isCorrupted || state == CellState.Corrupted;
        public float ProducingRemainingSeconds => producingRemaining;

        public string DisplayName => definition != null ? definition.displayName : name;
        public Zone1CellType CellType => definition != null ? definition.cellType : Zone1CellType.SoulPit;
        public ResourceType ProducesResource => definition != null ? definition.producesResource : ResourceType.WeakSouls;
        public float ProductionSeconds => definition != null ? definition.productionSeconds : 5f;
        public int ProductionAmount => definition != null ? definition.productionAmount : 1;
        public int PurchaseCostDarkCoins => definition != null ? definition.purchaseCostDarkCoins : 50;
        public float CorruptionRiskOnCollect => definition != null ? definition.corruptionRiskOnCollect : 0f;

        public void Configure(int newSlotIndex, Zone1CellDefinition def, CellState initialState, int initialLevel = 1)
        {
            slotIndex = newSlotIndex;
            definition = def;
            level = Mathf.Max(1, initialLevel);
            SetStateInternal(initialState, notify: false);
            isCorrupted = initialState == CellState.Corrupted;
            producingRemaining = 0f;
            NotifyChanged();
        }

        private void Update()
        {
            if (state != CellState.Producing)
                return;

            producingRemaining -= Time.deltaTime;
            if (producingRemaining <= 0f)
            {
                producingRemaining = 0f;
                SetStateInternal(CellState.ReadyToCollect, notify: true);
            }
            else
            {
                // Avoid spamming UI each frame; only notify at ~10Hz.
                if (Time.frameCount % 6 == 0)
                    NotifyChanged();
            }
        }

        public bool CanBuy(ResourceManager resources) =>
            state == CellState.Blocked && resources != null && resources.Get(ResourceType.DarkCoins) >= PurchaseCostDarkCoins;

        public bool TryBuy(ResourceManager resources)
        {
            if (resources == null)
                return false;
            if (state != CellState.Blocked)
                return false;
            if (!resources.TrySpend(ResourceType.DarkCoins, PurchaseCostDarkCoins))
                return false;

            isCorrupted = false;
            producingRemaining = 0f;
            SetStateInternal(CellState.Available, notify: true);
            return true;
        }

        public bool CanProduce() => state == CellState.Available && !IsCorrupted;

        public bool TryStartProduction()
        {
            if (!CanProduce())
                return false;

            producingRemaining = Mathf.Max(0.1f, ProductionSeconds);
            SetStateInternal(CellState.Producing, notify: true);
            return true;
        }

        public bool CanCollect() => state == CellState.ReadyToCollect && !IsCorrupted;

        public bool TryCollect(ResourceManager resources, ProgressionManager progression, out ResourceType producedType, out int amount)
        {
            producedType = ProducesResource;
            amount = ProductionAmount;

            if (resources == null)
                return false;
            if (!CanCollect())
                return false;

            resources.Add(producedType, amount);
            progression?.AddXp(CollectXp(amount));

            producingRemaining = 0f;
            SetStateInternal(CellState.Available, notify: true);

            if (CorruptionRiskOnCollect > 0f && UnityEngine.Random.value < CorruptionRiskOnCollect)
                Corrupt();

            return true;
        }

        int CollectXp(int producedAmount)
        {
            // Simple and tunable: 2 XP per unit, min 2.
            return Mathf.Max(2, producedAmount * 2);
        }

        public bool CanUpgrade(ResourceManager resources)
        {
            if (resources == null)
                return false;
            if (state != CellState.Available)
                return false;
            if (IsCorrupted)
                return false;

            // Placeholder upgrade cost curve.
            var cost = UpgradeCostDarkCoins();
            return resources.Get(ResourceType.DarkCoins) >= cost;
        }

        public bool TryUpgrade(ResourceManager resources, ProgressionManager progression)
        {
            if (resources == null)
                return false;
            if (!CanUpgrade(resources))
                return false;

            var cost = UpgradeCostDarkCoins();
            if (!resources.TrySpend(ResourceType.DarkCoins, cost))
                return false;

            level += 1;
            progression?.AddXp(10);
            NotifyChanged();
            return true;
        }

        int UpgradeCostDarkCoins()
        {
            // Cost grows; intentionally mild for Zone1 tests.
            return 25 + (level - 1) * 20;
        }

        public void Corrupt()
        {
            isCorrupted = true;
            producingRemaining = 0f;
            SetStateInternal(CellState.Corrupted, notify: true);
        }

        public bool CanCleanse(ResourceManager resources)
        {
            if (resources == null)
                return false;
            if (!IsCorrupted)
                return false;

            // Cleaning cost: small fixed + level scaling.
            return resources.Get(ResourceType.DarkCoins) >= CleanseCostDarkCoins();
        }

        public bool TryCleanse(ResourceManager resources)
        {
            if (resources == null)
                return false;
            if (!CanCleanse(resources))
                return false;
            if (!resources.TrySpend(ResourceType.DarkCoins, CleanseCostDarkCoins()))
                return false;

            isCorrupted = false;
            SetStateInternal(CellState.Available, notify: true);
            return true;
        }

        int CleanseCostDarkCoins() => 15 + (level - 1) * 10;

        void SetStateInternal(CellState newState, bool notify)
        {
            state = newState;
            if (newState != CellState.Corrupted)
                isCorrupted = false;
            if (notify)
                NotifyChanged();
        }

        void NotifyChanged() => Changed?.Invoke(this);
    }
}

