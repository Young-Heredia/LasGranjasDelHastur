using System;
using System.Collections.Generic;
using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1.Cells;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    public class AssistantManager : MonoBehaviour
    {
        public event Action Changed;

        [Header("Assistants")]
        [SerializeField, Min(1)] private int maxAssistants = 30;
        [SerializeField, Min(0)] private int initialAssistants = 1;
        [SerializeField, Min(0.05f)] private float automationTickSeconds = 0.25f;
        [SerializeField] private bool oneAssistantPerCell = true;
        [SerializeField, Min(1)] private int assistantBuyBaseCost = 120;
        [SerializeField, Min(0)] private int assistantBuyCostStep = 35;

        readonly List<int> _assistantAssignedSlot = new();
        CellManager _cells;
        ResourceManager _resources;
        ProgressionManager _progression;
        float _tick;
        bool _initialized;

        public int TotalAssistants => _assistantAssignedSlot.Count;
        public int AssignedAssistants
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _assistantAssignedSlot.Count; i++)
                {
                    if (_assistantAssignedSlot[i] >= 0)
                        count++;
                }
                return count;
            }
        }
        public int AvailableAssistants => Mathf.Max(0, TotalAssistants - AssignedAssistants);
        public int NextAssistantCost => assistantBuyBaseCost + Mathf.Max(0, TotalAssistants - initialAssistants) * assistantBuyCostStep;

        public void Configure(int newInitialAssistants, int newMaxAssistants, float newAutomationTickSeconds)
        {
            initialAssistants = Mathf.Max(0, newInitialAssistants);
            maxAssistants = Mathf.Max(1, newMaxAssistants);
            automationTickSeconds = Mathf.Max(0.05f, newAutomationTickSeconds);
        }

        public void Initialize(CellManager cells, ResourceManager resources, ProgressionManager progression)
        {
            _cells = cells;
            _resources = resources;
            _progression = progression;

            if (_initialized)
                return;

            EnsureAssistantCount(initialAssistants);
            _initialized = true;
            Changed?.Invoke();
        }

        void Update()
        {
            if (!_initialized || _cells == null || _resources == null)
                return;

            _tick += Time.deltaTime;
            if (_tick < automationTickSeconds)
                return;
            _tick = 0f;

            for (var i = 0; i < _assistantAssignedSlot.Count; i++)
            {
                var slot = _assistantAssignedSlot[i];
                if (slot < 0)
                    continue;

                var cell = _cells.GetCellBySlotIndex(slot);
                if (cell == null)
                {
                    _assistantAssignedSlot[i] = -1;
                    continue;
                }

                if (cell.CanCollect(_resources))
                {
                    if (cell.TryCollect(_resources, _progression, out _, out _))
                        _cells.ApplyVisual(cell);
                    continue;
                }

                if (cell.CanProduce(_resources))
                {
                    if (cell.TryStartProduction(_resources))
                        _cells.ApplyVisual(cell);
                }
            }
        }

        public bool CanAssignToCell(FarmCell cell)
        {
            if (cell == null)
                return false;
            if (cell.State == CellState.Blocked || cell.IsCorrupted)
                return false;
            if (oneAssistantPerCell && HasAssistantOnCell(cell))
                return false;
            return AvailableAssistants > 0;
        }

        public bool TryAssignToCell(FarmCell cell)
        {
            if (!CanAssignToCell(cell))
                return false;

            for (var i = 0; i < _assistantAssignedSlot.Count; i++)
            {
                if (_assistantAssignedSlot[i] >= 0)
                    continue;
                _assistantAssignedSlot[i] = cell.SlotIndex;
                Changed?.Invoke();
                return true;
            }

            return false;
        }

        public bool TryUnassignFromCell(FarmCell cell)
        {
            if (cell == null)
                return false;

            for (var i = 0; i < _assistantAssignedSlot.Count; i++)
            {
                if (_assistantAssignedSlot[i] != cell.SlotIndex)
                    continue;
                _assistantAssignedSlot[i] = -1;
                Changed?.Invoke();
                return true;
            }

            return false;
        }

        public bool TryBuyAssistant()
        {
            if (_resources == null)
                return false;
            if (TotalAssistants >= maxAssistants)
                return false;

            var cost = NextAssistantCost;
            if (!_resources.TrySpend(ResourceType.DarkCoins, cost))
                return false;

            _assistantAssignedSlot.Add(-1);
            Changed?.Invoke();
            return true;
        }

        public bool HasAssistantOnCell(FarmCell cell)
        {
            return GetAssistantCountOnCell(cell) > 0;
        }

        public int GetAssistantCountOnCell(FarmCell cell)
        {
            if (cell == null)
                return 0;

            var count = 0;
            for (var i = 0; i < _assistantAssignedSlot.Count; i++)
            {
                if (_assistantAssignedSlot[i] == cell.SlotIndex)
                    count++;
            }
            return count;
        }

        public List<AssistantSaveData> CaptureSaveData()
        {
            var data = new List<AssistantSaveData>(_assistantAssignedSlot.Count);
            for (var i = 0; i < _assistantAssignedSlot.Count; i++)
            {
                data.Add(new AssistantSaveData
                {
                    assistantId = i,
                    assignedSlotIndex = _assistantAssignedSlot[i],
                });
            }
            return data;
        }

        public void ApplySaveData(int savedTotalAssistants, List<AssistantSaveData> assignments)
        {
            var target = Mathf.Clamp(savedTotalAssistants, 0, maxAssistants);
            if (target <= 0 && initialAssistants > 0)
                target = initialAssistants;
            EnsureAssistantCount(target);

            for (var i = 0; i < _assistantAssignedSlot.Count; i++)
                _assistantAssignedSlot[i] = -1;

            if (assignments != null)
            {
                for (var i = 0; i < assignments.Count; i++)
                {
                    var saved = assignments[i];
                    if (saved == null)
                        continue;
                    if (saved.assistantId < 0 || saved.assistantId >= _assistantAssignedSlot.Count)
                        continue;
                    _assistantAssignedSlot[saved.assistantId] = saved.assignedSlotIndex;
                }
            }

            Changed?.Invoke();
        }

        void EnsureAssistantCount(int count)
        {
            count = Mathf.Clamp(count, 0, maxAssistants);
            _assistantAssignedSlot.Clear();
            for (var i = 0; i < count; i++)
                _assistantAssignedSlot.Add(-1);
        }
    }
}
