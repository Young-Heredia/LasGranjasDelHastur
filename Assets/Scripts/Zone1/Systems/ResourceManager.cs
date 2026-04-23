using System;
using System.Collections.Generic;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    public class ResourceManager : MonoBehaviour
    {
        public event Action Changed;
        public event Action<ResourceType, int, int> ResourceChanged;

        [Header("Initial Values (Zone1 tests)")]
        [SerializeField] private int initialDarkCoins = 150;
        [SerializeField] private int initialWeakSouls = 0;
        [SerializeField] private int initialPureEnergy = 0;
        [SerializeField] private int initialMemoryShards = 0;
        [SerializeField] private int initialUnstableSouls = 0;

        readonly Dictionary<ResourceType, int> _amounts = new();
        readonly Dictionary<ResourceType, int> _caps = new();
        bool _initialized;

        void Awake()
        {
            InitializeIfNeeded();
        }

        public void ConfigureInitialValues(int darkCoins, int weakSouls, int pureEnergy, int memoryShards, int unstableSouls, bool resetNow = true)
        {
            initialDarkCoins = Mathf.Max(0, darkCoins);
            initialWeakSouls = Mathf.Max(0, weakSouls);
            initialPureEnergy = Mathf.Max(0, pureEnergy);
            initialMemoryShards = Mathf.Max(0, memoryShards);
            initialUnstableSouls = Mathf.Max(0, unstableSouls);

            if (resetNow)
            {
                _initialized = false;
                InitializeIfNeeded();
                Changed?.Invoke();
            }
        }

        void InitializeIfNeeded()
        {
            if (_initialized)
                return;

            _amounts.Clear();
            _amounts[ResourceType.DarkCoins] = Mathf.Max(0, initialDarkCoins);
            _amounts[ResourceType.WeakSouls] = Mathf.Max(0, initialWeakSouls);
            _amounts[ResourceType.PureEnergy] = Mathf.Max(0, initialPureEnergy);
            _amounts[ResourceType.MemoryShards] = Mathf.Max(0, initialMemoryShards);
            _amounts[ResourceType.UnstableSouls] = Mathf.Max(0, initialUnstableSouls);

            _caps[ResourceType.DarkCoins] = int.MaxValue;
            _caps[ResourceType.WeakSouls] = int.MaxValue;
            _caps[ResourceType.PureEnergy] = int.MaxValue;
            _caps[ResourceType.MemoryShards] = int.MaxValue;
            _caps[ResourceType.UnstableSouls] = int.MaxValue;
            _initialized = true;
        }

        public int Get(ResourceType type) => _amounts.TryGetValue(type, out var v) ? v : 0;
        public int GetCapacity(ResourceType type) => _caps.TryGetValue(type, out var v) ? v : int.MaxValue;
        public bool HasFiniteCapacity(ResourceType type) => GetCapacity(type) < int.MaxValue;
        public int GetRemainingCapacity(ResourceType type) => Mathf.Max(0, GetCapacity(type) - Get(type));
        public bool IsAtCapacity(ResourceType type) => Get(type) >= GetCapacity(type);

        public float GetFill01(ResourceType type)
        {
            var cap = GetCapacity(type);
            if (cap <= 0 || cap == int.MaxValue)
                return 0f;
            return Mathf.Clamp01(Get(type) / (float)cap);
        }

        public bool CanAdd(ResourceType type, int amount)
        {
            if (amount <= 0)
                return true;
            return Get(type) + amount <= GetCapacity(type);
        }

        public void ConfigureStorageCaps(int weakSoulsCap, int pureEnergyCap, int memoryShardsCap, int unstableSoulsCap, bool clampCurrentAmounts = true)
        {
            _caps[ResourceType.DarkCoins] = int.MaxValue;
            _caps[ResourceType.WeakSouls] = Mathf.Max(1, weakSoulsCap);
            _caps[ResourceType.PureEnergy] = Mathf.Max(1, pureEnergyCap);
            _caps[ResourceType.MemoryShards] = Mathf.Max(1, memoryShardsCap);
            _caps[ResourceType.UnstableSouls] = Mathf.Max(1, unstableSoulsCap);

            if (!clampCurrentAmounts)
            {
                Changed?.Invoke();
                return;
            }

            ClampToCaps();
            Changed?.Invoke();
        }

        public void Add(ResourceType type, int amount)
        {
            if (amount == 0)
                return;
            var cur = Get(type);
            var next = Mathf.Clamp(cur + amount, 0, GetCapacity(type));
            if (next == cur)
                return;
            _amounts[type] = next;
            Changed?.Invoke();
            ResourceChanged?.Invoke(type, next, next - cur);
        }

        public bool TrySpend(ResourceType type, int amount)
        {
            if (amount <= 0)
                return true;
            var cur = Get(type);
            if (cur < amount)
                return false;
            var next = cur - amount;
            _amounts[type] = next;
            Changed?.Invoke();
            ResourceChanged?.Invoke(type, next, next - cur);
            return true;
        }

        public void Multiply(ResourceType type, float factor)
        {
            factor = Mathf.Max(0f, factor);
            var cur = Get(type);
            var next = Mathf.FloorToInt(cur * factor);
            _amounts[type] = next;
            Changed?.Invoke();
            ResourceChanged?.Invoke(type, next, next - cur);
        }

        public void Set(ResourceType type, int amount)
        {
            var cur = Get(type);
            var next = Mathf.Max(0, amount);
            _amounts[type] = next;
            Changed?.Invoke();
            ResourceChanged?.Invoke(type, next, next - cur);
        }

        void ClampToCaps()
        {
            ClampResource(ResourceType.WeakSouls);
            ClampResource(ResourceType.PureEnergy);
            ClampResource(ResourceType.MemoryShards);
            ClampResource(ResourceType.UnstableSouls);
        }

        void ClampResource(ResourceType type)
        {
            var cur = Get(type);
            var next = Mathf.Clamp(cur, 0, GetCapacity(type));
            if (next != cur)
            {
                _amounts[type] = next;
                ResourceChanged?.Invoke(type, next, next - cur);
            }
        }
    }
}

