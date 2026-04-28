using System.IO;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    /// <summary>
    /// Resuelve la ruta de sprite de celdas: prioriza <c>Assets/02_Sprites/Jose/Zone1/Cells</c> si el archivo existe; si no, <c>Lucas/Zone1/Cells</c>.
    /// Estados: <see cref="CellState.Blocked"/>, vacío = <c>idle</c>, <see cref="CellState.Producing"/>, <see cref="CellState.ReadyToCollect"/>, <c>corrupt</c>.
    /// </summary>
    public static class CellSpritePathResolver
    {
        public const string JoseCellsDir = "Assets/02_Sprites/Jose/Zone1/Cells";
        public const string LucasCellsDir = "Assets/02_Sprites/Lucas/Zone1/Cells";

        public static string ResolveForCell(FarmCell cell)
        {
            var name = GetFileNameForCell(cell);
            return ResolveByFileName(name);
        }

        public static string ResolveByFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return $"{LucasCellsDir}/zone1_soulpit_blocked.png";
            if (!fileName.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                return $"{LucasCellsDir}/{fileName}";

            var jose = $"{JoseCellsDir}/{fileName}";
            if (FileExistsAsAssetPath(jose))
                return jose;
            return $"{LucasCellsDir}/{fileName}";
        }

        public static string GetFileNameForCell(FarmCell cell)
        {
            if (cell == null)
                return "zone1_soulpit_blocked.png";

            var type = cell.CellType switch
            {
                Zone1CellType.SoulPit => "soulpit",
                Zone1CellType.EnergyWell => "energywell",
                Zone1CellType.EchoChamber => "echochamber",
                Zone1CellType.BrokenAltar => "brokenaltar",
                _ => "soulpit"
            };

            var state = cell.IsCorrupted ? "corrupt" : cell.State switch
            {
                CellState.Blocked => "blocked",
                CellState.Producing => "producing",
                CellState.ReadyToCollect => "ready",
                _ => "idle"
            };

            return $"zone1_{type}_{state}.png";
        }

        public static bool FileExistsAsAssetPath(string assetsPath)
        {
            if (string.IsNullOrEmpty(assetsPath) || !assetsPath.StartsWith("Assets/"))
                return false;
            var rel = assetsPath["Assets/".Length..].Replace('/', Path.DirectorySeparatorChar);
            var full = Path.Combine(Application.dataPath, rel);
            return File.Exists(full);
        }
    }
}
