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

        ResourceManager _resources;
        ProgressionManager _progression;

        public IReadOnlyList<BuyerDefinition> Buyers => buyers;

        public void Initialize(ResourceManager resources, ProgressionManager progression)
        {
            _resources = resources;
            _progression = progression;

            if (buyers == null || buyers.Count == 0)
                buyers = CreateRuntimeDefaultBuyers();

            Changed?.Invoke();
        }

        public int GetAvailableToSell(ResourceType type) => _resources != null ? _resources.Get(type) : 0;

        public bool TrySell(BuyerDefinition buyer, int amount)
        {
            if (buyer == null || _resources == null)
                return false;
            amount = Mathf.Max(0, amount);
            if (amount <= 0)
                return false;

            if (_resources.Get(buyer.buysResource) < amount)
                return false;

            var revenue = amount * Mathf.Max(1, buyer.basePricePerUnit);
            if (!_resources.TrySpend(buyer.buysResource, amount))
                return false;

            _resources.Add(ResourceType.DarkCoins, revenue);
            _progression?.AddXp(Mathf.Max(2, amount)); // Simple: 1 XP per unit, min 2.
            Changed?.Invoke();
            return true;
        }

        List<BuyerDefinition> CreateRuntimeDefaultBuyers()
        {
            var list = new List<BuyerDefinition>();

            var deepOnes = ScriptableObject.CreateInstance<BuyerDefinition>();
            deepOnes.buyerName = "Los Profundos";
            deepOnes.buysResource = ResourceType.WeakSouls;
            deepOnes.basePricePerUnit = 3;
            list.Add(deepOnes);

            var yekuvian = ScriptableObject.CreateInstance<BuyerDefinition>();
            yekuvian.buyerName = "Yekuvian";
            yekuvian.buysResource = ResourceType.PureEnergy;
            yekuvian.basePricePerUnit = 6;
            list.Add(yekuvian);

            var fallenAngels = ScriptableObject.CreateInstance<BuyerDefinition>();
            fallenAngels.buyerName = "Ángeles Caídos";
            fallenAngels.buysResource = ResourceType.UnstableSouls;
            fallenAngels.basePricePerUnit = 12;
            list.Add(fallenAngels);

            return list;
        }
    }
}

