using UnityEngine;

namespace LasGranjasDelHastur.Zone1.Gacha
{
    /// <summary>
    /// Entrada al gacha mundial (mismo panel que Zona 1). Las fuentes usan <see cref="Zone1GachaFountainInteract"/>;
    /// otras zonas pueden usar este marcador. <see cref="CellManager"/> prioriza ambos antes que <see cref="FarmCell"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GachaWorldInteract : MonoBehaviour { }
}
