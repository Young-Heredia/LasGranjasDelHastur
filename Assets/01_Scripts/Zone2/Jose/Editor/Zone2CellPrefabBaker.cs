using System.IO;
using System.Linq;
using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone2;
using LasGranjasDelHastur.Zone2.Systems;
using UnityEditor;
using UnityEngine;

namespace LasGranjasDelHastur.Zone2.Jose.Editor
{
    /// <summary>Prefabs de celdas Z2 en <c>Assets/04_Prefabs/Jose/Zone2/Cells</c> (paralelo a FarmCellSlot bajo Jose).</summary>
    public static class Zone2CellPrefabBaker
    {
        const string OutDir = "Assets/04_Prefabs/Jose/Zone2/Cells";
        const string JoseCellDir = "Assets/02_Sprites/Jose/Zone2/Cells";
        const string LucasPackZone2 = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/Cells/Zone2";

        [MenuItem("Las Granjas del Hastur/Zone2 (Jose)/Copiar sprites celdas Z2 (Lucas pack → Jose)", false, 0)]
        public static void CopyLucasZone2CellPngsToJose()
        {
            EnsureFolder(JoseCellDir);
            var pairs = new[]
            {
                ("Zone2_Cell_CityCondenser.png", "zone2_lunargarden.png"),
                ("Zone2_Cell_CultistTower.png", "zone2_cometmill.png"),
                ("Zone2_Cell_CursedMarket.png", "zone2_planetarycore.png"),
                ("Zone2_Cell_YithArchive.png", "zone2_stellarincubator.png"),
            };
            var n = 0;
            foreach (var (srcName, dstName) in pairs)
            {
                var src = LucasPackZone2 + "/" + srcName;
                var dst = JoseCellDir + "/" + dstName;
                if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), src.Replace('/', Path.DirectorySeparatorChar))))
                    continue;
                if (AssetDatabase.LoadAssetAtPath<Object>(dst) != null)
                    continue;
                if (AssetDatabase.CopyAsset(src, dst))
                    n++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Jose/Zone2] Copiados {n} PNG a {JoseCellDir} (Huerto Lunar, Molino, Núcleo, Incubadora; prioridad en runtime).");
        }

        [MenuItem("Las Granjas del Hastur/Zone2 (Jose)/Bake celdas Z2 (prefabs)", false, 20)]
        public static void BakeZone2CellPrefabs()
        {
            EnsureFolder(OutDir);
            const int kSort = 40;

            GameObject CreateSlot(string name, Zone2DistrictType district)
            {
                var go = new GameObject(name);
                go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                go.AddComponent<BoxCollider2D>().size = Vector2.one;
                var sr = go.AddComponent<SpriteRenderer>();
                var p = Zone2CellSpritePathResolver.ResolveDistrict(district);
                sr.sprite = Zone1ArtProvider.LoadSprite(p);
                if (sr.sprite == null)
                    sr.color = new Color(0.35f, 0.4f, 0.45f, 1f);
                else
                    sr.color = new Color(0.55f, 0.55f, 0.60f, 1f);
                sr.sortingOrder = kSort;
                go.AddComponent<Zone2CellSlot>();
                go.AddComponent<Zone2CellEnergyVfx>();
                var drv = go.AddComponent<Zone2CellVisualDriver>();
                drv.Initialize(sr);
                return go;
            }

            {
                var go = CreateSlot("Z2_CellSlot_Base", Zone2DistrictType.LunarGarden);
                var path = OutDir + "/Z2_CellSlot_Base.prefab";
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
            }

            (string file, Zone2DistrictType t)[] variants =
            {
                ("Z2_HuertoLunar", Zone2DistrictType.LunarGarden),
                ("Z2_MolinoCometas", Zone2DistrictType.CometMill),
                ("Z2_NucleoPlanetario", Zone2DistrictType.PlanetaryCore),
                ("Z2_IncubadoraEstelar", Zone2DistrictType.StellarIncubator),
            };

            foreach (var (file, t) in variants)
            {
                var go = CreateSlot(file, t);
                var path = OutDir + "/" + file + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Jose/Zone2] Prefabs: " + OutDir + " — asigna Z2_CellSlot_Base.prefab a Zone2CellManager → Cell Slot Prefab (opcional).");
        }

        static void EnsureFolder(string assetsPath)
        {
            if (string.IsNullOrEmpty(assetsPath) || !assetsPath.StartsWith("Assets/"))
                return;
            if (AssetDatabase.IsValidFolder(assetsPath))
                return;

            var rel = assetsPath[("Assets/".Length)..];
            var segs = rel.Split('/').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            var cur = "Assets";
            foreach (var s in segs)
            {
                var next = cur + "/" + s;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, s);
                cur = next;
            }
        }
    }
}
