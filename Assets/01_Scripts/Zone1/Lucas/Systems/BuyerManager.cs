using System;
using System.Collections.Generic;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    public class BuyerManager : MonoBehaviour
    {
        public event Action Changed;

        [Header("Definitions (optional; runtime defaults if empty)")]
        [SerializeField] private List<BuyerDefinition> buyers = new();

        [Header("Dynamic Prices")]
        [SerializeField, Range(0.5f, 1f)] private float priceMinMultiplier = 0.75f;
        [SerializeField, Range(1f, 2f)] private float priceMaxMultiplier = 1.35f;
        [SerializeField, Range(0f, 0.1f)] private float levelDemandBonusPerLevel = 0.02f;
        [SerializeField, Range(0f, 0.5f)] private float stockPenaltyAtFull = 0.2f;
        [SerializeField, Range(0f, 0.1f)] private float soldPressurePerUnit = 0.02f;
        [SerializeField, Range(0f, 1f)] private float soldPressureRecoveryPerSecond = 0.12f;

        ResourceManager _resources;
        ProgressionManager _progression;
        readonly Dictionary<BuyerDefinition, float> _soldPressure = new();
        readonly List<BuyerDefinition> _buyerKeysBuffer = new();
        float _broadcastTimer;

        public IReadOnlyList<BuyerDefinition> Buyers => buyers;

        public void Initialize(ResourceManager resources, ProgressionManager progression)
        {
            _resources = resources;
            _progression = progression;

            if (buyers == null || buyers.Count == 0)
                buyers = CreateRuntimeDefaultBuyers();

            _soldPressure.Clear();
            foreach (var buyer in buyers)
                _soldPressure[buyer] = 0f;

            Changed?.Invoke();
        }

        public void ConfigureEconomy(float newPriceMinMultiplier, float newPriceMaxMultiplier, float newLevelDemandBonusPerLevel, float newStockPenaltyAtFull, float newSoldPressurePerUnit, float newSoldPressureRecoveryPerSecond)
        {
            priceMinMultiplier = Mathf.Clamp(newPriceMinMultiplier, 0.5f, 1f);
            priceMaxMultiplier = Mathf.Clamp(newPriceMaxMultiplier, 1f, 2f);
            if (priceMaxMultiplier < priceMinMultiplier)
                priceMaxMultiplier = priceMinMultiplier;
            levelDemandBonusPerLevel = Mathf.Clamp(newLevelDemandBonusPerLevel, 0f, 0.1f);
            stockPenaltyAtFull = Mathf.Clamp(newStockPenaltyAtFull, 0f, 0.5f);
            soldPressurePerUnit = Mathf.Clamp(newSoldPressurePerUnit, 0f, 0.1f);
            soldPressureRecoveryPerSecond = Mathf.Clamp(newSoldPressureRecoveryPerSecond, 0f, 1f);
            Changed?.Invoke();
        }

        void Update()
        {
            if (_soldPressure.Count == 0 || soldPressureRecoveryPerSecond <= 0f)
                return;

            var recover = soldPressureRecoveryPerSecond * Time.deltaTime;
            var changed = false;
            _buyerKeysBuffer.Clear();
            _buyerKeysBuffer.AddRange(_soldPressure.Keys);
            for (var i = 0; i < _buyerKeysBuffer.Count; i++)
            {
                var key = _buyerKeysBuffer[i];
                var cur = _soldPressure[key];
                var next = Mathf.Max(0f, cur - recover);
                if (Mathf.Abs(next - cur) <= 0.0001f)
                    continue;
                _soldPressure[key] = next;
                changed = true;
            }

            if (!changed)
                return;

            _broadcastTimer += Time.deltaTime;
            if (_broadcastTimer >= 0.4f)
            {
                _broadcastTimer = 0f;
                Changed?.Invoke();
            }
        }

        public static int GetXpRewardForUnits(int unitsSold) => Mathf.Max(2, Mathf.Max(0, unitsSold));

        public int GetAvailableToSell(ResourceType type) => _resources != null ? _resources.Get(type) : 0;
        public int GetCurrentPrice(BuyerDefinition buyer)
        {
            if (buyer == null)
                return 1;
            return Mathf.Max(1, Mathf.RoundToInt(buyer.basePricePerUnit * ComputePriceMultiplier(buyer)));
        }

        public bool TrySell(BuyerDefinition buyer, int amount)
        {
            if (buyer == null || _resources == null)
                return false;
            amount = Mathf.Max(0, amount);
            if (amount <= 0)
                return false;

            if (_resources.Get(buyer.buysResource) < amount)
                return false;

            var price = GetCurrentPrice(buyer);
            var revenue = amount * price;
            if (!_resources.TrySpend(buyer.buysResource, amount))
                return false;

            _resources.Add(ResourceType.DarkCoins, revenue);
            _progression?.AddXp(GetXpRewardForUnits(amount));
            _soldPressure[buyer] = Mathf.Clamp01(GetSoldPressure(buyer) + amount * soldPressurePerUnit);
            Changed?.Invoke();
            return true;
        }

        float ComputePriceMultiplier(BuyerDefinition buyer)
        {
            if (buyer == null)
                return 1f;

            var levelBonus = 1f + Mathf.Max(0, (_progression?.Level ?? 1) - 1) * levelDemandBonusPerLevel;

            var stockFill = _resources != null ? _resources.GetFill01(buyer.buysResource) : 0f;
            var stockMultiplier = Mathf.Lerp(1f + stockPenaltyAtFull * 0.5f, 1f - stockPenaltyAtFull, stockFill);

            var pressure = GetSoldPressure(buyer);
            var pressureMultiplier = 1f - pressure * 0.45f;

            var raw = levelBonus * stockMultiplier * pressureMultiplier;
            return Mathf.Clamp(raw, priceMinMultiplier, priceMaxMultiplier);
        }

        float GetSoldPressure(BuyerDefinition buyer)
        {
            if (buyer == null)
                return 0f;
            return _soldPressure.TryGetValue(buyer, out var value) ? value : 0f;
        }

        List<BuyerDefinition> CreateRuntimeDefaultBuyers()
        {
            var list = new List<BuyerDefinition>();

            var deepOnes = ScriptableObject.CreateInstance<BuyerDefinition>();
            deepOnes.buyerName = "Los Profundos";
            deepOnes.buysResource = ResourceType.WeakSouls;
            deepOnes.basePricePerUnit = 3;
            deepOnes.contractUnitsPerDeal = 6;
            list.Add(deepOnes);

            var yekuvian = ScriptableObject.CreateInstance<BuyerDefinition>();
            yekuvian.buyerName = "Yekuvian";
            yekuvian.buysResource = ResourceType.PureEnergy;
            yekuvian.basePricePerUnit = 6;
            yekuvian.contractUnitsPerDeal = 4;
            list.Add(yekuvian);

            var fallenAngels = ScriptableObject.CreateInstance<BuyerDefinition>();
            fallenAngels.buyerName = "Ángeles Caídos";
            fallenAngels.buysResource = ResourceType.UnstableSouls;
            fallenAngels.basePricePerUnit = 12;
            fallenAngels.contractUnitsPerDeal = 10;
            list.Add(fallenAngels);

            return list;
        }
    }
}

