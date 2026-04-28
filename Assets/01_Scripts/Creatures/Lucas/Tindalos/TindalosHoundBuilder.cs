using LasGranjasDelHastur;
using UnityEngine;

namespace LasGranjasDelHastur.Creatures
{
    public static class TindalosHoundBuilder
    {
        public static GameObject Build(Transform parent, TindalosHoundKind kind, Vector3 localPos, int sortingOrderOffset = 0)
        {
            var def = TindalosHoundDef.For(kind);
            var go = new GameObject(SafeObjectName(def.DisplayName));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPos;
            }
            else
            {
                go.transform.position = localPos;
            }

            go.transform.localScale = Vector3.one;
            go.layer = 0;

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = def.BodyScale;
            var bodySr = body.AddComponent<SpriteRenderer>();
            bodySr.sprite = RuntimeSpriteFactory.OpaqueWhiteSprite;
            bodySr.color = def.Body;
            bodySr.sortingOrder = def.SortingOrder + sortingOrderOffset;

            var head = new GameObject("Head");
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = def.HeadOffset;
            head.transform.localScale = def.HeadScale;
            var headSr = head.AddComponent<SpriteRenderer>();
            headSr.sprite = RuntimeSpriteFactory.OpaqueWhiteSprite;
            headSr.color = def.Head;
            headSr.sortingOrder = def.SortingOrder + 1 + sortingOrderOffset;

            var anim = go.AddComponent<TindalosSimpleAnim>();
            anim.Setup(def);

            return go;
        }

        public static string SafeObjectName(string display) =>
            display
                .Replace(" ", "_")
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u")
                .Replace("ñ", "n")
                + "_Tindalos";

        public static TindalosHoundKind[] AllKinds { get; } =
        {
            TindalosHoundKind.Pup,
            TindalosHoundKind.Adolescent,
            TindalosHoundKind.Adult,
            TindalosHoundKind.ElderDog,
            TindalosHoundKind.ShadowDog,
        };
    }
}
