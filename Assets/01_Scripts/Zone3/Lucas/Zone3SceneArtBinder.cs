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
        const string Path_Backdrop = "Assets/02_Sprites/Lucas/Zone3/NewBackGround/z3_background_black_space_stars_3840x2160.png";

        [Header("Scene Targets (auto-detect by name)")]
        [SerializeField] SpriteRenderer backdrop;

        [Header("Sprites (auto-filled in Editor)")]
        [SerializeField] Sprite backdropSprite;

        void Reset() => RebindAndApply();
        void OnEnable() => RebindAndApply();
        void OnValidate() => RebindAndApply();

        void RebindAndApply()
        {
            var world = GameObject.Find("WorldRoot")?.transform;
            if (world != null)
            {
                backdrop = FindSpriteRenderer(world, "Layer_WallsBack/CelestialBackdrop");
            }

#if UNITY_EDITOR
            if (backdropSprite == null) backdropSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(Path_Backdrop);
#endif

            ApplyIfPossible(backdrop, backdropSprite, fallbackColor: Color.white);
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

