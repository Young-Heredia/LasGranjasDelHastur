using System;
using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone1.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone3
{
    public static class Zone3RuntimeScaffold
    {
        const string SceneName = "Zone3_Celestial";
        const string Zone3BackdropPath = "Assets/02_Sprites/Lucas/Zone3/NewBackGround/z3_background_black_space_stars_3840x2160.png";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != SceneName)
                return;
            EnsureSceneScaffold();
        }

        public static void EnsureSceneScaffold()
        {
            EnsureCamera();
            EnsureEventSystem();
            EnsureWorldHierarchy();
            EnsureUiHierarchy();
            EnsureSystemsHierarchy();
        }

        static void EnsureCamera()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<UnityEngine.Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.02f, 0.08f, 1f);
                go.AddComponent<AudioListener>();
            }

            if (cam != null && cam.GetComponent<LasGranjasDelHastur.Camera.CameraController2D>() == null)
                cam.gameObject.AddComponent<LasGranjasDelHastur.Camera.CameraController2D>();
        }

        static void EnsureEventSystem()
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
            if (world.GetComponent<Zone3SceneArtBinder>() == null)
                world.AddComponent<Zone3SceneArtBinder>();
            var floor = EnsureChild(world.transform, "Layer_Floor");
            var back = EnsureChild(world.transform, "Layer_WallsBack");
            var cell = EnsureChild(world.transform, "Layer_CellArea");
            var decor = EnsureChild(world.transform, "Layer_Decor");
            var fog = EnsureChild(world.transform, "Layer_Fog");
            var front = EnsureChild(world.transform, "Layer_WallsFront");
            var atmo = EnsureChild(world.transform, "Layer_Atmosphere");
            var slots = EnsureChild(world.transform, "CellSlotsRoot");
            EnsureZone3BackdropOnly(floor.transform, back.transform, cell.transform, decor.transform, fog.transform, front.transform, atmo.transform);

            var legacyBackdropLayer = world.transform.Find("Layer_Backdrop");
            if (legacyBackdropLayer != null)
                UnityEngine.Object.Destroy(legacyBackdropLayer.gameObject);
            var legacyNebulaLayer = world.transform.Find("Layer_Nebula");
            if (legacyNebulaLayer != null)
                UnityEngine.Object.Destroy(legacyNebulaLayer.gameObject);
        }

        static void EnsureZone3BackdropOnly(
            Transform floor,
            Transform wallsBack,
            Transform cellArea,
            Transform decor,
            Transform fog,
            Transform wallsFront,
            Transform atmosphere)
        {
            DestroyChildrenExcept(floor, keepName: null);
            DestroyChildrenExcept(cellArea, keepName: null);
            DestroyChildrenExcept(decor, keepName: null);
            DestroyChildrenExcept(fog, keepName: null);
            DestroyChildrenExcept(wallsFront, keepName: null);
            DestroyChildrenExcept(atmosphere, keepName: null);

            DestroyChildrenExcept(wallsBack, keepName: "CelestialBackdrop");
            CreateSpriteBackdrop(wallsBack, "CelestialBackdrop", Zone3BackdropPath, new Vector3(0f, 0.1f, 0f), new Vector2(40f, 24f), sortingOrder: -50);
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
                UnityEngine.Object.Destroy(ch.gameObject);
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

            var sprite = TryLoadSprite(assetPath);
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

        static void EnsureSystemsHierarchy()
        {
            var systems = GetOrCreateRoot("Systems");
            EnsureComponentUnderRoot<ResourceManager>(systems.transform, "ResourceManager");
            EnsureComponentUnderRoot<ProgressionManager>(systems.transform, "ProgressionManager");
            var cellManager = EnsureComponentUnderRoot<CellManager>(systems.transform, "CellManager");
            EnsureComponentUnderRoot<AssistantManager>(systems.transform, "AssistantManager");
            EnsureComponentUnderRoot<BuyerManager>(systems.transform, "BuyerManager");
            EnsureComponentUnderRoot<TaxManager>(systems.transform, "TaxManager");
            EnsureComponentUnderRoot<UIManager>(systems.transform, "UIManager");

            EnsureComponentUnderRoot<Zone3Manager>(systems.transform, "Zone3Manager");

            var slots = GameObject.Find("WorldRoot")?.transform.Find("CellSlotsRoot");
            if (slots != null && cellManager != null && cellManager.transform.parent != slots)
                cellManager.transform.SetParent(slots, worldPositionStays: false);
        }

        static T EnsureComponentUnderRoot<T>(Transform systemsRoot, string name) where T : Component
        {
            if (systemsRoot == null || !systemsRoot)
                return null;

            // Solo recursos/progresión son compartidos entre escenas.
            if (typeof(T) == typeof(ResourceManager) || typeof(T) == typeof(ProgressionManager))
            {
                var global = UnityEngine.Object.FindFirstObjectByType<T>();
                if (global != null)
                    return global;
            }

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

            // Reuse Zone 1's UI layout contract (names, anchors, sizes), so Zone3 can bind the same panels.
            EnsurePanel(ui.transform, "HUDCanvas", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(0f, 110f), new Color(0.05f, 0.05f, 0.06f, 0f), stretchHorizontal: true);
            EnsurePanel(ui.transform, "CellInfoPanel", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, 0f), new Vector2(360f, 420f), new Color(0.06f, 0.06f, 0.07f, 0.92f));
            EnsurePanel(ui.transform, "SalesPanel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-270f, 0f), new Vector2(520f, 420f), new Color(0.06f, 0.06f, 0.07f, 0.92f));
            EnsurePanel(ui.transform, "TaxAlertPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(520f, 360f), new Color(0.08f, 0.06f, 0.06f, 0.94f));
            EnsurePanel(ui.transform, "HoverInfoPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(320f, 80f), new Color(0.05f, 0.05f, 0.06f, 0.9f));

            SetActiveIfExists(ui.transform, "CellInfoPanel", false);
            SetActiveIfExists(ui.transform, "SalesPanel", false);
            SetActiveIfExists(ui.transform, "TaxAlertPanel", false);
            SetActiveIfExists(ui.transform, "HoverInfoPanel", false);
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

                var panelGo = panel.gameObject;
                if (panelGo == null)
                    return;

                if (!panelGo.TryGetComponent<RectTransform>(out var rt) || rt == null)
                    rt = panelGo.AddComponent<RectTransform>();
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

                if (!panelGo.TryGetComponent<Image>(out var img) || img == null)
                    img = panelGo.AddComponent<Image>();
                if (img == null)
                    return;
                img.color = color;
                img.raycastTarget = false;
            }
            catch (MissingReferenceException)
            {
            }
            catch (NullReferenceException)
            {
            }
            catch (Exception)
            {
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

        static void CreateSpriteBlock(Transform parent, string name, string spriteAssetPath, Vector3 pos, Vector3 scale, Color color, int sortingOrder)
        {
            if (parent == null || !parent || parent.Find(name) != null)
                return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            var sprite = TryLoadSprite(spriteAssetPath);
            sr.sprite = sprite ?? LasGranjasDelHastur.RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = sprite != null ? new Color(1f, 1f, 1f, color.a) : color;
            sr.sortingOrder = sortingOrder;

            if (sprite != null)
            {
                var b = sprite.bounds.size;
                if (b.x > 0.001f && b.y > 0.001f)
                    go.transform.localScale = new Vector3(scale.x / b.x, scale.y / b.y, 1f);
                else
                    go.transform.localScale = scale;
            }
            else
            {
                go.transform.localScale = scale;
            }
        }

        static void CreateSpriteBlock(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int sortingOrder) =>
            CreateSpriteBlock(parent, name, null, pos, scale, color, sortingOrder);

        static Sprite TryLoadSprite(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
            return null;
#endif
        }

        static GameObject GetOrCreateRoot(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go : new GameObject(name);
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
    }
}
