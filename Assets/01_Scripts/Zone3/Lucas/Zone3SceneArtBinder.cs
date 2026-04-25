using UnityEngine;

namespace LasGranjasDelHastur.Zone3
{
    /// <summary>
    /// En Editor, deja serializados los sprites del pack en la escena (Hierarchy/Inspector),
    /// similar al enfoque de Zona 1. En build, usa esas referencias ya serializadas.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class Zone3SceneArtBinder : MonoBehaviour
    {
        const string PackRoot = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/";
        const string Path_Backplate = PackRoot + "Zones/Zone3_Celestial/Zone3_BackgroundPlate_CelestialFarm.png";
        const string Path_Backdrop = PackRoot + "Zones/Zone3_Celestial/Zone3_Backdrop_DeepSpace.png";
        const string Path_Front = PackRoot + "Zones/Zone3_Celestial/Zone3_Decor_ForegroundPlatforms.png";
        const string Path_MistSheet = PackRoot + "Zones/Zone3_Celestial/Zone3_AstralMistOverlay_Sheet.png";

        [Header("Scene Targets (auto-detect by name)")]
        [SerializeField] SpriteRenderer floor;
        [SerializeField] SpriteRenderer backdrop;
        [SerializeField] SpriteRenderer front;
        [SerializeField] SpriteRenderer mist;

        [Header("Sprites (auto-filled in Editor)")]
        [SerializeField] Sprite floorSprite;
        [SerializeField] Sprite backdropSprite;
        [SerializeField] Sprite frontSprite;
        [SerializeField] Sprite mistSprite;

        void Reset() => RebindAndApply();
        void OnEnable() => RebindAndApply();
        void OnValidate() => RebindAndApply();

        void RebindAndApply()
        {
            var world = GameObject.Find("WorldRoot")?.transform;
            if (world != null)
            {
                floor = FindSpriteRenderer(world, "Layer_Floor/AstralPlane");
                backdrop = FindSpriteRenderer(world, "Layer_WallsBack/DeepSpaceBackdrop");
                mist = FindSpriteRenderer(world, "Layer_Fog/AstralMist");
                front = FindSpriteRenderer(world, "Layer_WallsFront/ForegroundPlatforms");
            }

#if UNITY_EDITOR
            if (floorSprite == null) floorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(Path_Backplate);
            if (backdropSprite == null) backdropSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(Path_Backdrop);
            if (frontSprite == null) frontSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(Path_Front);
            if (mistSprite == null) mistSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(Path_MistSheet);
#endif

            ApplyIfPossible(floor, floorSprite, fallbackColor: new Color(0.03f, 0.03f, 0.09f, 1f));
            ApplyIfPossible(backdrop, backdropSprite, fallbackColor: new Color(0.05f, 0.04f, 0.11f, 1f));
            ApplyIfPossible(mist, mistSprite, fallbackColor: new Color(0.44f, 0.38f, 0.62f, 0.10f));
            ApplyIfPossible(front, frontSprite, fallbackColor: new Color(0.08f, 0.07f, 0.15f, 0.98f));
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

