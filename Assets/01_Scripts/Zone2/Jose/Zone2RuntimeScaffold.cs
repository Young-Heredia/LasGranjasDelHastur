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
    }
}
