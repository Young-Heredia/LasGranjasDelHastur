using System.Linq;
using LasGranjasDelHastur.Creatures;
using UnityEditor;
using UnityEngine;

namespace LasGranjasDelHastur.Creatures.Editor
{
    public static class TindalosPrefabBaker
    {
        const string OutDir = "Assets/04_Prefabs/Jose/Creatures/Tindalos";

        [MenuItem("Las Granjas del Hastur/Creatures (Jose)/Bake Tíndalos (5 prefabs)", false, 50)]
        public static void BakeTindalos()
        {
            EnsureFolder(OutDir);
            (TindalosHoundKind k, string file)[] set =
            {
                (TindalosHoundKind.Pup, "Tindalos_Cachorro"),
                (TindalosHoundKind.Adolescent, "Tindalos_Adolescente"),
                (TindalosHoundKind.Adult, "Tindalos_Adulto"),
                (TindalosHoundKind.ElderDog, "Perro_Anciano"),
                (TindalosHoundKind.ShadowDog, "Perro_Sombrio"),
            };

            foreach (var (kind, file) in set)
            {
                var go = TindalosHoundBuilder.Build(null, kind, Vector3.zero, 0);
                var p = OutDir + "/" + file + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(go, p);
                Object.DestroyImmediate(go);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tíndalos] Prefabs: " + OutDir);
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
