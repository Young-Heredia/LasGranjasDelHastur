#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1.Gacha.Editor
{
    public static class Zone1GachaPrefabMenu
    {
        const string PrefabPath04 = "Assets/04_Prefabs/Zone1/Zone1GachaPanel.prefab";
        const string PrefabPathResources = "Assets/Resources/Prefabs/Zone1/Zone1GachaPanel.prefab";

        [MenuItem("Las Granjas/Zone1/Generar prefab Gacha")]
        static void GenerateGachaPrefab()
        {
            const int fivePullCount = 5;
            var root = Zone1GachaUiRuntimeFactory.CreateOverlayRoot(fivePullCount, out _);
            root.SetActive(true);

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath04) ?? "Assets/04_Prefabs/Zone1");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath04);

            var resDir = Path.GetDirectoryName(PrefabPathResources);
            if (!string.IsNullOrEmpty(resDir))
                Directory.CreateDirectory(resDir);
            if (File.Exists(PrefabPathResources))
                AssetDatabase.DeleteAsset(PrefabPathResources);
            AssetDatabase.CopyAsset(PrefabPath04, PrefabPathResources);

            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Zone1 Gacha",
                "Prefab creado:\n" + PrefabPath04 + "\n\nCopia en Resources (carga en build sin asignar en escena):\n" + PrefabPathResources + "\n\nResources.Load: \"Prefabs/Zone1/Zone1GachaPanel\"",
                "OK");
        }
    }
}
#endif
