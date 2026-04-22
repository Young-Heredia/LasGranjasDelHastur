using System.IO;
using LasGranjasDelHastur;
using LasGranjasDelHastur.Camera;
using LasGranjasDelHastur.Zone1.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LasGranjasDelHastur.Zone1
{
    public static class Zone1Bootstrap
    {
        public const string SceneName = "Zone1_Dungeons";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != SceneName)
                return;

            // Always ensure critical singletons/components even when scene is already scaffolded.
            EnsureAudioManagerExists();
            EnsureCameraSetup();
            EnsureArtTunerExists();
            EnsureEditorPlaceholderPanelsAreClosed();

            // Build missing structure only when absent.
            if (Object.FindFirstObjectByType<Zone1Manager>() == null || GameObject.Find("WorldRoot") == null || GameObject.Find("Systems") == null)
                EnsureSceneScaffold(includeAudioManager: false);
        }

        public static void EnsureSceneScaffold(bool includeAudioManager)
        {
            if (includeAudioManager)
                EnsureAudioManagerExists();
            EnsureCameraSetup();
            EnsureWorldPlaceholder();
            EnsureSystems();
        }

        static void EnsureAudioManagerExists()
        {
            if (Object.FindFirstObjectByType<AudioManager>() != null)
                return;
            var go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
        }

        static void EnsureCameraSetup()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
                return;
            if (cam.GetComponent<CameraController2D>() == null)
                cam.gameObject.AddComponent<CameraController2D>();
        }

        static void EnsureArtTunerExists()
        {
            if (Object.FindFirstObjectByType<Zone1ArtTuner>() != null)
                return;

            var systems = GameObject.Find("Systems");
            var go = new GameObject("Zone1ArtTuner");
            if (systems != null)
                go.transform.SetParent(systems.transform, false);
            go.AddComponent<Zone1ArtTuner>();
        }

        static void EnsureEditorPlaceholderPanelsAreClosed()
        {
            var ui = GameObject.Find("UI");
            if (ui == null)
                return;

            foreach (var panelName in new[] { "CellInfoPanel", "SalesPanel", "TaxAlertPanel", "HoverInfoPanel", "ActionHintPanel" })
            {
                var t = ui.transform.Find(panelName);
                if (t != null)
                    t.gameObject.SetActive(false);
            }
        }

        static void EnsureWorldPlaceholder()
        {
            if (GameObject.Find("WorldRoot") != null)
                return;

            var gridRoot = new GameObject("Grid / TilemapRoot");
            var worldRoot = new GameObject("WorldRoot");

            var cellSlots = new GameObject("CellSlotsRoot");
            cellSlots.transform.SetParent(worldRoot.transform, false);
            cellSlots.AddComponent<CellManager>();

            var props = new GameObject("DungeonPropsPlaceholderRoot");
            props.transform.SetParent(worldRoot.transform, false);

            CreateFloorTiled(props.transform);
            CreateFog(props.transform);
            CreateTorch(props.transform, new Vector3(-7.5f, 3.5f, 0));
            CreateTorch(props.transform, new Vector3(7.5f, 3.5f, 0));
            CreateRitualMark(props.transform, new Vector3(0f, 0f, 0f));
            CreateProp(props.transform, "zone1_prop_cage_broken.png", new Vector3(-6.4f, -3.2f, 0f), new Vector3(0.9f, 0.9f, 1f), 12);
            CreateProp(props.transform, "zone1_prop_chain_hanging.png", new Vector3(-3.8f, 3.4f, 0f), new Vector3(0.8f, 0.8f, 1f), 22);
            CreateProp(props.transform, "zone1_prop_dark_banner.png", new Vector3(3.8f, 3.2f, 0f), new Vector3(0.9f, 0.9f, 1f), 12);
            CreateProp(props.transform, "zone1_prop_dark_puddle.png", new Vector3(5.6f, -2.8f, 0f), new Vector3(0.9f, 0.9f, 1f), 4);
            CreateProp(props.transform, "zone1_prop_ritual_candles.png", new Vector3(0.5f, -3.4f, 0f), new Vector3(0.65f, 0.65f, 1f), 14);
            CreateProp(props.transform, "zone1_prop_sealed_door.png", new Vector3(0f, 4.4f, 0f), new Vector3(1.3f, 1.1f, 1f), 3);
            CreateAmbientOverlays(props.transform);
        }

        static void CreateFloorTiled(Transform parent)
        {
            var root = new GameObject("DungeonFloorRoot");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(0, 0, 5f);

            var tilePaths = new[]
            {
                "Assets/Sprites/Zone1/Tiles/zone1_floor_tile_01.png",
                "Assets/Sprites/Zone1/Tiles/zone1_floor_tile_02.png",
                "Assets/Sprites/Zone1/Tiles/zone1_floor_tile_03.png",
                "Assets/Sprites/Zone1/Tiles/zone1_floor_tile_04.png",
                "Assets/Sprites/Zone1/Tiles/zone1_floor_tile_05.png",
                "Assets/Sprites/Zone1/Tiles/zone1_floor_tile_06.png",
            };

            for (var y = -7; y <= 7; y++)
            {
                for (var x = -11; x <= 11; x++)
                {
                    var go = new GameObject($"Floor_{x}_{y}");
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = new Vector3(x, y * 0.95f, 0f);
                    go.transform.localScale = Vector3.one;

                    var sr = go.AddComponent<SpriteRenderer>();
                    var index = Mathf.Abs((x * 31 + y * 17) % tilePaths.Length);
                    var path = tilePaths[index];

                    if ((x + y) % 13 == 0)
                        path = "Assets/Sprites/Zone1/Tiles/zone1_floor_corrupt_transition.png";
                    if ((x - y) % 19 == 0)
                        path = "Assets/Sprites/Zone1/Tiles/zone1_floor_ritual_tile.png";

                    sr.sprite = Zone1ArtProvider.LoadSprite(path) ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
                    sr.color = Color.white;
                    sr.sortingOrder = 0;

                    // Sparse humidity layer for depth variation.
                    if ((x * 7 + y * 11) % 23 == 0)
                    {
                        var wet = new GameObject($"FloorWet_{x}_{y}");
                        wet.transform.SetParent(go.transform, false);
                        wet.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                        wet.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
                        var wetSr = wet.AddComponent<SpriteRenderer>();
                        wetSr.sprite = Zone1ArtProvider.LoadSprite("Assets/Sprites/Zone1/Overlays/zone1_overlay_humidity.png") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
                        wetSr.color = new Color(0.62f, 0.72f, 0.82f, 0.08f);
                        wetSr.sortingOrder = 1;
                    }
                }
            }
        }

        static void CreateFog(Transform parent)
        {
            var go = new GameObject("LowFog");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0, -3.5f, 1f);
            go.transform.localScale = new Vector3(12f, 4f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Zone1ArtProvider.LoadSprite("Assets/Sprites/Zone1/Overlays/zone1_overlay_humidity.png") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = new Color(0.7f, 0.75f, 0.75f, 0.18f);
            sr.sortingOrder = 50;

            var anim = go.AddComponent<SpriteSheetAnimator>();
            anim.Configure("Assets/Sprites/Zone1/Spritesheets/zone1_lowfog_sheet.png", 96, 48, 6f);
        }

        static void CreateTorch(Transform parent, Vector3 pos)
        {
            var go = new GameObject("TorchPlaceholder");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.4f, 1.2f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Zone1ArtProvider.LoadSprite("Assets/Sprites/Zone1/Props/zone1_prop_column_damaged.png") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = new Color(0.5f, 0.35f, 0.15f, 1f);
            sr.sortingOrder = 20;

            var flame = new GameObject("Flame");
            flame.transform.SetParent(go.transform, false);
            flame.transform.localPosition = new Vector3(0, 0.8f, 0);
            flame.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            var fsr = flame.AddComponent<SpriteRenderer>();
            fsr.sprite = Zone1ArtProvider.LoadSprite("Assets/Sprites/Zone1/Spritesheets/zone1_torch_sheet.png") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            fsr.color = new Color(1f, 0.85f, 0.25f, 1f);
            fsr.sortingOrder = 21;

            var anim = flame.AddComponent<SpriteSheetAnimator>();
            anim.Configure("Assets/Sprites/Zone1/Spritesheets/zone1_torch_sheet.png", 32, 32, 10f);
        }

        static void CreateRitualMark(Transform parent, Vector3 pos)
        {
            var go = new GameObject("RitualMarkPlaceholder");
            go.transform.SetParent(parent, false);
            go.transform.position = pos + new Vector3(0, -1.2f, 2f);
            go.transform.localScale = new Vector3(4.2f, 1.4f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Zone1ArtProvider.LoadSprite("Assets/Sprites/Zone1/Tiles/zone1_floor_ritual_tile.png") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = new Color(0.55f, 0.10f, 0.15f, 0.18f);
            sr.sortingOrder = 5;
        }

        static void CreateProp(Transform parent, string fileName, Vector3 pos, Vector3 scale, int sortingOrder)
        {
            var go = new GameObject($"Prop_{Path.GetFileNameWithoutExtension(fileName)}");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;

            var sh = new GameObject("Shadow");
            sh.transform.SetParent(go.transform, false);
            sh.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            sh.transform.localScale = new Vector3(0.85f, 0.32f, 1f);
            var shSr = sh.AddComponent<SpriteRenderer>();
            shSr.sprite = RuntimeSpriteFactory.OpaqueWhiteSprite;
            shSr.color = new Color(0f, 0f, 0f, 0.24f);
            shSr.sortingOrder = sortingOrder + Mathf.RoundToInt(-pos.y * 3f) - 1;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Zone1ArtProvider.LoadSprite($"Assets/Sprites/Zone1/Props/{fileName}") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = Color.white;
            sr.sortingOrder = sortingOrder + Mathf.RoundToInt(-pos.y * 3f);
        }

        static void CreateAmbientOverlays(Transform parent)
        {
            var runes = new GameObject("AmbientRunes");
            runes.transform.SetParent(parent, false);
            runes.transform.position = new Vector3(0f, 0f, 1.8f);
            runes.transform.localScale = new Vector3(5f, 4f, 1f);
            var runesSr = runes.AddComponent<SpriteRenderer>();
            runesSr.sprite = Zone1ArtProvider.LoadSprite("Assets/Sprites/Zone1/Overlays/zone1_overlay_runes.png");
            runesSr.color = new Color(1f, 1f, 1f, 0.35f);
            runesSr.sortingOrder = 6;

            var vignette = new GameObject("AmbientVignette");
            vignette.transform.SetParent(parent, false);
            vignette.transform.position = new Vector3(0f, 0f, 6f);
            vignette.transform.localScale = new Vector3(5f, 5f, 1f);
            var vigSr = vignette.AddComponent<SpriteRenderer>();
            vigSr.sprite = Zone1ArtProvider.LoadSprite("Assets/Sprites/Zone1/Overlays/zone1_overlay_vignette.png");
            vigSr.color = new Color(1f, 1f, 1f, 0.25f);
            vigSr.sortingOrder = 90;
        }

        static void EnsureSystems()
        {
            var systems = GameObject.Find("Systems");
            if (systems == null)
                systems = new GameObject("Systems");

            EnsureSystemComponent<ResourceManager>(systems.transform, "ResourceManager");
            EnsureSystemComponent<ProgressionManager>(systems.transform, "ProgressionManager");
            EnsureSystemComponent<AssistantManager>(systems.transform, "AssistantManager");
            EnsureSystemComponent<BuyerManager>(systems.transform, "BuyerManager");
            EnsureSystemComponent<TaxManager>(systems.transform, "TaxManager");
            EnsureSystemComponent<Zone1ArtTuner>(systems.transform, "Zone1ArtTuner");
            EnsureSystemComponent<UIManager>(systems.transform, "UIManager");
            EnsureSystemComponent<Zone1Manager>(systems.transform, "Zone1Manager");
        }

        static void EnsureSystemComponent<T>(Transform systemsRoot, string objectName) where T : Component
        {
            if (Object.FindFirstObjectByType<T>() != null)
                return;

            var go = systemsRoot.Find(objectName)?.gameObject;
            if (go == null)
            {
                go = new GameObject(objectName);
                go.transform.SetParent(systemsRoot, false);
            }

            if (go.GetComponent<T>() == null)
                go.AddComponent<T>();
        }
    }
}

