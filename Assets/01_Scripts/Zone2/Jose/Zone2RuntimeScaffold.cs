using System;
using LasGranjasDelHastur.Zone2.Jose.Systems;
using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone1.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone2.Jose
{
    /// <summary>
    /// Construye en runtime la jerarquía mínima de Zone2 cuando la escena no fue scaffoldada en Editor.
    /// </summary>
    public static class Zone2RuntimeScaffold
    {
        const string SceneName = "Zone2_Cities";
        const string Zone2BackdropPath = "Assets/02_Sprites/Lucas/Zone2/fondo_ciudades_corrompidas_pixel_art_3072x2048.png";

        /// <summary>Multiplicador sobre el sprite del fondo: empuja la escena hacia azul oscuro y baja el contraste respecto a las celdas.</summary>
        static readonly Color Zone2BackdropCoolTint = new(0.42f, 0.50f, 0.64f, 1f);

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
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != SceneName)
                return;

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
                cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f, 1f);
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
            if (world.GetComponent<Zone2SceneArtBinder>() == null)
                world.AddComponent<Zone2SceneArtBinder>();

            var floor = EnsureChild(world.transform, "Layer_Floor");
            var wallsBack = EnsureChild(world.transform, "Layer_WallsBack");
            var cellArea = EnsureChild(world.transform, "Layer_CellArea");
            var decor = EnsureChild(world.transform, "Layer_Decor");
            var fog = EnsureChild(world.transform, "Layer_Fog");
            var wallsFront = EnsureChild(world.transform, "Layer_WallsFront");
            var atmosphere = EnsureChild(world.transform, "Layer_Atmosphere");
            var slots = EnsureChild(world.transform, "CellSlotsRoot");

            // New art direction: use a single authored background sprite instead of placeholder blocks.
            EnsureZone2BackdropOnly(
                floor.transform,
                wallsBack.transform,
                cellArea.transform,
                decor.transform,
                fog.transform,
                wallsFront.transform,
                atmosphere.transform);
            EnsureGridGuides(slots.transform, new Color(0.24f, 0.45f, 0.48f, 0.18f));

            // Zone2 now uses Zone1.CellManager to build and manage FarmCell slots.
        }

        static void EnsureZone2BackdropOnly(
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

            DestroyChildrenExcept(wallsBack, keepName: "CondensedSkyline_Back");
            CreateSpriteBackdrop(wallsBack, "CondensedSkyline_Back", Zone2BackdropPath, new Vector3(0f, 0.2f, 0f), new Vector2(38f, 24f), sortingOrder: -50, tintMultiply: Zone2BackdropCoolTint);
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

            // Reuse Zone 1's UI layout contract (names, anchors, sizes), so Zone2/Zone3 can share UI binders.
            EnsurePanel(ui.transform, "HUDCanvas", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(0f, 110f), new Color(0.05f, 0.05f, 0.06f, 0f), true);
            EnsurePanel(ui.transform, "CellInfoPanel", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, 0f), new Vector2(360f, 420f), new Color(0.06f, 0.06f, 0.07f, 0.92f));
            EnsurePanel(ui.transform, "SalesPanel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-270f, 0f), new Vector2(520f, 420f), new Color(0.06f, 0.06f, 0.07f, 0.92f));
            EnsurePanel(ui.transform, "TaxAlertPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(520f, 360f), new Color(0.08f, 0.06f, 0.06f, 0.94f));
            EnsurePanel(ui.transform, "HoverInfoPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(320f, 80f), new Color(0.05f, 0.05f, 0.06f, 0.9f));

            SetActiveIfExists(ui.transform, "CellInfoPanel", false);
            SetActiveIfExists(ui.transform, "SalesPanel", false);
            SetActiveIfExists(ui.transform, "TaxAlertPanel", false);
            SetActiveIfExists(ui.transform, "HoverInfoPanel", false);
        }

        static void EnsureSystemsHierarchy()
        {
            var systems = GetOrCreateRoot("Systems");
            // Use Zone 1 stack so we can reuse Zone1.UI.UIManager verbatim.
            EnsureComponentUnderRoot<ResourceManager>(systems.transform, "ResourceManager");
            EnsureComponentUnderRoot<ProgressionManager>(systems.transform, "ProgressionManager");
            var cellManager = EnsureComponentUnderRoot<CellManager>(systems.transform, "CellManager");
            EnsureComponentUnderRoot<AssistantManager>(systems.transform, "AssistantManager");
            EnsureComponentUnderRoot<BuyerManager>(systems.transform, "BuyerManager");
            EnsureComponentUnderRoot<TaxManager>(systems.transform, "TaxManager");
            EnsureComponentUnderRoot<UIManager>(systems.transform, "UIManager");

            EnsureComponentUnderRoot<Zone2Manager>(systems.transform, "Zone2Manager");

            // Critical: ensure the grid spawns under the world slot root.
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

        static void EnsureGridGuides(Transform parent, Color color)
        {
            if (parent == null || parent.Find("SlotGuides") != null)
                return;

            var root = new GameObject("SlotGuides");
            root.transform.SetParent(parent, false);
            var idx = 0;
            for (var row = 0; row < Zone2CellGridLayout.Rows; row++)
            {
                for (var col = 0; col < Zone2CellGridLayout.Columns; col++)
                {
                    var go = new GameObject($"CellGuide_{idx++:00}");
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = Zone2CellGridLayout.WorldSlotAnchor(col, row);
                    go.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = LasGranjasDelHastur.RuntimeSpriteFactory.OpaqueWhiteSprite;
                    sr.color = color;
                    sr.sortingOrder = 2;
                }
            }
        }

        static void EnsureCityBaseFloor(Transform parent)
        {
            if (parent.Find("CityFloorPlate") != null)
                return;
            CreateSpriteBlock(parent, "CityFloorPlate", new Vector3(0f, 0f, 0f), new Vector3(26f, 16f, 1f), new Color(0.08f, 0.12f, 0.14f, 1f), 0);
            CreateSpriteBlock(parent, "RitualStreets", new Vector3(0f, -0.2f, 0f), new Vector3(22f, 11.5f, 1f), new Color(0.12f, 0.17f, 0.15f, 0.5f), 1);
        }

        static void EnsureCityBackStructures(Transform parent)
        {
            // Main backdrop: use the new Zone2 background sprite directly (runtime-safe load).
            CreateSpriteBackdrop(parent, "CondensedSkyline_Back", Zone2BackdropPath, new Vector3(0f, 1.6f, 0f), new Vector2(32f, 20f), sortingOrder: -20, tintMultiply: Zone2BackdropCoolTint);
            CreateSpriteBlock(parent, "CorruptTower_Left", new Vector3(-7.6f, 3.2f, 0f), new Vector3(2.2f, 5.2f, 1f), new Color(0.20f, 0.12f, 0.18f, 0.95f), -3);
            CreateSpriteBlock(parent, "CorruptTower_Right", new Vector3(7.6f, 3.3f, 0f), new Vector3(2.4f, 5.4f, 1f), new Color(0.20f, 0.12f, 0.18f, 0.95f), -3);
        }

        static void CreateSpriteBackdrop(Transform parent, string name, string assetPath, Vector3 pos, Vector2 targetWorldSize, int sortingOrder, Color? tintMultiply = null)
        {
            if (parent == null)
                return;

            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = go.AddComponent<SpriteRenderer>();
            var sprite = Zone1ArtProvider.LoadSprite(assetPath);
            sr.sprite = sprite ?? LasGranjasDelHastur.RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.sortingOrder = sortingOrder;

            if (sprite != null)
            {
                sr.color = tintMultiply ?? Color.white;
                var b = sprite.bounds.size;
                if (b.x > 0.001f && b.y > 0.001f)
                    go.transform.localScale = new Vector3(targetWorldSize.x / b.x, targetWorldSize.y / b.y, 1f);
                else
                    go.transform.localScale = Vector3.one;
            }
            else
            {
                // Fallback tint if sprite missing.
                sr.color = new Color(0.14f, 0.14f, 0.16f, 1f);
                go.transform.localScale = new Vector3(targetWorldSize.x, targetWorldSize.y, 1f);
            }
        }

        static void EnsureCellAreaGrid(Transform parent)
        {
            if (parent.Find("UrbanCellField") != null)
                return;
            var field = new GameObject("UrbanCellField");
            field.transform.SetParent(parent, false);
            CreateSpriteBlock(field.transform, "FieldPlate", new Vector3(0f, -0.1f, 0f), new Vector3(15.5f, 9.8f, 1f), new Color(0.10f, 0.14f, 0.15f, 0.54f), 4);
            CreateSpriteBlock(field.transform, "FieldRitualRoad", new Vector3(0f, -0.1f, 0f), new Vector3(14.4f, 1.2f, 1f), new Color(0.18f, 0.13f, 0.09f, 0.45f), 5);
        }

        static void EnsureCityDecor(Transform parent)
        {
            if (parent.Find("UrbanDecor") != null)
                return;
            var decor = new GameObject("UrbanDecor");
            decor.transform.SetParent(parent, false);
            CreateSpriteBlock(decor.transform, "BrokenDistrict_Left", new Vector3(-9f, -2.8f, 0f), new Vector3(3.8f, 2.5f, 1f), new Color(0.16f, 0.15f, 0.13f, 0.95f), 8);
            CreateSpriteBlock(decor.transform, "BrokenDistrict_Right", new Vector3(9f, -2.6f, 0f), new Vector3(4.0f, 2.6f, 1f), new Color(0.16f, 0.15f, 0.13f, 0.95f), 8);
            CreateSpriteBlock(decor.transform, "CultRoadSigil", new Vector3(0f, -4.4f, 0f), new Vector3(4.4f, 1.3f, 1f), new Color(0.45f, 0.22f, 0.10f, 0.28f), 9);
        }

        static void EnsureCityFog(Transform parent)
        {
            if (parent.Find("UrbanFog") != null)
                return;
            CreateSpriteBlock(parent, "UrbanFog", new Vector3(0f, -2.6f, 0f), new Vector3(20f, 5.2f, 1f), new Color(0.48f, 0.50f, 0.45f, 0.12f), 24);
        }

        static void EnsureCityFrontRubble(Transform parent)
        {
            if (parent.Find("FrontRubble") != null)
                return;
            CreateSpriteBlock(parent, "FrontRubble", new Vector3(0f, -6.4f, 0f), new Vector3(24f, 2.0f, 1f), new Color(0.12f, 0.10f, 0.10f, 0.96f), 76);
        }

        static void EnsureCityAtmosphere(Transform parent)
        {
            if (parent.Find("UrbanVignette") != null)
                return;
            CreateSpriteBlock(parent, "UrbanVignette", new Vector3(0f, 0f, 0f), new Vector3(28f, 17f, 1f), new Color(0.03f, 0.04f, 0.06f, 0.22f), 88);
        }

        static void CreateSpriteBlock(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int sortingOrder)
        {
            if (parent == null || parent.Find(name) != null)
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

        static void EnsurePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, bool stretchHorizontal = false)
        {
            // Unity can destroy/recreate objects mid-frame during scene load; keep this resilient.
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

                // Avoid GetComponent<T>() on a Transform that can be destroyed between checks.
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
                // Parent/panel destroyed mid-pass (editor refresh or scene transition).
            }
            catch (NullReferenceException)
            {
                // Defensive fallback for transient lifetimes.
            }
            catch (Exception)
            {
                // Last-resort guard: Unity can throw other transient exceptions during domain reload/scene transitions.
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
