using System.IO;
using LasGranjasDelHastur.Zone2.Jose;
using LasGranjasDelHastur.Zone3;
using UnityEngine.SceneManagement;
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
        public const string LucasZone2BuildingsDir = "Assets/02_Sprites/Lucas/Zone2/Cells/Buildings";

        public static string ResolveForCell(FarmCell cell)
        {
            // When running Zone2/Zone3 using the Zone1 CellManager stack, route sprites to the zone packs.
            var scene = SceneManager.GetActiveScene().name;
            if (scene == "Zone2_Cities")
            {
                var fileName = GetZone2BuildingFileNameForCell(cell);
                var z2 = $"{LucasZone2BuildingsDir}/{fileName}";
                if (FileExistsAsAssetPath(z2))
                    return z2;
                var district = cell != null ? MapZone1CellTypeToZone2District(cell.CellType) : Zone2DistrictType.LunarGarden;
                return Zone2CellSpritePathResolver.ResolveDistrict(district);
            }
            if (scene == "Zone3_Celestial")
            {
                var kind = cell != null ? MapZone1CellTypeToZone3Kind(cell.CellType) : Zone3CellSpritePathResolver.Kind.LunarOrchard;
                var state = cell != null ? cell.State : CellState.Blocked;
                var corrupted = cell != null && cell.IsCorrupted;
                return Zone3CellSpritePathResolver.Resolve(kind, state, corrupted);
            }

            var name = GetFileNameForCell(cell);
            return ResolveByFileName(name);
        }

        static Zone2DistrictType MapZone1CellTypeToZone2District(Zone1CellType t) =>
            t switch
            {
                Zone1CellType.SoulPit => Zone2DistrictType.LunarGarden,
                Zone1CellType.EnergyWell => Zone2DistrictType.CometMill,
                Zone1CellType.EchoChamber => Zone2DistrictType.PlanetaryCore,
                _ => Zone2DistrictType.StellarIncubator
            };

        static Zone3CellSpritePathResolver.Kind MapZone1CellTypeToZone3Kind(Zone1CellType t) =>
            t switch
            {
                Zone1CellType.SoulPit => Zone3CellSpritePathResolver.Kind.LunarOrchard,
                Zone1CellType.EnergyWell => Zone3CellSpritePathResolver.Kind.CometMill,
                Zone1CellType.EchoChamber => Zone3CellSpritePathResolver.Kind.PlanetaryCore,
                _ => Zone3CellSpritePathResolver.Kind.StarIncubator
            };

        public static string ResolveByFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return $"{LucasCellsDir}/zone1_soulpit_blocked.png";
            if (!fileName.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                return $"{LucasCellsDir}/{fileName}";

            // Lucas agregó variantes con prefijo "N" para evitar choques de nombres.
            // Si existen, se prefieren sobre el nombre sin prefijo (solo para Lucas/Zone1/Cells).
            if (fileName.StartsWith("zone1_", System.StringComparison.OrdinalIgnoreCase))
            {
                var lucasN = $"{LucasCellsDir}/N{fileName}";
                if (FileExistsAsAssetPath(lucasN))
                    return lucasN;
            }

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

        /// <summary>
        /// Sprites authored for Zone2 corrupted buildings: <c>z2_building_{building}_{state}.png</c> under
        /// <see cref="LucasZone2BuildingsDir"/>.
        /// </summary>
        public static string GetZone2BuildingFileNameForCell(FarmCell cell)
        {
            if (cell == null)
                return "z2_building_souls_pit_blocked.png";

            var building = cell.CellType switch
            {
                Zone1CellType.SoulPit => "souls_pit",
                Zone1CellType.EnergyWell => "energy_reactor",
                Zone1CellType.EchoChamber => "memory_archive",
                Zone1CellType.BrokenAltar => "coin_mint",
                _ => "unstable_incubator"
            };

            var state = cell.IsCorrupted ? "corrupt" : cell.State switch
            {
                CellState.Blocked => "blocked",
                CellState.Producing => "producing",
                CellState.ReadyToCollect => "ready",
                _ => "idle"
            };

            return $"z2_building_{building}_{state}.png";
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
