using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace LasGranjasDelHastur.Zone2.Jose
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class Zone2EditorScaffold
    {
        const string SceneName = "Zone2_Cities";

#if UNITY_EDITOR
        static bool _applying;

        static Zone2EditorScaffold()
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
            c.backgroundColor = new Color(0.02f, 0.03f, 0.06f, 1f);
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

            EnsureCityBaseFloor(floor.transform);
            EnsureCityBackStructures(wallsBack.transform);
            EnsureCellAreaGrid(cellArea.transform);
            EnsureCityDecor(decor.transform);
            EnsureCityFog(fog.transform);
            EnsureCityFrontRubble(wallsFront.transform);
            EnsureCityAtmosphere(atmosphere.transform);
            EnsureGridGuides(slots.transform, new Color(0.24f, 0.45f, 0.48f, 0.18f));
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

            EnsurePanel(ui.transform, "HUDCanvas", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(0f, 148f), new Color(0.04f, 0.08f, 0.10f, 0.88f), true);
            EnsurePanel(ui.transform, "CellInfoPanel", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, 0f), new Vector2(360f, 430f), new Color(0.06f, 0.11f, 0.12f, 0.92f));
            EnsurePanel(ui.transform, "SalesPanel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-270f, 0f), new Vector2(530f, 430f), new Color(0.09f, 0.10f, 0.08f, 0.92f));
            EnsurePanel(ui.transform, "TaxAlertPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(540f, 360f), new Color(0.11f, 0.07f, 0.05f, 0.94f));
            EnsurePanel(ui.transform, "HoverInfoPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(340f, 86f), new Color(0.05f, 0.08f, 0.09f, 0.9f));

            SetActiveIfExists(ui.transform, "CellInfoPanel", false);
            SetActiveIfExists(ui.transform, "SalesPanel", false);
            SetActiveIfExists(ui.transform, "TaxAlertPanel", false);
            SetActiveIfExists(ui.transform, "HoverInfoPanel", false);
        }

        static void EnsureSystemsHierarchy()
        {
            var systems = GetOrCreateRoot("Systems");
            EnsureChild(systems.transform, "ResourceManager");
            EnsureChild(systems.transform, "ProgressionManager");
            EnsureChild(systems.transform, "BuyerManager");
            EnsureChild(systems.transform, "TaxManager");
            EnsureChild(systems.transform, "UIManager");
            var zone2Go = systems.transform.Find("Zone2PrototypeGame")?.gameObject;
            if (zone2Go == null)
            {
                zone2Go = new GameObject("Zone2PrototypeGame");
                zone2Go.transform.SetParent(systems.transform, false);
            }

            if (zone2Go.GetComponent<Zone2PrototypeGame>() == null)
                zone2Go.AddComponent<Zone2PrototypeGame>();
        }

        static void EnsureGridGuides(Transform parent, Color color)
        {
            if (parent.Find("SlotGuides") != null)
                return;

            var root = new GameObject("SlotGuides");
            root.transform.SetParent(parent, false);
            var idx = 0;
            // Coherente con la rejilla: 6×5 = 30 celdas (misma lógica que Zona 1 en cantidad)
            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 4; col++)
                {
                    var go = new GameObject($"CellGuide_{idx++:00}");
                    go.transform.SetParent(root.transform, false);
                    // Misma grilla base de Zona 1 (origen/espaciado aproximados).
                    go.transform.localPosition = new Vector3(-3.3f + col * 2.2f, 1.8f - row * 2.2f, 0f);
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
            if (parent.Find("CondensedSkyline_Back") != null)
                return;
            CreateSpriteBlock(parent, "CondensedSkyline_Back", new Vector3(0f, 4.6f, 0f), new Vector3(22f, 5.5f, 1f), new Color(0.14f, 0.14f, 0.16f, 1f), -4);
            CreateSpriteBlock(parent, "CorruptTower_Left", new Vector3(-7.6f, 3.2f, 0f), new Vector3(2.2f, 5.2f, 1f), new Color(0.20f, 0.12f, 0.18f, 0.95f), -3);
            CreateSpriteBlock(parent, "CorruptTower_Right", new Vector3(7.6f, 3.3f, 0f), new Vector3(2.4f, 5.4f, 1f), new Color(0.20f, 0.12f, 0.18f, 0.95f), -3);
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
