using UnityEngine;

namespace LasGranjasDelHastur.Zone2.Jose
{
    /// <summary>
    /// En Editor, deja serializados los sprites del pack en la escena (Hierarchy/Inspector),
    /// similar al enfoque de Zona 1. En build, usa esas referencias ya serializadas.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class Zone2SceneArtBinder : MonoBehaviour
    {
        const string PackRoot = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/";
        const string Path_Backplate = PackRoot + "Zones/Zone2_Cities/Zone2_BackgroundPlate_CityFarm.png";
        const string Path_Backdrop = PackRoot + "Zones/Zone2_Cities/Zone2_Backdrop_CollapsedSkyline.png";
        const string Path_Front = PackRoot + "Zones/Zone2_Cities/Zone2_Decor_RubbleFront.png";
        const string Path_FogSheet = PackRoot + "Zones/Zone2_Cities/Zone2_FogOverlay_City_Sheet.png";

        [Header("Scene Targets (auto-detect by name)")]
        [SerializeField] SpriteRenderer floor;
        [SerializeField] SpriteRenderer backdrop;
        [SerializeField] SpriteRenderer front;
        [SerializeField] SpriteRenderer fog;

        [Header("Sprites (auto-filled in Editor)")]
        [SerializeField] Sprite floorSprite;
        [SerializeField] Sprite backdropSprite;
        [SerializeField] Sprite frontSprite;
        [SerializeField] Sprite fogSprite;

        void Reset() => RebindAndApply();
        void OnEnable() => RebindAndApply();
        void OnValidate() => RebindAndApply();

        void RebindAndApply()
        {
            // Scene lookup is cheap here and makes it resilient to scene edits.
            var world = GameObject.Find("WorldRoot")?.transform;
            if (world != null)
            {
                floor = FindSpriteRenderer(world, "Layer_Floor/CityFloorPlate");
                backdrop = FindSpriteRenderer(world, "Layer_WallsBack/CondensedSkyline_Back");
                fog = FindSpriteRenderer(world, "Layer_Fog/UrbanFog");
                front = FindSpriteRenderer(world, "Layer_WallsFront/FrontRubble");
            }

#if UNITY_EDITOR
            if (floorSprite == null) floorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(Path_Backplate);
            if (backdropSprite == null) backdropSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(Path_Backdrop);
            if (frontSprite == null) frontSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(Path_Front);
            if (fogSprite == null) fogSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(Path_FogSheet);
#endif

            ApplyIfPossible(floor, floorSprite, fallbackColor: new Color(0.08f, 0.12f, 0.14f, 1f));
            ApplyIfPossible(backdrop, backdropSprite, fallbackColor: new Color(0.14f, 0.14f, 0.16f, 1f));
            ApplyIfPossible(fog, fogSprite, fallbackColor: new Color(0.48f, 0.50f, 0.45f, 0.12f));
            ApplyIfPossible(front, frontSprite, fallbackColor: new Color(0.12f, 0.10f, 0.10f, 0.96f));
        }

        static SpriteRenderer FindSpriteRenderer(Transform root, string relative)
        {
            if (root == null)
                return null;
            var t = root.Find(relative);
            if (t == null)
                return null;
            return t.GetComponent<SpriteRenderer>();
        }

        static void ApplyIfPossible(SpriteRenderer sr, Sprite sprite, Color fallbackColor)
        {
            if (sr == null)
                return;
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(fallbackColor.a));
            }
            else
            {
                sr.color = fallbackColor;
            }
        }
    }
}

