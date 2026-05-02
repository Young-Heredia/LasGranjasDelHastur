using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    [CreateAssetMenu(menuName = "Las Granjas del Hastur/Zone1/Buyer Definition", fileName = "Zone1Buyer_")]
    public class BuyerDefinition : ScriptableObject
    {
        public string buyerName = "Buyer";
        public ResourceType buysResource;
        [Min(1)] public int basePricePerUnit = 1;

        /// <summary>Unidades que este comprador exige por contrato en el panel de oferta (venta atómica).</summary>
        [Min(1)] public int contractUnitsPerDeal = 1;

        public int ContractBatchSize => Mathf.Max(1, contractUnitsPerDeal);
    }
}

