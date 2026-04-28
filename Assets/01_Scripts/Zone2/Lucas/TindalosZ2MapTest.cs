using LasGranjasDelHastur.Creatures;
using UnityEngine;

namespace LasGranjasDelHastur.Zone2
{
    /// <summary>
    /// Coloca frente a la rejilla (Z2) 5 ejemplares de Tíndalos para probar posición; solo si no existen aún.
    /// </summary>
    public static class TindalosZ2MapTest
    {
        const string ContainerName = "Layer_Decor/Tindalos_Demo";
        // Banda bajo la rejilla (Y ~ -5) para no tapar las 6×5 celdas; espaciado en X
        static readonly Vector3[] TestPositions =
        {
            new(-7.5f, -5.0f, 0f),
            new(-3.5f, -5.1f, 0f),
            new(0f, -5.15f, 0f),
            new(3.5f, -5.1f, 0f),
            new(7.5f, -5.0f, 0f),
        };

        public static void PlacePrototypesIfMissing(Transform worldRoot)
        {
            if (worldRoot == null)
                return;
            if (worldRoot.Find(ContainerName) != null)
                return;
            var decor = worldRoot.Find("Layer_Decor");
            if (decor == null)
                return;
            var root = new GameObject("Tindalos_Demo");
            root.transform.SetParent(decor, false);
            root.transform.localPosition = Vector3.zero;
            for (var i = 0; i < TindalosHoundBuilder.AllKinds.Length; i++)
            {
                var k = TindalosHoundBuilder.AllKinds[i];
                TindalosHoundBuilder.Build(root.transform, k, i < TestPositions.Length
                    ? TestPositions[i]
                    : new Vector3((i - 2) * 3f, -3.5f, 0f), 0);
            }
        }
    }
}
