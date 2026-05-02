using System;
using System.Collections.Generic;
using LasGranjasDelHastur.Zone1;

namespace LasGranjasDelHastur.Core
{
    [Serializable]
    public class SaveGameData
    {
        public int saveVersion = 2;
        public string savedAtUtc = "";
        public string lastSceneName = "MainMenu";
        /// <summary>Multas de impuestos compartidas entre todas las zonas jugables.</summary>
        public int globalTaxStrikes;
        public bool zone1Available = false;
        public Zone1SaveData zone1 = new();
        public bool zone2Available = false;
        public Zone2SaveData zone2 = new();
        public bool zone3Available = false;
        public Zone3SaveData zone3 = new();
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

        /// <summary>
        /// True si ya se cobró el bono del easter egg de cultistas en esta run (se resetea con Game Over / nuevo juego).
        /// </summary>
        public bool zone1EasterEggBonusClaimed;

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
        /// <summary>Zona 2/3: arquetipo (0=Huerto Lunar … 3=Incubadora). Z1 ignora; default 0.</summary>
        public int assistantType;
        /// <summary>Zona 2/3: nivel de asistente. Z1 ignora; 0 en disco se trata como 1 en runtime.</summary>
        public int assistantLevel;
    }

    [Serializable]
    public class Zone2SaveData
    {
        public bool valid = false;
        public int darkCoins;
        public int citySupplies;
        public int arcaneBlueprints;
        public int difficultyTier;
        public int totalSold;
        public float taxTimer;
        public float runtimeSeconds;
        public int strikes;
        public int assistantsTotal;
        public int nextCellCost;
        public List<AssistantSaveData> assistants = new();
        public List<Zone2CellSaveData> cells = new();
    }

    [Serializable]
    public class Zone3SaveData
    {
        public bool valid = false;
        public int darkCoins;
        public int astralResidue;
        public int voidInk;
        public int difficultyTier;
        public int totalSold;
        public float taxTimer;
        public float runtimeSeconds;
        public int prestigePoints;
        public bool endNarrativeShown;
        public int strikes;
        public int assistantsTotal;
        public int nextCellCost;
        public List<AssistantSaveData> assistants = new();
        public List<Zone3CellSaveData> cells = new();
    }

    [Serializable]
    public class Zone2CellSaveData
    {
        public int cellId;
        public string displayName = "";
        public bool unlocked;
        public int level;
        public bool producing;
        public bool ready;
        public bool corrupted;
        public float remainingSeconds;
        public int assignedAssistants;
    }

    [Serializable]
    public class Zone3CellSaveData
    {
        public int cellId;
        public string displayName = "";
        public bool unlocked;
        public int level;
        public bool producing;
        public bool ready;
        public bool corrupted;
        public float remainingSeconds;
        public int assignedAssistants;
    }
}

