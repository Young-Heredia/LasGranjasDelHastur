using LasGranjasDelHastur;
using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone2.Jose;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LasGranjasDelHastur.Zone2.Lucas
{
    /// <summary>9 Pibles alrededor de la rejilla y lejos de los escudos gacha; runtime idempotente.</summary>
    public static class Zone2PibleEasterEggBootstrap
    {
        const string RootName = "Zone2PibleEasterRoot";

        /// <summary>
        /// Posiciones mundo (alejadas de <see cref="Zone2CellGridLayout"/> y de <see cref="Zone2GachaWorldBootstrap.ShieldWorldPositions"/>).
        /// </summary>
        static readonly (string name, Vector3 pos, Vector3 scale)[] Placements =
        {
            ("Pible_1", new Vector3(-10.15f, 4.85f, 0f), new Vector3(0.92f, 0.92f, 1f)),
            ("Pible_2", new Vector3(-10.05f, -1.05f, 0f), new Vector3(0.9f, 0.9f, 1f)),
            ("Pible_3", new Vector3(-9.85f, -4.95f, 0f), new Vector3(-0.9f, 0.9f, 1f)),
            ("Pible_4", new Vector3(-4.85f, 7.95f, 0f), new Vector3(0.88f, 0.88f, 1f)),
            ("Pible_5", new Vector3(5.95f, 5.65f, 0f), new Vector3(-0.88f, 0.88f, 1f)),
            ("Pible_6", new Vector3(6.85f, 1.35f, 0f), new Vector3(0.86f, 0.86f, 1f)),
            ("Pible_7", new Vector3(6.55f, -3.95f, 0f), new Vector3(-0.86f, 0.86f, 1f)),
            ("Pible_8", new Vector3(-1.05f, -5.85f, 0f), new Vector3(0.9f, 0.9f, 1f)),
            ("Pible_9", new Vector3(3.15f, -5.65f, 0f), new Vector3(-0.9f, 0.9f, 1f)),
        };

        public static void EnsurePibles(Transform worldRoot)
        {
            if (worldRoot == null || !worldRoot)
                return;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Zone2_Cities")
                return;

            if (worldRoot.Find(RootName) != null)
                return;

            var rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(worldRoot, false);
            var ctrl = rootGo.AddComponent<Zone2PibleEasterEggController>();
            ctrl.PruneDestroyedPibles();

            foreach (var p in Placements)
                EnsureOnePible(rootGo.transform, ctrl, p.name, p.pos, p.scale);

            ctrl.PruneDestroyedPibles();
        }

        static void EnsureOnePible(
            Transform parent,
            Zone2PibleEasterEggController ctrl,
            string objectName,
            Vector3 worldPos,
            Vector3 scale)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.localScale = scale;

            var sr = go.AddComponent<SpriteRenderer>();
            const string idle =
                "Assets/02_Sprites/Lucas/Zone2/EasterEgg/zone1_easteregg_tindalos_pible_idle_v3.png";
            sr.sprite = Zone1ArtProvider.LoadSprite(idle) ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = Color.white;
            sr.sortingOrder = 44 + Mathf.RoundToInt(-worldPos.y * 3f);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.72f, 0.88f);
            col.offset = new Vector2(0f, 0.08f);

            var clickable = go.AddComponent<Zone2PibleClickable>();
            clickable.controller = ctrl;

            var anim = go.AddComponent<SpriteSheetAnimator>();
            anim.enabled = false;

            ctrl.RegisterPible(go.transform);
        }
    }
}
