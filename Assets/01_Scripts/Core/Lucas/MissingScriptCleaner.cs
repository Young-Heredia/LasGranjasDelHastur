using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace LasGranjasDelHastur.Core
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class MissingScriptCleaner
    {
#if UNITY_EDITOR
        static MissingScriptCleaner()
        {
            EditorSceneManager.sceneOpened += (_, _) => CleanActiveScene();
            EditorApplication.delayCall += CleanActiveScene;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void CleanActiveScene()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || Application.isPlaying)
                return;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            var removedAny = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null)
                    continue;
                if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root) > 0)
                    removedAny = true;
            }

            if (removedAny)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
                return;
            CleanAllLoadedObjectsInPlayMode();
        }

        static void CleanAllLoadedObjectsInPlayMode()
        {
            if (!Application.isPlaying)
                return;

            var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allGameObjects)
            {
                if (go == null)
                    continue;
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            }
        }
#endif
    }
}
