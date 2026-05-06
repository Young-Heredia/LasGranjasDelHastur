using System.IO;
using UnityEngine;

namespace LasGranjasDelHastur.Zone3
{
    public static class Zone3CellSpritePathResolver
    {
        public enum Kind
        {
            LunarOrchard,
            CometMill,
            PlanetaryCore,
            StarIncubator
        }

        const string PackRoot = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/Cells/Zone3";

        public static string Resolve(Kind kind)
        {
            var path = $"{PackRoot}/{FileName(kind)}";
            return FileExistsAsAssetPath(path) ? path : $"{PackRoot}/Zone3_Cell_LunarOrchard.png";
        }

        static string FileName(Kind kind) =>
            kind switch
            {
                Kind.LunarOrchard => "Zone3_Cell_LunarOrchard.png",
                Kind.CometMill => "Zone3_Cell_CometMill.png",
                Kind.PlanetaryCore => "Zone3_Cell_PlanetaryCore.png",
                _ => "Zone3_Cell_StarIncubator.png",
            };

        static bool FileExistsAsAssetPath(string assetsPath)
        {
            if (string.IsNullOrEmpty(assetsPath) || !assetsPath.StartsWith("Assets/"))
                return false;
            var rel = assetsPath["Assets/".Length..].Replace('/', Path.DirectorySeparatorChar);
            var full = Path.Combine(Application.dataPath, rel);
            return File.Exists(full);
        }
    }
}

