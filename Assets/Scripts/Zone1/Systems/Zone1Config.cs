using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    [CreateAssetMenu(menuName = "Las Granjas del Hastur/Zone1/Zone1 Config", fileName = "Zone1Config")]
    public class Zone1Config : ScriptableObject
    {
        [Header("Initial Resources")]
        [Min(0)] public int initialDarkCoins = 150;
        [Min(0)] public int initialWeakSouls = 0;
        [Min(0)] public int initialPureEnergy = 0;
        [Min(0)] public int initialMemoryShards = 0;
        [Min(0)] public int initialUnstableSouls = 0;

        [Header("Progression")]
        [Min(1)] public int initialLevel = 1;
        [Min(0)] public int initialXp = 0;
        [Min(1)] public int baseXpToLevel = 50;
        [Min(1)] public int xpGrowthPerLevel = 25;

        [Header("Cells Setup")]
        [Min(1)] public int gridColumns = 6;
        [Min(1)] public int gridRows = 5;
        public Vector2 gridSpacing = new(2.14f, 1.92f);
        public Vector2 gridOrigin = new(-5.35f, 3.65f);
        [Min(0)] public int initiallyUnlockedCells = 1;
        [Min(0)] public int initiallyPurchasableCells = 2;

        [Header("Tax (Zone1)")]
        public string collectorName = "Cthulhu";
        [Range(0f, 1f)] public float baseTaxPercent = 0.15f;
        [Min(5f)] public float taxIntervalSeconds = 90f;
        [Min(5f)] public float payWindowSeconds = 20f;
        [Range(0f, 1f)] public float moneyLossOnFail = 0.75f;
        [Min(1)] public int finePerStrikeStep = 25;
        [Min(1)] public int maxStrikesBeforeGameOver = 3;

        [Header("Buyer Dynamic Prices")]
        [Range(0.5f, 1f)] public float buyerPriceMinMultiplier = 0.75f;
        [Range(1f, 2f)] public float buyerPriceMaxMultiplier = 1.35f;
        [Range(0f, 0.1f)] public float buyerLevelDemandBonusPerLevel = 0.02f;
        [Range(0f, 0.5f)] public float buyerStockPenaltyAtFull = 0.2f;
        [Range(0f, 0.1f)] public float buyerSoldPressurePerUnit = 0.02f;
        [Range(0f, 1f)] public float buyerPressureRecoveryPerSecond = 0.12f;

        [Header("Cell Costs Scaling")]
        [Range(0f, 1f)] public float cellPurchaseSlotScale = 0.12f;
        [Min(1)] public int cellUpgradeBaseCost = 25;
        [Min(0)] public int cellUpgradePerLevelAdd = 20;
        [Range(1f, 3f)] public float cellUpgradeLevelMultiplier = 1.15f;

        [Header("Storage Capacity")]
        [Min(1)] public int weakSoulsCapacity = 120;
        [Min(1)] public int pureEnergyCapacity = 90;
        [Min(1)] public int memoryShardsCapacity = 70;
        [Min(1)] public int unstableSoulsCapacity = 55;

        [Header("Assistants (Zone1)")]
        [Min(0)] public int initialAssistants = 1;
        [Min(1)] public int maxAssistants = 30;
        [Min(0.05f)] public float assistantAutomationTickSeconds = 0.25f;

        [Header("Camera Bounds")]
        public Vector2 cameraMinBounds = new(-13f, -10f);
        public Vector2 cameraMaxBounds = new(13f, 8f);
    }
}

