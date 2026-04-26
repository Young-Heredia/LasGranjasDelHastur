using LasGranjasDelHastur.Core;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    /// <summary>
    /// Jerarquía visual de un slot: sombra, anillo de selección, pulso "listo", marcador de asistente. Usado por <see cref="CellManager"/> y el baker de prefab.
    /// </summary>
    public static class FarmCellSlotHierarchy
    {
        public struct FxRefs
        {
            public GameObject SelectionRing;
            public GameObject ReadyPulse;
            public GameObject AssistantMarker;
        }

        public static FxRefs Ensure(Transform parent, int baseSortingOrder)
        {
            var refs = new FxRefs();
            if (parent == null)
                return refs;

            if (parent.GetComponent<SpriteRenderer>() is { } mainSr)
                mainSr.sortingOrder = baseSortingOrder;

            EnsureGroundShadow(parent, baseSortingOrder);
            refs.SelectionRing = EnsureSelectionRing(parent, baseSortingOrder);
            refs.ReadyPulse = EnsureReadyPulse(parent, baseSortingOrder);
            refs.AssistantMarker = EnsureAssistantMarker(parent, baseSortingOrder);
            return refs;
        }

        static void EnsureGroundShadow(Transform parent, int baseSortingOrder)
        {
            if (parent.Find("GroundShadow") is { } gsh)
            {
                if (gsh.GetComponent<SpriteRenderer>() is { } ssr)
                    ssr.sortingOrder = baseSortingOrder - 1;
                return;
            }

            var shadow = new GameObject("GroundShadow");
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = new Vector3(0f, -0.58f, 0f);
            shadow.transform.localScale = new Vector3(1.1f, 0.55f, 1f);
            var shadowSr = shadow.AddComponent<SpriteRenderer>();
            shadowSr.sprite = RuntimeSpriteFactory.OpaqueWhiteSprite;
            shadowSr.color = new Color(0f, 0f, 0f, 0.28f);
            shadowSr.sortingOrder = baseSortingOrder - 1;
        }

        static GameObject EnsureSelectionRing(Transform parent, int baseSortingOrder)
        {
            var t = parent.Find("SelectionRing");
            if (t == null)
            {
                var ring = new GameObject("SelectionRing");
                ring.transform.SetParent(parent, false);
                ring.transform.localPosition = new Vector3(0f, -0.65f, 0f);
                ring.transform.localScale = new Vector3(1.1f, 0.45f, 1f);
                var ringSr = ring.AddComponent<SpriteRenderer>();
                ringSr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/Spritesheets/zone1_select_ring_sheet.png");
                ringSr.color = new Color(1f, 1f, 1f, 0.9f);
                ringSr.sortingOrder = baseSortingOrder + 3;
                var ringAnim = ring.AddComponent<SpriteSheetAnimator>();
                ringAnim.Configure("Assets/02_Sprites/Lucas/Zone1/Spritesheets/zone1_select_ring_sheet.png", 32, 32, 8f);
                ring.SetActive(false);
                return ring;
            }

            if (t.GetComponent<SpriteRenderer>() is { } rsr)
                rsr.sortingOrder = baseSortingOrder + 3;
            if (t.GetComponent<SpriteSheetAnimator>() == null)
            {
                var a = t.gameObject.AddComponent<SpriteSheetAnimator>();
                a.Configure("Assets/02_Sprites/Lucas/Zone1/Spritesheets/zone1_select_ring_sheet.png", 32, 32, 8f);
            }

            t.gameObject.SetActive(false);
            return t.gameObject;
        }

        static GameObject EnsureReadyPulse(Transform parent, int baseSortingOrder)
        {
            var t = parent.Find("ReadyPulse");
            if (t == null)
            {
                var ready = new GameObject("ReadyPulse");
                ready.transform.SetParent(parent, false);
                ready.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                ready.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
                var readySr = ready.AddComponent<SpriteRenderer>();
                readySr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/Spritesheets/zone1_ready_collect_sheet.png");
                readySr.color = Color.white;
                readySr.sortingOrder = baseSortingOrder + 4;
                var readyAnim = ready.AddComponent<SpriteSheetAnimator>();
                readyAnim.Configure("Assets/02_Sprites/Lucas/Zone1/Spritesheets/zone1_ready_collect_sheet.png", 32, 32, 10f);
                ready.SetActive(false);
                return ready;
            }

            if (t.GetComponent<SpriteRenderer>() is { } rsr)
                rsr.sortingOrder = baseSortingOrder + 4;
            if (t.GetComponent<SpriteSheetAnimator>() == null)
            {
                var a = t.gameObject.AddComponent<SpriteSheetAnimator>();
                a.Configure("Assets/02_Sprites/Lucas/Zone1/Spritesheets/zone1_ready_collect_sheet.png", 32, 32, 10f);
            }

            t.gameObject.SetActive(false);
            return t.gameObject;
        }

        static GameObject EnsureAssistantMarker(Transform parent, int baseSortingOrder)
        {
            var t = parent.Find("AssistantMarker");
            if (t == null)
            {
                var assistant = new GameObject("AssistantMarker");
                assistant.transform.SetParent(parent, false);
                assistant.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                assistant.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
                var assistantSr = assistant.AddComponent<SpriteRenderer>();
                assistantSr.sprite = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/Icons/zone1_icon_level.png")
                    ?? RuntimeSpriteFactory.OpaqueWhiteSprite;
                assistantSr.color = new Color(0.80f, 0.95f, 1f, 0.95f);
                assistantSr.sortingOrder = baseSortingOrder + 5;
                assistant.SetActive(false);
                return assistant;
            }

            if (t.GetComponent<SpriteRenderer>() is { } asr)
                asr.sortingOrder = baseSortingOrder + 5;
            t.gameObject.SetActive(false);
            return t.gameObject;
        }
    }
}
