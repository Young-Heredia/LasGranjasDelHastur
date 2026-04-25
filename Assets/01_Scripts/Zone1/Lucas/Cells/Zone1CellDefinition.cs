using UnityEngine;

namespace LasGranjasDelHastur.Zone1.Cells
{
    [CreateAssetMenu(menuName = "Las Granjas del Hastur/Zone1/Cell Definition", fileName = "Zone1CellDefinition_")]
    public class Zone1CellDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Cell";
        public LasGranjasDelHastur.Zone1.Zone1CellType cellType;

        [Header("Production")]
        public LasGranjasDelHastur.Zone1.ResourceType producesResource;
        [Min(0.1f)] public float productionSeconds = 5f;
        [Min(1)] public int productionAmount = 1;

        [Header("Economy")]
        [Min(0)] public int purchaseCostDarkCoins = 50;

        [Header("Corruption")]
        [Range(0f, 1f)] public float corruptionRiskOnCollect = 0f;
    }
}

