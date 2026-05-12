#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LasGranjasDelHastur.UI.Edwin.Editor
{
    /// <summary>Ensures <see cref="CosmicHarvestRhythmSceneBuilder"/> runs <see cref="CosmicHarvestRhythmSceneBuilder.EnsureEditorHierarchy"/>
    /// after opening or reloading scenes so the full mock UI (including RightPanels cards) exists in Edit Mode; save the scene to persist.</summary>
    [InitializeOnLoad]
    static class CosmicHarvestRhythmSceneEditHook
    {
        static CosmicHarvestRhythmSceneEditHook()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += QueueRefreshLoadedScenes;
        }

        static void QueueRefreshLoadedScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            RefreshAllLoadedEditModeScenes();
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode _)
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid())
                    return;
                RefreshBuildersInScene(scene);
            };
        }

        static void RefreshAllLoadedEditModeScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.isLoaded && s.IsValid())
                    RefreshBuildersInScene(s);
            }
        }

        static void RefreshBuildersInScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var builders = root.GetComponentsInChildren<CosmicHarvestRhythmSceneBuilder>(true);
                foreach (var b in builders)
                {
                    if (b != null && b.enabled)
                        b.EnsureEditorHierarchy();
                }
            }
        }
    }
}
#endif
