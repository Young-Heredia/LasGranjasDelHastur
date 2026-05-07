using LasGranjasDelHastur;
using LasGranjasDelHastur.Zone1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LasGranjasDelHastur.Zone3.Lucas
{
    public static class Zone3FlautistaEasterEggBootstrap
    {
        const string RootName = "Zone3FlautistaEasterRoot";
        static readonly (string name, Vector3 pos, Vector3 scale)[] Placements =
        {
            ("Flautista_1", new Vector3(-10.1f, 4.7f, 0f), new Vector3(0.92f, 0.92f, 1f)),
            ("Flautista_2", new Vector3(-10.1f, -1.1f, 0f), new Vector3(0.90f, 0.90f, 1f)),
            ("Flautista_3", new Vector3(-9.8f, -5.0f, 0f), new Vector3(-0.90f, 0.90f, 1f)),
            ("Flautista_4", new Vector3(-4.9f, 7.7f, 0f), new Vector3(0.88f, 0.88f, 1f)),
            ("Flautista_5", new Vector3(6.0f, 5.6f, 0f), new Vector3(-0.88f, 0.88f, 1f)),
            ("Flautista_6", new Vector3(6.8f, 1.3f, 0f), new Vector3(0.86f, 0.86f, 1f)),
            ("Flautista_7", new Vector3(6.6f, -3.9f, 0f), new Vector3(-0.86f, 0.86f, 1f)),
            ("Flautista_8", new Vector3(-1.0f, -5.9f, 0f), new Vector3(0.90f, 0.90f, 1f)),
            ("Flautista_9", new Vector3(3.2f, -5.7f, 0f), new Vector3(-0.90f, 0.90f, 1f)),
        };

        public static void EnsureFlautistas(Transform worldRoot)
        {
            if (worldRoot == null || !worldRoot)
                return;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Zone3_Celestial")
                return;

            if (worldRoot.Find(RootName) != null)
                return;

            var rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(worldRoot, false);
            var ctrl = rootGo.AddComponent<Zone3FlautistaEasterEggController>();
            ctrl.PruneDestroyedFlautistas();

            foreach (var p in Placements)
                EnsureOne(rootGo.transform, ctrl, p.name, p.pos, p.scale);

            ctrl.PruneDestroyedFlautistas();
        }

        static void EnsureOne(
            Transform parent,
            Zone3FlautistaEasterEggController ctrl,
            string objectName,
            Vector3 worldPos,
            Vector3 scale)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.localScale = scale;

            var sr = go.AddComponent<SpriteRenderer>();
            const string idle = "Assets/02_Sprites/Lucas/Zone3/EasterEgg/flautista_amorfo_pixel_art_64x64.png";
            sr.sprite = Zone1ArtProvider.LoadSprite(idle) ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = Color.white;
            sr.sortingOrder = 44 + Mathf.RoundToInt(-worldPos.y * 3f);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.72f, 0.88f);
            col.offset = new Vector2(0f, 0.08f);

            var clickable = go.AddComponent<Zone3FlautistaClickable>();
            clickable.controller = ctrl;

            var anim = go.AddComponent<SpriteSheetAnimator>();
            anim.enabled = false;

            ctrl.RegisterFlautista(go.transform);
        }
    }
}
