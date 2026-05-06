using UnityEngine;

namespace LasGranjasDelHastur.Zone2.Jose
{
    /// <summary>
    /// Rejilla 4×3 para Zona 2 usando el mismo origen y paso que el asset <c>Zone1Config</c> (rejilla 6×5 en mazmorras),
    /// de modo que el ritmo espacial coincide con Zona 1.
    /// </summary>
    public static class Zone2CellGridLayout
    {
        public const int Columns = 4;
        public const int Rows = 3;

        /// <summary>Igual que <c>Zone1Config.gridSpacing</c>.</summary>
        public static readonly Vector2 Spacing = new(2.14f, 1.92f);

        /// <summary>Igual que <c>Zone1Config.gridOrigin</c> (esquina de slot [0,0]).</summary>
        public static readonly Vector2 Origin = new(-5.35f, 3.65f);

        public const int InitiallyUnlockedCells = 1;
        public const int InitiallyPurchasableCells = 2;

        public static Vector3 WorldSlotAnchor(int column, int row) =>
            new(Origin.x + column * Spacing.x, Origin.y - row * Spacing.y, 0f);

        /// <summary>
        /// Pequeño desplazamiento determinista por índice (no aleatorio) para separar siluetas isométricas
        /// sin romper la rejilla lógica.
        /// </summary>
        public static Vector2 SlotWobble(int slotIndex)
        {
            var u = slotIndex * 0.913f + 1.127f;
            return new Vector2(Mathf.Sin(u * 3.891f) * 0.06f, Mathf.Cos(u * 2.417f) * 0.048f);
        }
    }
}
