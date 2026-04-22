using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace LasGranjasDelHastur.Zone1
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class Zone1EditorScaffold
    {
#if UNITY_EDITOR
        static bool _applying;

        static Zone1EditorScaffold()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += EnsureActiveSceneScaffold;
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            TryEnsure(scene);
        }

        static void EnsureActiveSceneScaffold()
        {
            TryEnsure(SceneManager.GetActiveScene());
        }

        static void TryEnsure(Scene scene)
        {
            if (_applying)
                return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            if (!scene.IsValid() || !scene.isLoaded)
                return;
            if (scene.name != Zone1Bootstrap.SceneName)
                return;
            if (Application.isPlaying)
                return;

            _applying = true;
            try
            {
                Zone1Bootstrap.EnsureSceneScaffold(includeAudioManager: true);
                EnsureUiPlaceholders();

                EnsureArtTuner();
                EnsureEventSystemBridge();
                AutoWireZone1Manager();
                EnsureZone1ConfigAsset();
                EditorSceneManager.MarkSceneDirty(scene);
            }
            finally
            {
                _applying = false;
            }
        }

        static void EnsureUiPlaceholders()
        {
            var ui = GameObject.Find("UI");
            if (ui == null)
                ui = new GameObject("UI");
            if (ui == null)
                return;

            var canvas = ui.GetComponent<Canvas>();
            if (canvas == null)
                canvas = ui.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            if (ui.GetComponent<CanvasScaler>() == null)
                ui.AddComponent<CanvasScaler>();
            if (ui.GetComponent<GraphicRaycaster>() == null)
                ui.AddComponent<GraphicRaycaster>();

            EnsurePanel(ui.transform, "HUDCanvas", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(0, 110), new Color(0.05f, 0.05f, 0.06f, 0.9f), stretchHorizontal: true);
            EnsurePanel(ui.transform, "CellInfoPanel", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, 0f), new Vector2(360f, 420f), new Color(0.06f, 0.06f, 0.07f, 0.92f));
            EnsurePanel(ui.transform, "SalesPanel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-270f, 0f), new Vector2(520f, 420f), new Color(0.06f, 0.06f, 0.07f, 0.92f));
            EnsurePanel(ui.transform, "TaxAlertPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(520f, 360f), new Color(0.08f, 0.06f, 0.06f, 0.94f));
            EnsurePanel(ui.transform, "HoverInfoPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(320f, 80f), new Color(0.05f, 0.05f, 0.06f, 0.9f));

            // Keep only HUD visible in editor scaffold; runtime UIManager handles panel activation.
            SetActiveIfExists(ui.transform, "CellInfoPanel", false);
            SetActiveIfExists(ui.transform, "SalesPanel", false);
            SetActiveIfExists(ui.transform, "TaxAlertPanel", false);
            SetActiveIfExists(ui.transform, "HoverInfoPanel", false);
        }

        static void EnsureEventSystemBridge()
        {
            var es = EventSystem.current;
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
            }

            var bridge = es.GetComponent<LasGranjasDelHastur.EventSystemInputModuleBridge>();
            if (bridge == null)
                bridge = es.gameObject.AddComponent<LasGranjasDelHastur.EventSystemInputModuleBridge>();

            bridge.EnsureCorrectInputModule();
        }

        static void AutoWireZone1Manager()
        {
            var zone1 = Object.FindFirstObjectByType<Zone1Manager>();
            if (zone1 == null)
                return;
            zone1.AutoWireReferences();
            EditorUtility.SetDirty(zone1);
        }

        static void EnsureArtTuner()
        {
            var tuner = Object.FindFirstObjectByType<Zone1ArtTuner>();
            if (tuner != null)
                return;

            var systems = GameObject.Find("Systems");
            var go = new GameObject("Zone1ArtTuner");
            if (systems != null)
                go.transform.SetParent(systems.transform, false);
            go.AddComponent<Zone1ArtTuner>();
        }

        static void EnsureZone1ConfigAsset()
        {
            var zone1 = Object.FindFirstObjectByType<Zone1Manager>();
            if (zone1 == null)
                return;

            const string assetPath = "Assets/ScriptableObjects/Zone1/Zone1Config.asset";
            var config = AssetDatabase.LoadAssetAtPath<Zone1Config>(assetPath);
            if (config == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                    AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Zone1"))
                    AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Zone1");

                config = ScriptableObject.CreateInstance<Zone1Config>();
                AssetDatabase.CreateAsset(config, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var so = new SerializedObject(zone1);
            var configProp = so.FindProperty("zone1Config");
            if (configProp != null && configProp.objectReferenceValue != config)
            {
                configProp.objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(zone1);
            }
        }

        static void EnsurePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, bool stretchHorizontal = false)
        {
            if (parent == null)
                return;

            var panel = parent.Find(name);
            if (panel == null || !panel)
            {
                if (parent == null || !parent)
                    return;
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                panel = go.transform;
            }
            if (panel == null || !panel)
                return;

            var rt = panel.GetComponent<RectTransform>();
            if (rt == null)
                rt = panel.gameObject.AddComponent<RectTransform>();
            if (stretchHorizontal)
            {
                rt.anchorMin = new Vector2(0f, anchorMin.y);
                rt.anchorMax = new Vector2(1f, anchorMax.y);
                rt.sizeDelta = sizeDelta;
                rt.anchoredPosition = anchoredPosition;
            }
            else
            {
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.sizeDelta = sizeDelta;
                rt.anchoredPosition = anchoredPosition;
            }

            var image = panel != null ? panel.GetComponent<Image>() : null;
            if (image == null)
            {
                if (panel == null || !panel)
                    return;
                image = panel.gameObject.AddComponent<Image>();
            }
            image.color = color;
        }

        static void SetActiveIfExists(Transform parent, string childName, bool active)
        {
            var t = parent.Find(childName);
            if (t != null)
                t.gameObject.SetActive(active);
        }
#endif
    }
}

