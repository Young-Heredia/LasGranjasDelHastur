#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LasGranjasDelHastur.Zone1.Editor
{
    /// <summary>
    /// Adds child <c>MusicP_Display</c> under <c>Mini_Game_Rhythm_Cosmic_Harvest</c> in Zone1_Dungeons:
    /// <see cref="SpriteRenderer"/> (material/sorting como el resto del mundo) + <see cref="MusicPLoopSpritePlayer"/>
    /// para animar los sub-sprites de MusicP.png desde Resources.
    /// Menú o batchmode -executeMethod.
    /// </summary>
    public static class Zone1MusicPDisplaySetup
    {
        const string ScenePath = "Assets/00_Scenes/Lucas/Zone1_Dungeons.unity";
        const string MiniGameObjectName = "Mini_Game_Rhythm_Cosmic_Harvest";
        const string DisplayChildName = "MusicP_Display";
        const string DefaultLitMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";

        const int SortingOrder = 20;
        /// <summary>Uniform scale — sprites are large in world units at default import.</summary>
        const float DisplayUniformScale = 0.12f;

        [MenuItem("Las Granjas/Zone1/Añadir MusicP animado al mini-game rhythm")]
        static void SetupFromMenu()
        {
            RunInternal(silent: false);
        }

        /// <summary>For CLI: <c>-batchmode -quit -projectPath ... -executeMethod LasGranjasDelHastur.Zone1.Editor.Zone1MusicPDisplaySetup.SetupFromBatchMode</c></summary>
        public static void SetupFromBatchMode()
        {
            RunInternal(silent: true);
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        static void RunInternal(bool silent)
        {
            if (!File.Exists(ScenePath))
            {
                Fail(silent, "Escena no encontrada:\n" + ScenePath);
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var miniGame = FindTransformInLoadedScenes(MiniGameObjectName);
            if (miniGame == null)
            {
                Fail(silent, $"No se encontró '{MiniGameObjectName}' en la escena abierta.");
                return;
            }

            RemoveExistingDisplayChild(miniGame);

            var materialPath = AssetDatabase.GUIDToAssetPath(DefaultLitMaterialGuid);
            var litMat = string.IsNullOrEmpty(materialPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            var displayGo = new GameObject(DisplayChildName);
            displayGo.transform.SetParent(miniGame, false);
            displayGo.transform.localPosition = Vector3.zero;
            displayGo.transform.localRotation = Quaternion.identity;
            displayGo.transform.localScale = Vector3.one * DisplayUniformScale;

            var sr = displayGo.AddComponent<SpriteRenderer>();
            sr.sprite = LoadFirstMusicPSprite();
            sr.color = Color.white;
            sr.sortingLayerID = 0;
            sr.sortingOrder = SortingOrder;
            if (litMat != null)
                sr.sharedMaterial = litMat;

            displayGo.AddComponent<MusicPLoopSpritePlayer>();

            Undo.RegisterCreatedObjectUndo(displayGo, "MusicP_Display");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!silent)
            {
                EditorUtility.DisplayDialog(
                    "MusicP Zone1",
                    $"Listo: hijo '{DisplayChildName}' bajo '{MiniGameObjectName}' con {nameof(MusicPLoopSpritePlayer)}.",
                    "OK");
            }
        }

        static Sprite LoadFirstMusicPSprite()
        {
            const string texturePath = "Assets/Resources/Edwin/CosmicHarvestRhythm/MusicP.png";
            if (!File.Exists(texturePath))
            {
                Debug.LogWarning("[Zone1MusicPDisplaySetup] Sin preview sprite en editor: " + texturePath);
                return null;
            }

            var objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(texturePath);
            foreach (var o in objs)
            {
                if (o is Sprite sp && sp.name == "MusicP_0")
                    return sp;
            }

            foreach (var o in objs)
            {
                if (o is Sprite sp)
                    return sp;
            }

            return null;
        }

        static void Fail(bool silent, string message)
        {
            Debug.LogError("[Zone1MusicPDisplaySetup] " + message);
            if (!silent)
                EditorUtility.DisplayDialog("MusicP Zone1", message, "OK");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }

        static Transform FindTransformInLoadedScenes(string objectName)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    var found = FindDeep(root.transform, objectName);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        static Transform FindDeep(Transform t, string objectName)
        {
            if (t.name == objectName)
                return t;
            foreach (Transform c in t)
            {
                var found = FindDeep(c, objectName);
                if (found != null)
                    return found;
            }

            return null;
        }

        static void RemoveExistingDisplayChild(Transform miniGame)
        {
            for (var i = miniGame.childCount - 1; i >= 0; i--)
            {
                var c = miniGame.GetChild(i);
                if (c.name != DisplayChildName)
                    continue;
                Object.DestroyImmediate(c.gameObject);
            }
        }
    }
}
#endif
