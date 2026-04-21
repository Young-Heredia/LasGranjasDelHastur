using System;
using System.Collections.Generic;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    public class ResourceManager : MonoBehaviour
    {
        public event Action Changed;

        [Header("Initial Values (Zone1 tests)")]
        [SerializeField] private int initialDarkCoins = 150;
        [SerializeField] private int initialWeakSouls = 0;
        [SerializeField] private int initialPureEnergy = 0;
        [SerializeField] private int initialMemoryShards = 0;
        [SerializeField] private int initialUnstableSouls = 0;

        readonly Dictionary<ResourceType, int> _amounts = new();

        void Awake()
        {
            _amounts[ResourceType.DarkCoins] = Mathf.Max(0, initialDarkCoins);
            _amounts[ResourceType.WeakSouls] = Mathf.Max(0, initialWeakSouls);
            _amounts[ResourceType.PureEnergy] = Mathf.Max(0, initialPureEnergy);
            _amounts[ResourceType.MemoryShards] = Mathf.Max(0, initialMemoryShards);
            _amounts[ResourceType.UnstableSouls] = Mathf.Max(0, initialUnstableSouls);
        }

        public int Get(ResourceType type) => _amounts.TryGetValue(type, out var v) ? v : 0;

        public void Add(ResourceType type, int amount)
        {
            if (amount == 0)
                return;
            var next = Mathf.Max(0, Get(type) + amount);
            _amounts[type] = next;
            Changed?.Invoke();
        }

        public bool TrySpend(ResourceType type, int amount)
        {
            if (amount <= 0)
                return true;
            var cur = Get(type);
            if (cur < amount)
                return false;
            _amounts[type] = cur - amount;
            Changed?.Invoke();
            return true;
        }

        public void Multiply(ResourceType type, float factor)
        {
            factor = Mathf.Max(0f, factor);
            var cur = Get(type);
            _amounts[type] = Mathf.FloorToInt(cur * factor);
            Changed?.Invoke();
        }
    }
}

