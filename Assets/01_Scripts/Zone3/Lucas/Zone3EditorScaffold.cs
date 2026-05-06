using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone1.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace LasGranjasDelHastur.Zone3
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class Zone3EditorScaffold
    {
        const string SceneName = "Zone3_Celestial";
        const string Zone3BackdropPath = "Assets/02_Sprites/Lucas/Zone3/NewBackGround/z3_background_black_space_stars_3840x2160.png";

#if UNITY_EDITOR
        static bool _applying;

        static Zone3EditorScaffold()
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
            if (scene.name != SceneName || Application.isPlaying)
                return;

            _applying = true;
            try
            {
                EnsureCamera();
                EnsureEventSystemBridge();
                EnsureWorldHierarchy();
                EnsureUiHierarchy();
                EnsureSystemsHierarchy();
                EditorSceneManager.MarkSceneDirty(scene);
            }
            catch (MissingReferenceException)
            {
                // Scene objects can be recreated during editor delay calls.
                // Skip this pass and let next scaffold pass settle state.
            }
            catch (NullReferenceException)
            {
                // Defensive fallback for transient editor object lifetimes.
            }
            finally
            {
                _applying = false;
            }
        }
#endif

        static void EnsureCamera()
        {
            var cam = UnityEngine.Camera.main;
            if (cam != null)
                return;

            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var c = go.AddComponent<UnityEngine.Camera>();
            c.orthographic = true;
            c.orthographicSize = 5f;
            c.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.02f, 0.02f, 0.08f, 1f);
            go.AddComponent<AudioListener>();
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

        static void EnsureWorldHierarchy()
        {
            var world = GetOrCreateRoot("WorldRoot");
            var floor = EnsureChild(world.transform, "Layer_Floor");
            var wallsBack = EnsureChild(world.transform, "Layer_WallsBack");
            var cellArea = EnsureChild(world.transform, "Layer_CellArea");
            var decor = EnsureChild(world.transform, "Layer_Decor");
            var fog = EnsureChild(world.transform, "Layer_Fog");
            var wallsFront = EnsureChild(world.transform, "Layer_WallsFront");
            var atmosphere = EnsureChild(world.transform, "Layer_Atmosphere");
            var slots = EnsureChild(world.transform, "CellSlotsRoot");

            EnsureBackdropOnly(floor.transform, wallsBack.transform, cellArea.transform, decor.transform, fog.transform, wallsFront.transform, atmosphere.transform);
            EnsureGridGuides(slots.transform, new Color(0.42f, 0.30f, 0.62f, 0.18f));

            var legacyBackdropLayer = world.transform.Find("Layer_Backdrop");
            if (legacyBackdropLayer != null)
                UnityEngine.Object.DestroyImmediate(legacyBackdropLayer.gameObject);
            var legacyNebulaLayer = world.transform.Find("Layer_Nebula");
            if (legacyNebulaLayer != null)
                UnityEngine.Object.DestroyImmediate(legacyNebulaLayer.gameObject);
        }

        static void EnsureBackdropOnly(
            Transform floor,
            Transform wallsBack,
            Transform cellArea,
            Transform decor,
            Transform fog,
            Transform wallsFront,
            Transform atmosphere)
        {
            DestroyChildrenExcept(floor, null);
            DestroyChildrenExcept(cellArea, null);
            DestroyChildrenExcept(decor, null);
            DestroyChildrenExcept(fog, null);
            DestroyChildrenExcept(wallsFront, null);
            DestroyChildrenExcept(atmosphere, null);

            DestroyChildrenExcept(wallsBack, "CelestialBackdrop");
            CreateSpriteBackdrop(wallsBack, "CelestialBackdrop", Zone3BackdropPath, new Vector3(0f, 0.1f, 0f), new Vector2(40f, 24f), -50);
        }

        static void DestroyChildrenExcept(Transform parent, string keepName)
        {
            if (parent == null || !parent)
                return;
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var ch = parent.GetChild(i);
                if (ch == null)
                    continue;
                if (!string.IsNullOrEmpty(keepName) && ch.name == keepName)
                    continue;
                UnityEngine.Object.DestroyImmediate(ch.gameObject);
            }
        }

        static void CreateSpriteBackdrop(Transform parent, string name, string assetPath, Vector3 pos, Vector2 targetWorldSize, int sortingOrder)
        {
            if (parent == null || !parent)
                return;
            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = go.AddComponent<SpriteRenderer>();
#if UNITY_EDITOR
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
            var sprite = Zone1ArtProvider.LoadSprite(assetPath);
#endif
            sr.sprite = sprite ?? LasGranjasDelHastur.RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.sortingOrder = sortingOrder;
            sr.color = Color.white;

            if (sprite != null)
            {
                var b = sprite.bounds.size;
                if (b.x > 0.001f && b.y > 0.001f)
                    go.transform.localScale = new Vector3(targetWorldSize.x / b.x, targetWorldSize.y / b.y, 1f);
                else
                    go.transform.localScale = Vector3.one;
            }
            else
            {
                go.transform.localScale = new Vector3(targetWorldSize.x, targetWorldSize.y, 1f);
            }
        }

        static void EnsureUiHierarchy()
        {
            var ui = GetOrCreateRoot("UI");
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

            EnsurePanel(ui.transform, "HUDCanvas", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(0f, 148f), new Color(0.06f, 0.05f, 0.11f, 0f), true);
            EnsurePanel(ui.transform, "CellInfoPanel", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, 0f), new Vector2(360f, 430f), new Color(0.08f, 0.07f, 0.13f, 0.92f));
            EnsurePanel(ui.transform, "SalesPanel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-270f, 0f), new Vector2(530f, 430f), new Color(0.10f, 0.07f, 0.12f, 0.92f));
            EnsurePanel(ui.transform, "TaxAlertPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(540f, 360f), new Color(0.13f, 0.06f, 0.10f, 0.94f));
            EnsurePanel(ui.transform, "HoverInfoPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(340f, 86f), new Color(0.06f, 0.05f, 0.10f, 0.9f));

            SetActiveIfExists(ui.transform, "CellInfoPanel", false);
            SetActiveIfExists(ui.transform, "SalesPanel", false);
            SetActiveIfExists(ui.transform, "TaxAlertPanel", false);
            SetActiveIfExists(ui.transform, "HoverInfoPanel", false);
        }

        static void EnsureSystemsHierarchy()
        {
            var systems = GetOrCreateRoot("Systems");
            EnsureComponentUnderRoot<ResourceManager>(systems.transform, "ResourceManager");
            EnsureComponentUnderRoot<ProgressionManager>(systems.transform, "ProgressionManager");
            EnsureComponentUnderRoot<CellManager>(systems.transform, "CellManager");
            EnsureComponentUnderRoot<AssistantManager>(systems.transform, "AssistantManager");
            EnsureComponentUnderRoot<BuyerManager>(systems.transform, "BuyerManager");
            EnsureComponentUnderRoot<TaxManager>(systems.transform, "TaxManager");
            EnsureComponentUnderRoot<UIManager>(systems.transform, "UIManager");

            EnsureComponentUnderRoot<Zone3Manager>(systems.transform, "Zone3Manager");
        }

        static T EnsureComponentUnderRoot<T>(Transform systemsRoot, string name) where T : Component
        {
            if (systemsRoot == null || !systemsRoot)
                return null;
            var existing = systemsRoot.Find(name)?.gameObject;
            if (existing == null)
            {
                existing = new GameObject(name);
                existing.transform.SetParent(systemsRoot, false);
            }
            var c = existing.GetComponent<T>();
            if (c == null)
                c = existing.AddComponent<T>();
            return c;
        }

        static void EnsureGridGuides(Transform parent, Color color)
        {
            if (parent.Find("SlotGuides") != null)
                return;

            var root = new GameObject("SlotGuides");
            root.transform.SetParent(parent, false);
            var idx = 0;
            // Mantener coherencia con Zona 1: 4x3 (12 celdas)
            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 4; col++)
                {
                    var go = new GameObject($"CellGuide_{idx++:00}");
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = new Vector3(-3.3f + col * 2.2f, 1.8f - row * 2.2f, 0f);
                    go.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = LasGranjasDelHastur.RuntimeSpriteFactory.OpaqueWhiteSprite;
                    sr.color = color;
                    sr.sortingOrder = 2;
                }
            }
        }

        static void CreateSpriteBlock(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int sortingOrder)
        {
            if (parent.Find(name) != null)
                return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LasGranjasDelHastur.RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
        }

        static GameObject GetOrCreateRoot(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
                return go;
            return new GameObject(name);
        }

        static GameObject EnsureChild(Transform parent, string childName)
        {
            if (parent == null || !parent)
                return null;
            var child = parent.Find(childName);
            if (child != null)
                return child.gameObject;
            var go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            return go;
        }

        static void EnsurePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, bool stretchHorizontal = false)
        {
            try
            {
                if (parent == null || !parent)
                    return;

                var panel = parent.Find(name);
                if (panel == null)
                {
                    var go = new GameObject(name);
                    if (parent == null || !parent)
                        return;
                    go.transform.SetParent(parent, false);
                    panel = go.transform;
                }
                if (panel == null || !panel)
                    return;

                var rt = panel.GetComponent<RectTransform>();
                if (rt == null)
                {
                    if (panel == null || !panel)
                        return;
                    rt = panel.gameObject.AddComponent<RectTransform>();
                }
                if (rt == null)
                    return;

                if (stretchHorizontal)
                {
                    rt.anchorMin = new Vector2(0f, anchorMin.y);
                    rt.anchorMax = new Vector2(1f, anchorMax.y);
                }
                else
                {
                    rt.anchorMin = anchorMin;
                    rt.anchorMax = anchorMax;
                }

                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = sizeDelta;

                var img = panel.GetComponent<Image>();
                if (img == null)
                {
                    if (panel == null || !panel)
                        return;
                    img = panel.gameObject.AddComponent<Image>();
                }
                if (img == null)
                    return;
                img.color = color;
            }
            catch (MissingReferenceException)
            {
                // Parent/panel was destroyed mid-pass by editor refresh.
            }
        }

        static void SetActiveIfExists(Transform parent, string childName, bool active)
        {
            if (parent == null || !parent)
                return;
            var child = parent.Find(childName);
            if (child != null)
                child.gameObject.SetActive(active);
        }
    }
}
