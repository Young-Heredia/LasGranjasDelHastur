using System.Collections;
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

        /// <summary>
        /// Fuentes + cultistas (idempotente). Llamar tras cargar Zone1: <see cref="GameObject.Find"/> puede apuntar a raíces de otra escena o DDOL.
        /// </summary>
        public static void EnsureZone1RuntimeDecor() => EnsureDecorExtrasExist();

        static GameObject FindRootInActiveScene(string objectName)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                return null;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go != null && go.name == objectName)
                    return go;
            }

            return null;
        }

        /// <summary>
        /// <see cref="Transform.Find"/> no incluye hijos inactivos; al recargar Zone1 a veces quedan capas desactivadas y el decor parece "vacío".
        /// </summary>
        static Transform FindChildByNameIncludingInactive(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName))
                return null;
            for (var i = 0; i < parent.childCount; i++)
            {
                var ch = parent.GetChild(i);
                if (ch != null && ch.name == childName)
                    return ch;
            }

            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InstallZone1DecorSceneHook()
        {
            SceneManager.sceneLoaded -= OnZone1SceneLoadedDeferredDecor;
            SceneManager.sceneLoaded += OnZone1SceneLoadedDeferredDecor;
        }

        static void OnZone1SceneLoadedDeferredDecor(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneName)
                return;
            var host = new GameObject("__Zone1DecorDeferredEnsure");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<Zone1DecorDeferredEnsureHost>();
        }

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
            EnsureDecorExtrasExist();

            // Build missing structure only when absent.
            if (Object.FindFirstObjectByType<Zone1Manager>() == null || FindRootInActiveScene("WorldRoot") == null || FindRootInActiveScene("Systems") == null)
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

            var systemsGo = FindRootInActiveScene("Systems");
            var go = new GameObject("Zone1ArtTuner");
            if (systemsGo != null)
                go.transform.SetParent(systemsGo.transform, false);
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
            var worldRoot = FindRootInActiveScene("WorldRoot");
            if (worldRoot == null)
                worldRoot = new GameObject("WorldRoot");

            var floorRoot = GetOrCreateChild(worldRoot.transform, "Layer_Floor");
            var wallsBackRoot = GetOrCreateChild(worldRoot.transform, "Layer_WallsBack");
            var cellAreaRoot = GetOrCreateChild(worldRoot.transform, "Layer_CellArea");
            var cellSlots = GetOrCreateChild(worldRoot.transform, "CellSlotsRoot");
            var decorRoot = GetOrCreateChild(worldRoot.transform, "Layer_Decor");
            var fogRoot = GetOrCreateChild(worldRoot.transform, "Layer_Fog");
            var wallsFrontRoot = GetOrCreateChild(worldRoot.transform, "Layer_WallsFront");
            var atmosphereRoot = GetOrCreateChild(worldRoot.transform, "Layer_Atmosphere");

            if (cellSlots.GetComponent<CellManager>() == null)
                cellSlots.AddComponent<CellManager>();

            CreateFloorTiled(floorRoot.transform);
            CreateBrokenWalls(wallsBackRoot.transform, wallsFrontRoot.transform);
            CreateCellFieldLayout(cellAreaRoot.transform);
            CreateDungeonDecor(decorRoot.transform);
            CreateFog(fogRoot.transform);
            CreateAmbientOverlays(atmosphereRoot.transform);
        }

        static void EnsureDecorExtrasExist()
        {
            var worldRoot = FindRootInActiveScene("WorldRoot");
            if (worldRoot == null)
                return;

            var decor = FindChildByNameIncludingInactive(worldRoot.transform, "Layer_Decor");
            if (decor == null)
                return;

            if (!decor.gameObject.activeSelf)
                decor.gameObject.SetActive(true);

            var dungeonDecor = FindChildByNameIncludingInactive(decor, "DungeonDecor");
            if (dungeonDecor == null)
                CreateDungeonDecor(decor);

            dungeonDecor = FindChildByNameIncludingInactive(decor, "DungeonDecor");
            if (dungeonDecor == null)
                return;

            if (!dungeonDecor.gameObject.activeSelf)
                dungeonDecor.gameObject.SetActive(true);

            EnsureFountains(dungeonDecor);
            EnsureCultists(dungeonDecor);
        }

        static void EnsureCultists(Transform dungeonDecor)
        {
            if (dungeonDecor == null)
                return;

            // Controller singleton under DungeonDecor.
            var ctrlTf = FindChildByNameIncludingInactive(dungeonDecor, "CultistEasterEggController");
            Zone1CultistEasterEggController ctrl = null;
            if (ctrlTf != null)
                ctrl = ctrlTf.GetComponent<Zone1CultistEasterEggController>();
            if (ctrl == null)
            {
                var goCtrl = ctrlTf != null ? ctrlTf.gameObject : new GameObject("CultistEasterEggController");
                goCtrl.transform.SetParent(dungeonDecor, false);
                ctrl = goCtrl.GetComponent<Zone1CultistEasterEggController>();
                if (ctrl == null)
                    ctrl = goCtrl.AddComponent<Zone1CultistEasterEggController>();
            }

            ctrl.PruneDestroyedCultists();

            // 8 cultists placed around the map (avoid covering the grid center).
            var placements = new (string name, Vector3 pos, Vector3 scale, int sorting, bool isBook)[]
            {
                ("Cultist_1", new Vector3(-11.2f, 3.9f, 0f), new Vector3(0.95f, 0.95f, 1f), 20, false),
                ("Cultist_2", new Vector3(11.1f, 3.7f, 0f), new Vector3(-0.95f, 0.95f, 1f), 20, true),
                ("Cultist_3", new Vector3(-11.6f, -3.8f, 0f), new Vector3(0.9f, 0.9f, 1f), 75, true),
                ("Cultist_4", new Vector3(11.6f, -3.6f, 0f), new Vector3(-0.9f, 0.9f, 1f), 75, false),
                ("Cultist_5", new Vector3(-6.9f, 5.15f, 0f), new Vector3(0.85f, 0.85f, 1f), 16, false),
                ("Cultist_6", new Vector3(6.9f, 5.15f, 0f), new Vector3(-0.85f, 0.85f, 1f), 16, true),
                ("Cultist_7", new Vector3(-6.7f, -6.1f, 0f), new Vector3(0.82f, 0.82f, 1f), 82, true),
                ("Cultist_8", new Vector3(6.7f, -6.1f, 0f), new Vector3(-0.82f, 0.82f, 1f), 82, false),
            };

            foreach (var p in placements)
                EnsureOneCultist(dungeonDecor, ctrl, p.name, p.pos, p.scale, p.sorting, p.isBook);

            ctrl.PruneDestroyedCultists();
        }

        static void EnsureOneCultist(Transform parent, Zone1CultistEasterEggController ctrl, string name, Vector3 pos, Vector3 scale, int sortingOrder, bool isBook)
        {
            var existing = FindChildByNameIncludingInactive(parent, name);
            if (existing != null)
            {
                var click = existing.GetComponent<Zone1CultistClickable>();
                var existingSr = existing.GetComponent<SpriteRenderer>();
                if (click != null && existingSr != null && existingSr.sprite != null && click.controller == ctrl)
                {
                    if (!existing.gameObject.activeSelf)
                        existing.gameObject.SetActive(true);
                    return;
                }

                Object.Destroy(existing.gameObject);
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;

            var sr = go.AddComponent<SpriteRenderer>();
            var spritePath = isBook
                ? "Assets/02_Sprites/Lucas/Zone1/Props/zone1_cultist_yellow_book_64.png"
                : "Assets/02_Sprites/Lucas/Zone1/Props/zone1_cultist_yellow_staff_64.png";
            sr.sprite = Zone1ArtProvider.LoadSprite(spritePath) ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = Color.white;
            sr.sortingOrder = sortingOrder + Mathf.RoundToInt(-pos.y * 3f);

            // Hitbox for clicks.
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 1.2f);
            col.offset = new Vector2(0f, 0.15f);

            var clickable = go.AddComponent<Zone1CultistClickable>();
            clickable.controller = ctrl;

            // Animator added but disabled by default; controller enables on easter.
            var anim = go.AddComponent<SpriteSheetAnimator>();
            anim.enabled = false;

            ctrl?.RegisterCultist(go.transform, isBook);
        }

        static void CreateFloorTiled(Transform parent)
        {
            if (parent.Find("DungeonFloorRoot") != null)
                return;

            var root = new GameObject("DungeonFloorRoot");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(0, 0, 5f);

            var tilePaths = new[]
            {
                "Assets/02_Sprites/Lucas/Zone1/Tiles/NWzone1_floor_tile_01.png",
                "Assets/02_Sprites/Lucas/Zone1/Tiles/NWzone1_floor_tile_02.png",
                "Assets/02_Sprites/Lucas/Zone1/Tiles/NWzone1_floor_tile_03.png",
                "Assets/02_Sprites/Lucas/Zone1/Tiles/NWzone1_floor_tile_04.png",
                "Assets/02_Sprites/Lucas/Zone1/Tiles/NWzone1_floor_tile_05.png",
                "Assets/02_Sprites/Lucas/Zone1/Tiles/NWzone1_floor_tile_06.png",
            };

            for (var y = -10; y <= 10; y++)
            {
                for (var x = -13; x <= 13; x++)
                {
                    var go = new GameObject($"Floor_{x}_{y}");
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = new Vector3(x, y * 0.95f, 0f);
                    go.transform.localScale = Vector3.one;

                    var sr = go.AddComponent<SpriteRenderer>();
                    var index = Mathf.Abs((x * 31 + y * 17) % tilePaths.Length);
                    var path = tilePaths[index];

                    if ((x + y) % 13 == 0)
                        path = "Assets/02_Sprites/Lucas/Zone1/Tiles/Nwzone1_floor_corrupt_transition.png";
                    if ((x - y) % 19 == 0)
                        path = "Assets/02_Sprites/Lucas/Zone1/Tiles/Nwzone1_floor_ritual_tile.png";

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
                        wetSr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/Overlays/zone1_overlay_humidity.png") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
                        wetSr.color = new Color(0.62f, 0.72f, 0.82f, 0.08f);
                        wetSr.sortingOrder = 1;
                    }
                }
            }
        }

        static void CreateFog(Transform parent)
        {
            CreateFogBand(parent, "LowFog_Front", new Vector3(0f, -5.8f, 1f), new Vector3(13f, 3.4f, 1f), new Color(0.72f, 0.76f, 0.74f, 0.2f), 70, 6f);
            CreateFogBand(parent, "LowFog_Center", new Vector3(0f, -1.8f, 1f), new Vector3(12f, 2.8f, 1f), new Color(0.66f, 0.73f, 0.73f, 0.14f), 68, 4.5f);
            CreateFogBand(parent, "LowFog_Back", new Vector3(0f, 2.7f, 1f), new Vector3(11f, 2.2f, 1f), new Color(0.55f, 0.64f, 0.68f, 0.1f), 16, 3.8f);
        }

        static void CreateFogBand(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int sortingOrder, float fps)
        {
            if (parent.Find(name) != null)
                return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/Overlays/zone1_overlay_humidity.png") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;

            var anim = go.AddComponent<SpriteSheetAnimator>();
            anim.Configure("Assets/02_Sprites/Lucas/Zone1/Spritesheets/zone1_lowfog_sheet.png", 96, 48, fps);
        }

        static void CreateTorch(Transform parent, string name, Vector3 pos, int baseSortingOrder)
        {
            if (parent.Find(name) != null)
                return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.4f, 1.2f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/Props/zone1_prop_column_damaged.png") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = new Color(0.5f, 0.35f, 0.15f, 1f);
            sr.sortingOrder = baseSortingOrder;

            var flame = new GameObject("Flame");
            flame.transform.SetParent(go.transform, false);
            flame.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            flame.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            var fsr = flame.AddComponent<SpriteRenderer>();
            fsr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/Spritesheets/zone1_torch_sheet.png") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            fsr.color = new Color(1f, 0.85f, 0.25f, 1f);
            fsr.sortingOrder = baseSortingOrder + 1;

            var anim = flame.AddComponent<SpriteSheetAnimator>();
            anim.Configure("Assets/02_Sprites/Lucas/Zone1/Spritesheets/zone1_torch_sheet.png", 32, 32, 10f);
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
            sr.sprite = Zone1ArtProvider.LoadSprite($"Assets/02_Sprites/Lucas/Zone1/Props/{fileName}") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = Color.white;
            sr.sortingOrder = sortingOrder + Mathf.RoundToInt(-pos.y * 3f);
        }

        static void CreateBrokenWalls(Transform wallsBackRoot, Transform wallsFrontRoot)
        {
            if (wallsBackRoot.Find("DungeonWallsBack") == null)
            {
                var back = new GameObject("DungeonWallsBack");
                back.transform.SetParent(wallsBackRoot, false);

                CreateWallSegment(back.transform, "TopWall_Left", new Vector3(-7.5f, 5.6f, 0f), new Vector3(10.5f, 2.4f, 1f), -10, new Color(0.19f, 0.18f, 0.22f, 1f));
                CreateWallSegment(back.transform, "TopWall_Right", new Vector3(7.5f, 5.6f, 0f), new Vector3(10.5f, 2.4f, 1f), -10, new Color(0.19f, 0.18f, 0.22f, 1f));
                CreateWallSegment(back.transform, "LeftWall", new Vector3(-12.2f, -0.2f, 0f), new Vector3(2.3f, 11.8f, 1f), -9, new Color(0.17f, 0.16f, 0.20f, 1f));
                CreateWallSegment(back.transform, "RightWall", new Vector3(12.2f, -0.2f, 0f), new Vector3(2.3f, 11.8f, 1f), -9, new Color(0.17f, 0.16f, 0.20f, 1f));
                CreateWallSegment(back.transform, "BrokenDoorLintel", new Vector3(0f, 5.2f, 0f), new Vector3(3.8f, 1.2f, 1f), -8, new Color(0.26f, 0.22f, 0.18f, 1f));

                CreateProp(back.transform, "zone1_prop_sealed_door.png", new Vector3(0f, 4.55f, 0f), new Vector3(1.35f, 1.15f, 1f), -3);
                CreateProp(back.transform, "zone1_prop_chain_hanging.png", new Vector3(-4.9f, 4.8f, 0f), new Vector3(0.8f, 0.8f, 1f), -1);
                CreateProp(back.transform, "zone1_prop_chain_hanging.png", new Vector3(4.7f, 4.8f, 0f), new Vector3(0.85f, 0.85f, 1f), -1);
            }

            if (wallsFrontRoot.Find("DungeonWallsFront") == null)
            {
                var front = new GameObject("DungeonWallsFront");
                front.transform.SetParent(wallsFrontRoot, false);

                CreateWallSegment(front.transform, "BottomWall_Left", new Vector3(-8.3f, -6.8f, 0f), new Vector3(8.6f, 1.7f, 1f), 78, new Color(0.16f, 0.15f, 0.18f, 1f));
                CreateWallSegment(front.transform, "BottomWall_Right", new Vector3(8.3f, -6.8f, 0f), new Vector3(8.6f, 1.7f, 1f), 78, new Color(0.16f, 0.15f, 0.18f, 1f));
                CreateWallSegment(front.transform, "BrokenGapShadow", new Vector3(0f, -6.6f, 0f), new Vector3(3.2f, 1.1f, 1f), 77, new Color(0.04f, 0.03f, 0.05f, 1f));

                CreateProp(front.transform, "zone1_prop_cage_broken.png", new Vector3(-9.1f, -5.9f, 0f), new Vector3(1f, 1f, 1f), 82);
                CreateProp(front.transform, "zone1_prop_cage_broken.png", new Vector3(9.25f, -5.75f, 0f), new Vector3(-1f, 1f, 1f), 82);
                CreateProp(front.transform, "zone1_prop_bones_small.png", new Vector3(-2.4f, -5.95f, 0f), new Vector3(0.95f, 0.95f, 1f), 79);
                CreateProp(front.transform, "zone1_prop_bones_small.png", new Vector3(3.2f, -5.8f, 0f), new Vector3(0.85f, 0.85f, 1f), 79);
            }
        }

        static void CreateCellFieldLayout(Transform parent)
        {
            if (parent.Find("CellFieldLayout") != null)
                return;

            var root = new GameObject("CellFieldLayout");
            root.transform.SetParent(parent, false);

            CreateWallSegment(root.transform, "FieldShadow", new Vector3(0f, -0.45f, 0f), new Vector3(16.4f, 10.8f, 1f), 8, new Color(0.02f, 0.02f, 0.04f, 0.35f));
            CreateWallSegment(root.transform, "FieldPlate", new Vector3(0f, -0.1f, 0f), new Vector3(15.2f, 9.5f, 1f), 9, new Color(0.12f, 0.10f, 0.12f, 0.58f));

            for (var row = 0; row < 5; row++)
            {
                for (var col = 0; col < 6; col++)
                {
                    var guide = new GameObject($"SlotGuide_{row}_{col}");
                    guide.transform.SetParent(root.transform, false);
                    guide.transform.localPosition = new Vector3(-5.35f + col * 2.14f, 3.65f - row * 1.92f, 0f);
                    guide.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

                    var sr = guide.AddComponent<SpriteRenderer>();
                    sr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/Tiles/zone1_floor_ritual_tile.png") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
                    sr.color = new Color(0.65f, 0.2f, 0.18f, 0.08f);
                    sr.sortingOrder = 10;
                }
            }
        }

        static void CreateDungeonDecor(Transform decorRoot)
        {
            var existing = FindChildByNameIncludingInactive(decorRoot, "DungeonDecor");
            if (existing != null)
            {
                EnsureFountains(existing);
                return;
            }

            var root = new GameObject("DungeonDecor");
            root.transform.SetParent(decorRoot, false);

            CreateTorch(root.transform, "Torch_NorthWest", new Vector3(-8.9f, 3.8f, 0f), 24);
            CreateTorch(root.transform, "Torch_NorthEast", new Vector3(8.9f, 3.8f, 0f), 24);
            CreateTorch(root.transform, "Torch_SouthWest", new Vector3(-8.4f, -4.8f, 0f), 76);
            CreateTorch(root.transform, "Torch_SouthEast", new Vector3(8.4f, -4.8f, 0f), 76);

            CreateProp(root.transform, "zone1_prop_cage_broken.png", new Vector3(-10.2f, 1.8f, 0f), new Vector3(0.9f, 0.9f, 1f), 18);
            CreateProp(root.transform, "zone1_prop_cage_broken.png", new Vector3(10.1f, 1.5f, 0f), new Vector3(-0.92f, 0.92f, 1f), 18);
            CreateProp(root.transform, "zone1_prop_dark_banner.png", new Vector3(-6.5f, 4.2f, 0f), new Vector3(0.95f, 0.95f, 1f), 19);
            CreateProp(root.transform, "zone1_prop_dark_banner.png", new Vector3(6.5f, 4.2f, 0f), new Vector3(-0.95f, 0.95f, 1f), 19);
            CreateProp(root.transform, "zone1_prop_dark_puddle.png", new Vector3(-6.3f, -3.7f, 0f), new Vector3(1f, 1f, 1f), 11);
            CreateProp(root.transform, "zone1_prop_dark_puddle.png", new Vector3(6.0f, -3.2f, 0f), new Vector3(0.9f, 0.9f, 1f), 11);
            CreateProp(root.transform, "zone1_prop_ritual_candles.png", new Vector3(0f, -5.2f, 0f), new Vector3(0.72f, 0.72f, 1f), 78);
            CreateRitualMark(root.transform, new Vector3(0f, -4.2f, 0f));

            // Ambient animated fountains (new spritesheet 4x1 @ 128px).
            EnsureFountains(root.transform);
        }

        static void EnsureFountains(Transform parent)
        {
            CreateFountain(parent, "Fountain_West", new Vector3(-10.6f, 0.15f, 0f), new Vector3(0.85f, 0.85f, 1f), 22);
            CreateFountain(parent, "Fountain_East", new Vector3(10.55f, -0.1f, 0f), new Vector3(-0.85f, 0.85f, 1f), 22);
            CreateFountain(parent, "Fountain_North", new Vector3(0.0f, 4.65f, 0f), new Vector3(0.78f, 0.78f, 1f), 16);
        }

        static void CreateFountain(Transform parent, string name, Vector3 pos, Vector3 scale, int sortingOrder)
        {
            if (FindChildByNameIncludingInactive(parent, name) != null)
                return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;

            var sh = new GameObject("Shadow");
            sh.transform.SetParent(go.transform, false);
            sh.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            sh.transform.localScale = new Vector3(1.25f, 0.42f, 1f);
            var shSr = sh.AddComponent<SpriteRenderer>();
            shSr.sprite = RuntimeSpriteFactory.OpaqueWhiteSprite;
            shSr.color = new Color(0f, 0f, 0f, 0.22f);
            shSr.sortingOrder = sortingOrder - 1;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/Spritesheets/zone1_fountain_spritesheet_4x1_128.png") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = Color.white;
            sr.sortingOrder = sortingOrder;

            var anim = go.AddComponent<SpriteSheetAnimator>();
            anim.Configure("Assets/02_Sprites/Lucas/Zone1/Spritesheets/zone1_fountain_spritesheet_4x1_128.png", 128, 128, 8f);
        }

        static void CreateRitualMark(Transform parent, Vector3 pos)
        {
            if (parent.Find("RitualMarkPlaceholder") != null)
                return;

            var go = new GameObject("RitualMarkPlaceholder");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(5f, 1.7f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/Tiles/zone1_floor_ritual_tile.png") ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = new Color(0.55f, 0.10f, 0.15f, 0.18f);
            sr.sortingOrder = 12;
        }

        static void CreateAmbientOverlays(Transform parent)
        {
            if (parent.Find("AmbientRunes") != null || parent.Find("AmbientVignette") != null)
                return;

            var runes = new GameObject("AmbientRunes");
            runes.transform.SetParent(parent, false);
            runes.transform.position = new Vector3(0f, 0f, 1.8f);
            runes.transform.localScale = new Vector3(5f, 4f, 1f);
            var runesSr = runes.AddComponent<SpriteRenderer>();
            runesSr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/Overlays/zone1_overlay_runes.png");
            runesSr.color = new Color(1f, 1f, 1f, 0.35f);
            runesSr.sortingOrder = 6;

            var vignette = new GameObject("AmbientVignette");
            vignette.transform.SetParent(parent, false);
            vignette.transform.position = new Vector3(0f, 0f, 6f);
            vignette.transform.localScale = new Vector3(5f, 5f, 1f);
            var vigSr = vignette.AddComponent<SpriteRenderer>();
            vigSr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/Overlays/zone1_overlay_vignette.png");
            vigSr.color = new Color(1f, 1f, 1f, 0.25f);
            vigSr.sortingOrder = 90;
        }

        static void CreateWallSegment(Transform parent, string name, Vector3 pos, Vector3 scale, int sortingOrder, Color color)
        {
            if (parent.Find(name) != null)
                return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
        }

        static GameObject GetOrCreateChild(Transform parent, string childName)
        {
            var child = FindChildByNameIncludingInactive(parent, childName);
            if (child != null)
                return child.gameObject;

            var go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            return go;
        }

        static void EnsureSystems()
        {
            var systems = FindRootInActiveScene("Systems");
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

            var go = FindChildByNameIncludingInactive(systemsRoot, objectName)?.gameObject;
            if (go == null)
            {
                go = new GameObject(objectName);
                go.transform.SetParent(systemsRoot, false);
            }

            if (go.GetComponent<T>() == null)
                go.AddComponent<T>();
        }
    }

    /// <summary>
    /// Un frame después de cargar Zone1 (tras game over / menú) vuelve a asegurar cultistas y fuentes.
    /// </summary>
    sealed class Zone1DecorDeferredEnsureHost : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return null;
            Zone1Bootstrap.EnsureZone1RuntimeDecor();
            Destroy(gameObject);
        }
    }
}

