using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace LasGranjasDelHastur.Zone3
{
    public static class Zone3RuntimeScaffold
    {
        const string SceneName = "Zone3_Celestial";
        const string PackRoot = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/";
        const string Sprite_Z3_Backplate = PackRoot + "Zones/Zone3_Celestial/Zone3_BackgroundPlate_CelestialFarm.png";
        const string Sprite_Z3_Backdrop = PackRoot + "Zones/Zone3_Celestial/Zone3_Backdrop_DeepSpace.png";
        const string Sprite_Z3_FrontPlatforms = PackRoot + "Zones/Zone3_Celestial/Zone3_Decor_ForegroundPlatforms.png";
        const string Sprite_Z3_MistSheet = PackRoot + "Zones/Zone3_Celestial/Zone3_AstralMistOverlay_Sheet.png";

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
            if (slots != null && slots.GetComponent<LasGranjasDelHastur.Zone3.Systems.Zone3CellManager>() == null)
                slots.AddComponent<LasGranjasDelHastur.Zone3.Systems.Zone3CellManager>();

            CreateSpriteBlock(floor.transform, "AstralPlane", Sprite_Z3_Backplate, new Vector3(0f, 0f, 0f), new Vector3(26f, 16f, 1f), new Color(0.03f, 0.03f, 0.09f, 1f), 0);
            CreateSpriteBlock(back.transform, "DeepSpaceBackdrop", Sprite_Z3_Backdrop, new Vector3(0f, 4.8f, 0f), new Vector3(24f, 6f, 1f), new Color(0.05f, 0.04f, 0.11f, 1f), -4);
            CreateSpriteBlock(cell.transform, "CelestialField", new Vector3(0f, -0.1f, 0f), new Vector3(15.5f, 9.8f, 1f), new Color(0.10f, 0.08f, 0.18f, 0.54f), 4);
            CreateSpriteBlock(decor.transform, "AstralDecor", new Vector3(0f, -4.1f, 0f), new Vector3(8.6f, 2.2f, 1f), new Color(0.42f, 0.30f, 0.68f, 0.20f), 9);
            CreateSpriteBlock(fog.transform, "AstralMist", Sprite_Z3_MistSheet, new Vector3(0f, -2.3f, 0f), new Vector3(21f, 5.2f, 1f), new Color(0.44f, 0.38f, 0.62f, 0.10f), 24);
            CreateSpriteBlock(front.transform, "ForegroundPlatforms", Sprite_Z3_FrontPlatforms, new Vector3(0f, -6.3f, 0f), new Vector3(24f, 1.9f, 1f), new Color(0.08f, 0.07f, 0.15f, 0.98f), 76);
            CreateSpriteBlock(atmo.transform, "AstralVignette", new Vector3(0f, 0f, 0f), new Vector3(28f, 17f, 1f), new Color(0.04f, 0.02f, 0.08f, 0.24f), 88);
        }

        static void EnsureSystemsHierarchy()
        {
            var systems = GetOrCreateRoot("Systems");
            var zone3 = systems.transform.Find("Zone3PrototypeGame")?.gameObject;
            if (zone3 == null)
            {
                zone3 = new GameObject("Zone3PrototypeGame");
                zone3.transform.SetParent(systems.transform, false);
            }

            if (zone3.GetComponent<Zone3PrototypeGame>() == null)
                zone3.AddComponent<Zone3PrototypeGame>();
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
