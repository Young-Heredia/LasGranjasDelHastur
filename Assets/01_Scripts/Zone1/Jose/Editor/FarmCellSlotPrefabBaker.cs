using System.IO;
using System.Linq;
using LasGranjasDelHastur.Core;
using UnityEditor;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1.Editor
{
    /// <summary>
    /// Genera <c>Assets/04_Prefabs/Jose/Zone1/FarmCellSlot.prefab</c> y copia PNG de celdas Lucas → Jose (Base para retoque).
    /// </summary>
    public static class FarmCellSlotPrefabBaker
    {
        const string PrefabPath = "Assets/04_Prefabs/Jose/Zone1/FarmCellSlot.prefab";
        const string LucasCellDir = "Assets/02_Sprites/Lucas/Zone1/Cells";
        const string JoseCellDir = "Assets/02_Sprites/Jose/Zone1/Cells";

        [MenuItem("Las Granjas del Hastur/Zone1 (Jose)/Copiar sprites de celdas (Lucas → Jose)", false, 0)]
        public static void CopyLucasCellPngsToJose()
        {
            EnsureFolder(JoseCellDir);
            var projectRoot = Directory.GetCurrentDirectory();
            var absLucasDir = Path.Combine(projectRoot, "Assets", "02_Sprites", "Lucas", "Zone1", "Cells");
            if (!Directory.Exists(absLucasDir))
            {
                Debug.LogError("[Jose] No existe: " + absLucasDir);
                return;
            }

            var n = 0;
            var josePng = Path.Combine(projectRoot, "Assets", "02_Sprites", "Jose", "Zone1", "Cells");
            var files = Directory.GetFiles(absLucasDir, "zone1_*.png", SearchOption.TopDirectoryOnly);
            foreach (var absSrc in files)
            {
                var name = Path.GetFileName(absSrc);
                if (File.Exists(Path.Combine(josePng, name)))
                    continue;
                var srcAsset = LucasCellDir + "/" + name;
                var dstAsset = JoseCellDir + "/" + name;
                if (AssetDatabase.CopyAsset(srcAsset, dstAsset))
                    n++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Jose] Copiados {n} sprites a {JoseCellDir} (misma convención zone1_{{tipo}}_{{estado}}.png; edita los PNG a voluntad).");
        }

        [MenuItem("Las Granjas del Hastur/Zone1 (Jose)/Bake FarmCellSlot prefab", false, 10)]
        public static void BakeFarmCellSlotPrefab()
        {
            EnsureFolder("Assets/04_Prefabs/Jose/Zone1");

            const int kSort = 40;
            var go = new GameObject("FarmCellSlot");
            go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
            go.AddComponent<FarmCell>();
            go.AddComponent<WorldCellClickable>();
            var sr = go.AddComponent<SpriteRenderer>();
            var blockedPath = CellSpritePathResolver.ResolveByFileName("zone1_soulpit_blocked.png");
            sr.sprite = Zone1ArtProvider.LoadSprite(blockedPath) ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            sr.sortingOrder = kSort;

            FarmCellSlotHierarchy.Ensure(go.transform, kSort);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            if (prefab == null)
                Debug.LogError("[Jose] No se pudo guardar el prefab.");
            else
                Debug.Log("[Jose] Prefab: " + PrefabPath + " — Opcional: asígnalo a CellManager → Cell Slot Prefab en CellSlotsRoot.");
            AssetDatabase.SaveAssets();
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
