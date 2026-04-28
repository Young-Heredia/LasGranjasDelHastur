using LasGranjasDelHastur;
using UnityEngine;

namespace LasGranjasDelHastur.Zone2
{
    /// <summary>
    /// Capa de decoración: calles rituales, edificios dañados, torres corruptas, niebla densa (ciudad condensada).
    /// </summary>
    public static class Zone2CityDestroyedMap
    {
        public static void PopulateIfMissing(Transform worldRoot, string spriteFogPath)
        {
            if (worldRoot == null)
                return;
            if (worldRoot.Find("Layer_Floor/DestroyedCityRitualStreets") != null)
                return;

            var floor = worldRoot.Find("Layer_Floor");
            var back = worldRoot.Find("Layer_WallsBack");
            var decor = worldRoot.Find("Layer_Decor");
            var fog = worldRoot.Find("Layer_Fog");
            if (floor == null)
                return;

            var ritualRoot = new GameObject("DestroyedCityRitualStreets");
            ritualRoot.transform.SetParent(floor, false);
            ritualRoot.transform.SetAsLastSibling();
            // Cruces: “calles rituales” (tintes púrpura / carmín)
            AddLine(ritualRoot.transform, "RitualH_A", new Vector3(0f, 1.2f, 0f), new Vector3(20f, 0.2f, 1f), new Color(0.32f, 0.10f, 0.24f, 0.45f), 1);
            AddLine(ritualRoot.transform, "RitualV_B", new Vector3(-1.2f, -0.2f, 0f), new Vector3(0.2f, 9f, 1f), new Color(0.20f, 0.12f, 0.32f, 0.4f), 1);
            AddLine(ritualRoot.transform, "RitualH_C", new Vector3(2.4f, -1.4f, 0f), new Vector3(10f, 0.12f, 1f), new Color(0.38f, 0.12f, 0.20f, 0.32f), 1);

            if (decor != null)
            {
                var ruins = new GameObject("DamagedBuildings");
                ruins.transform.SetParent(decor, false);
                // Edificios dañados (bloques bajos, tonos ocre / gris)
                AddBlock(ruins.transform, "Damaged_A", new Vector3(-8.2f, 0.2f, 0f), new Vector3(3.0f, 1.6f, 1f), new Color(0.20f, 0.16f, 0.14f, 0.9f), 5);
                AddBlock(ruins.transform, "Damaged_B", new Vector3(7.0f, -0.4f, 0f), new Vector3(2.6f, 1.3f, 1f), new Color(0.16f, 0.15f, 0.16f, 0.88f), 5);
                AddBlock(ruins.transform, "Damaged_C", new Vector3(-2.0f, 3.2f, 0f), new Vector3(1.4f, 0.8f, 1f), new Color(0.24f, 0.18f, 0.12f, 0.55f), 4);
                AddBlock(ruins.transform, "Damaged_D", new Vector3(4.2f, 2.6f, 0f), new Vector3(1.2f, 0.6f, 1f), new Color(0.18f, 0.12f, 0.10f, 0.5f), 4);
                AddBlock(ruins.transform, "RubbleE", new Vector3(-4.0f, -2.0f, 0f), new Vector3(2.0f, 0.4f, 1f), new Color(0.14f, 0.10f, 0.10f, 0.4f), 3);
            }

            if (back != null && back.Find("CorruptTowersZ2") == null)
            {
                var tr = new GameObject("CorruptTowersZ2");
                tr.transform.SetParent(back, false);
                AddBlock(tr.transform, "CorruptTower_L", new Vector3(-10.2f, 0.1f, 0f), new Vector3(1.0f, 5.0f, 1f), new Color(0.26f, 0.05f, 0.20f, 0.95f), -5);
                AddBlock(tr.transform, "CorruptTower_R", new Vector3(10.0f, 0.0f, 0f), new Vector3(1.1f, 5.2f, 1f), new Color(0.24f, 0.06f, 0.22f, 0.95f), -5);
                AddBlock(tr.transform, "CorruptTower_C", new Vector3(0.0f, 3.0f, 0f), new Vector3(0.6f, 3.2f, 1f), new Color(0.20f, 0.04f, 0.16f, 0.5f), -4);
            }

            if (fog != null && fog.Find("CondensedCitySmog") == null)
            {
                var smog = new GameObject("CondensedCitySmog");
                smog.transform.SetParent(fog, false);
                var sr = smog.AddComponent<SpriteRenderer>();
                sr.sprite = TryFog(spriteFogPath) ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
                sr.color = new Color(0.58f, 0.5f, 0.42f, 0.18f);
                sr.sortingOrder = 25;
                smog.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                smog.transform.localScale = new Vector3(30f, 6.5f, 1f);
            }
        }

        static void AddLine(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color, int order)
        {
            if (parent.Find(name) != null)
                return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = color;
            sr.sortingOrder = order;
        }

        static void AddBlock(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color, int order)
        {
            if (parent == null || parent.Find(name) != null)
                return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = color;
            sr.sortingOrder = order;
        }

        static Sprite TryFog(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }
    }
}
