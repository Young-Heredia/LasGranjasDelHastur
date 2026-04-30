using System.IO;
using UnityEngine;

namespace LasGranjasDelHastur.Zone2.Jose
{
    /// <summary>
    /// Jose: <c>zone2_lunargarden</c>, <c>zone2_cometmill</c>, etc. Si no existen, prueba nombres heredados
    /// (zone2_condenser, …) y luego el pack de Lucas.
    /// </summary>
    public static class Zone2CellSpritePathResolver
    {
        public const string JoseCellsDir = "Assets/02_Sprites/Jose/Zone2/Cells";

        public static string ResolveDistrict(Zone2DistrictType t)
        {
            var j = $"{JoseCellsDir}/{FileNameForJose(t)}";
            if (FileExistsAsAssetPath(j))
                return j;
            var legacy = $"{JoseCellsDir}/{LegacyJoseFileName(t)}";
            if (FileExistsAsAssetPath(legacy))
                return legacy;
            return Zone2DistrictPaths.LucasPackSpritePath(t);
        }

        public static string FileNameForJose(Zone2DistrictType t) =>
            t switch
            {
                Zone2DistrictType.LunarGarden => "zone2_lunargarden.png",
                Zone2DistrictType.CometMill => "zone2_cometmill.png",
                Zone2DistrictType.PlanetaryCore => "zone2_planetarycore.png",
                _ => "zone2_stellarincubator.png",
            };

        static string LegacyJoseFileName(Zone2DistrictType t) =>
            t switch
            {
                Zone2DistrictType.LunarGarden => "zone2_condenser.png",
                Zone2DistrictType.CometMill => "zone2_cultisttower.png",
                Zone2DistrictType.PlanetaryCore => "zone2_cursedmarket.png",
                _ => "zone2_yitharchive.png",
            };

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
