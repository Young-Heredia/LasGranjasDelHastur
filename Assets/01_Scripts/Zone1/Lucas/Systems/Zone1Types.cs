using System;

namespace LasGranjasDelHastur.Zone1
{
    public enum ResourceType
    {
        DarkCoins = 0,
        WeakSouls = 10,
        PureEnergy = 20,
        MemoryShards = 30,
        UnstableSouls = 40,
    }

    public enum Zone1CellType
    {
        SoulPit = 0,
        EnergyWell = 1,
        EchoChamber = 2,
        BrokenAltar = 3,
    }

    [Serializable]
    public enum CellState
    {
        Blocked = 0,
        Available = 1,
        Producing = 2,
        ReadyToCollect = 3,
        Corrupted = 4,
        Upgrading = 5,
    }
}

