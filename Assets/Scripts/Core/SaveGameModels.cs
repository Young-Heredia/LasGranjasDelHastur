using System;
using System.Collections.Generic;
using LasGranjasDelHastur.Zone1;

namespace LasGranjasDelHastur.Core
{
    [Serializable]
    public class SaveGameData
    {
        public int saveVersion = 1;
        public string savedAtUtc = "";
        public string lastSceneName = "MainMenu";
        public bool zone1Available = false;
        public Zone1SaveData zone1 = new();
    }

    [Serializable]
    public class Zone1SaveData
    {
        public bool valid = false;
        public int darkCoins;
        public int weakSouls;
        public int pureEnergy;
        public int memoryShards;
        public int unstableSouls;

        public int level;
        public int xp;

        public int strikes;
        public int fineDebt;
        public float timeToNextTaxSeconds;
        public bool taxAlertActive;
        public float payWindowRemainingSeconds;

        public List<CellSaveData> cells = new();
        public int assistantTotal;
        public List<AssistantSaveData> assistants = new();
    }

    [Serializable]
    public class CellSaveData
    {
        public int slotIndex;
        public Zone1CellType cellType;
        public CellState state;
        public int level;
        public bool isCorrupted;
        public float producingRemainingSeconds;
    }

    [Serializable]
    public class AssistantSaveData
    {
        public int assistantId;
        public int assignedSlotIndex = -1;
    }
}

