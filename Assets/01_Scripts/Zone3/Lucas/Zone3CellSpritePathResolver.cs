using System.IO;
using LasGranjasDelHastur.Zone1;
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

        const string Zone3CellsRoot = "Assets/02_Sprites/Lucas/Zone3/NewCells";

        public static string Resolve(Kind kind, CellState state, bool corrupted)
        {
            var path = $"{Zone3CellsRoot}/{FileName(kind, state, corrupted)}";
            if (FileExistsAsAssetPath(path))
                return path;
            return $"{Zone3CellsRoot}/z3_celestial_larva_moon_blocked.png";
        }

        static string FileName(Kind kind, CellState state, bool corrupted)
        {
            var suffix = corrupted ? "corrupt" : state switch
            {
                CellState.Blocked => "blocked",
                CellState.Producing => "producing",
                CellState.ReadyToCollect => "ready",
                _ => "idle"
            };
            var baseName = kind switch
            {
                Kind.LunarOrchard => "z3_celestial_larva_moon",
                Kind.CometMill => "z3_celestial_energy_sun",
                Kind.PlanetaryCore => "z3_celestial_memory_comet",
                _ => "z3_celestial_coin_asteroid",
            };
            return $"{baseName}_{suffix}.png";
        }

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

