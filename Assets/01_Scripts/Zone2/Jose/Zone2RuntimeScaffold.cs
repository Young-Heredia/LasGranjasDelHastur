using LasGranjasDelHastur.Zone2.Jose.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone2.Jose
{
    /// <summary>
    /// Construye en runtime la jerarquía mínima de Zone2 cuando la escena no fue scaffoldada en Editor
    /// (misma idea que <see cref="LasGranjasDelHastur.Zone3.Zone3RuntimeScaffold"/>).
    /// </summary>
    public static class Zone2RuntimeScaffold
    {
        const string SceneName = "Zone2_Cities";
using LasGranjasDelHastur.Camera;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace LasGranjasDelHastur.Zone2.Jose
{
    public static class Zone2RuntimeScaffold
    {
        const string SceneName = "Zone2_Cities";
        const string PackRoot = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/";
        const string Sprite_Z2_Backplate = PackRoot + "Zones/Zone2_Cities/Zone2_BackgroundPlate_CityFarm.png";
        const string Sprite_Z2_Backdrop = PackRoot + "Zones/Zone2_Cities/Zone2_Backdrop_CollapsedSkyline.png";
        const string Sprite_Z2_FrontRubble = PackRoot + "Zones/Zone2_Cities/Zone2_Decor_RubbleFront.png";
        const string Sprite_Z2_FogSheet = PackRoot + "Zones/Zone2_Cities/Zone2_FogOverlay_City_Sheet.png";

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
            EnsureCamera();
            EnsureEventSystem();
            EnsureWorldHierarchy();
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
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f, 1f);
                go.AddComponent<AudioListener>();
            }

            if (cam != null && cam.GetComponent<LasGranjasDelHastur.Camera.CameraController2D>() == null)
                cam.gameObject.AddComponent<LasGranjasDelHastur.Camera.CameraController2D>();
                cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.015f, 0.02f, 0.07f, 1f);
                go.AddComponent<AudioListener>();
            }

            if (cam != null && cam.GetComponent<CameraController2D>() == null)
                cam.gameObject.AddComponent<CameraController2D>();

            if (cam != null && cam.GetComponent<CameraController2D>() is { } cc2d)
            {
                cam.orthographicSize = 7.2f;
                cc2d.SetBounds(new Vector2(-28f, -16f), new Vector2(28f, 16f));
            }

            if (cam != null && SceneManager.GetActiveScene().name == SceneName)
                cam.backgroundColor = new Color(0.015f, 0.02f, 0.07f, 1f);
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

            EnsureCityBaseFloor(floor.transform);
            EnsureCityBackStructures(wallsBack.transform);
            EnsureCellAreaGrid(cellArea.transform);
            EnsureCityDecor(decor.transform);
            EnsureCityFog(fog.transform);
            EnsureCityFrontRubble(wallsFront.transform);
            EnsureCityAtmosphere(atmosphere.transform);
            EnsureGridGuides(slots.transform, new Color(0.24f, 0.45f, 0.48f, 0.18f));

            if (slots != null && slots.GetComponent<Zone2CellManager>() == null)
                slots.AddComponent<Zone2CellManager>();
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
            var floor = EnsureChild(world.transform, "Layer_Floor");
            var back = EnsureChild(world.transform, "Layer_WallsBack");
            var cell = EnsureChild(world.transform, "Layer_CellArea");
            var decor = EnsureChild(world.transform, "Layer_Decor");
            var fog = EnsureChild(world.transform, "Layer_Fog");
            var front = EnsureChild(world.transform, "Layer_WallsFront");
            var atmo = EnsureChild(world.transform, "Layer_Atmosphere");
            var slots = EnsureChild(world.transform, "CellSlotsRoot");
            if (slots != null && slots.GetComponent<LasGranjasDelHastur.Zone2.Jose.Systems.Zone2CellManager>() == null)
                slots.AddComponent<LasGranjasDelHastur.Zone2.Jose.Systems.Zone2CellManager>();

            // Suelo y horizonte más amplios que Zona 1; rejilla 6×5 ≈ 12.8×7.7 unidades (espacio de celdas alineado a Z1).
            CreateSpriteBlock(floor.transform, "CityFloorPlate", Sprite_Z2_Backplate, new Vector3(0f, 0f, 0f), new Vector3(38f, 24f, 1f), new Color(0.08f, 0.12f, 0.14f, 1f), 0);
            CreateSpriteBlock(back.transform, "CondensedSkyline_Back", Sprite_Z2_Backdrop, new Vector3(0f, 4.8f, 0f), new Vector3(30f, 6.2f, 1f), new Color(0.14f, 0.14f, 0.16f, 1f), -4);
            CreateSpriteBlock(cell.transform, "UrbanCellField", new Vector3(0f, -0.1f, 0f), new Vector3(17.4f, 10.2f, 1f), new Color(0.10f, 0.14f, 0.15f, 0.54f), 4);
            CreateSpriteBlock(decor.transform, "UrbanDecor", new Vector3(0f, -4.0f, 0f), new Vector3(10f, 2.4f, 1f), new Color(0.28f, 0.18f, 0.11f, 0.22f), 9);
            CreateSpriteBlock(fog.transform, "UrbanFog", Sprite_Z2_FogSheet, new Vector3(0f, -2.4f, 0f), new Vector3(26f, 5.6f, 1f), new Color(0.48f, 0.50f, 0.45f, 0.12f), 24);
            CreateSpriteBlock(front.transform, "FrontRubble", Sprite_Z2_FrontRubble, new Vector3(0f, -6.6f, 0f), new Vector3(30f, 2.2f, 1f), new Color(0.12f, 0.10f, 0.10f, 0.96f), 76);
            CreateSpriteBlock(atmo.transform, "UrbanVignette", new Vector3(0f, 0f, 0f), new Vector3(36f, 24f, 1f), new Color(0.02f, 0.03f, 0.08f, 0.32f), 88);

            if (world != null)
            {
                Zone2CityDestroyedMap.PopulateIfMissing(world.transform, Sprite_Z2_FogSheet);
                Zone2SpaceScenery.PopulateIfMissing(world.transform);
                TindalosZ2MapTest.PlacePrototypesIfMissing(world.transform);
            }
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
            if (parent == null || parent.Find("SlotGuides") != null)
                return;

            var root = new GameObject("SlotGuides");
            root.transform.SetParent(parent, false);
            var idx = 0;
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
            var zone2 = systems.transform.Find("Zone2PrototypeGame")?.gameObject;
            if (zone2 == null)
            {
                zone2 = new GameObject("Zone2PrototypeGame");
                zone2.transform.SetParent(systems.transform, false);
            }

            if (zone2.GetComponent<Zone2PrototypeGame>() == null)
                zone2.AddComponent<Zone2PrototypeGame>();
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

            // Ajuste para que el sprite ocupe el mismo rectángulo que antes.
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
            // En runtime build esto no existe; ahí caemos al fallback de color.
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

        static void EnsurePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, bool stretchHorizontal = false)
        {
            if (parent == null || !parent)
                return;

            var panel = parent.Find(name);
            if (panel == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                panel = go.transform;
            }

            var rt = panel.GetComponent<RectTransform>();
            if (rt == null)
                rt = panel.gameObject.AddComponent<RectTransform>();

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
                img = panel.gameObject.AddComponent<Image>();
            img.color = color;
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
