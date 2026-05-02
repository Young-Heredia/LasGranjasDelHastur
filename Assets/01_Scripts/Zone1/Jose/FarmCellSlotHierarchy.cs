using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1.UI;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    /// <summary>
    /// Jerarquía visual de un slot: sombra, anillo de selección, pulso "listo", marcador de asistente. Usado por <see cref="CellManager"/> y el baker de prefab.
    /// </summary>
    public static class FarmCellSlotHierarchy
    {
        static Material _cachedParticleMaterial;
        public struct FxRefs
        {
            public GameObject SelectionRing;
            public GameObject ReadyPulse;
            public GameObject AssistantMarker;
            public GameObject ProducingFx;
            public GameObject ReadyFx;
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
            var prodA = new Color(1f, 1f, 1f, 0.78f);
            var prodB = new Color(1f, 1f, 1f, 0.42f);
            refs.ProducingFx = EnsureStateParticles(parent, "ProducingFx", baseSortingOrder + 6, 0.08f,
                prodA, prodB, 13f, 0.4f);
            refs.ReadyFx = EnsureStateParticles(parent, "ReadyFx", baseSortingOrder + 7, 0.38f,
                prodA, prodB, 22f, 0.3f);
            return refs;
        }

        static GameObject EnsureStateParticles(Transform parent, string childName, int sortingOrder, float localY,
            Color colorA, Color colorB, float rate, float radius)
        {
            var t = parent.Find(childName);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(childName);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(0f, localY, 0f);
                var ps = go.AddComponent<ParticleSystem>();
                StopAndClearParticles(ps);
                ConfigureLoopParticles(ps, sortingOrder, colorA, colorB, rate, radius);
                go.SetActive(false);
                return go;
            }

            go = t.gameObject;
            var existing = go.GetComponent<ParticleSystem>();
            if (existing == null)
            {
                existing = go.AddComponent<ParticleSystem>();
                StopAndClearParticles(existing);
                ConfigureLoopParticles(existing, sortingOrder, colorA, colorB, rate, radius);
            }
            else
            {
                StopAndClearParticles(existing);
                ConfigureLoopParticles(existing, sortingOrder, colorA, colorB, rate, radius);
            }

            go.SetActive(false);
            return go;
        }

        static void StopAndClearParticles(ParticleSystem ps)
        {
            if (ps == null)
                return;
            var mainStop = ps.main;
            mainStop.playOnAwake = false;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        static void ConfigureLoopParticles(ParticleSystem ps, int sortingOrder, Color colorA, Color colorB,
            float rate, float radius)
        {
            StopAndClearParticles(ps);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.72f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.32f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            main.maxParticles = 72;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = 0f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);

            var em = ps.emission;
            em.rateOverTime = rate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            ApplyParticleRendererSorting(ps, sortingOrder);
        }

        /// <summary>
        /// Sin material, Unity pinta las partículas en magenta.
        /// No usamos <c>Resources.GetBuiltinResource("Default-Particle.mat")</c>: en Unity 6 / algunos RP falla y escribe error en consola.
        /// </summary>
        static Material ResolveStateParticleMaterial()
        {
            if (_cachedParticleMaterial != null)
                return _cachedParticleMaterial;

            string[] shaderNames =
            {
                "Universal Render Pipeline/Particles/Unlit",
                "Universal Render Pipeline/Particles/Simple Lit",
                "Particles/Standard Unlit",
                "Legacy Shaders/Particles/Alpha Blended",
                "Mobile/Particles/Alpha Blended",
                "Sprites/Default",
            };

            foreach (var name in shaderNames)
            {
                var sh = Shader.Find(name);
                if (sh == null)
                    continue;
                var m = new Material(sh) { name = "FarmCellStateParticles_Runtime" };
                if (m.HasProperty("_BaseColor"))
                    m.SetColor("_BaseColor", Color.white);
                if (m.HasProperty("_TintColor"))
                    m.SetColor("_TintColor", Color.white);
                if (m.HasProperty("_Color"))
                    m.SetColor("_Color", Color.white);
                _cachedParticleMaterial = m;
                return _cachedParticleMaterial;
            }

            return null;
        }

        static void ApplyParticleRendererSorting(ParticleSystem ps, int sortingOrder)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sortingOrder = sortingOrder;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            var mat = ResolveStateParticleMaterial();
            if (mat != null)
                r.sharedMaterial = mat;
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
                assistant.transform.localScale = new Vector3(0.78f, 0.78f, 1f);
                var assistantSr = assistant.AddComponent<SpriteRenderer>();
                assistantSr.sprite = Zone1ArtProvider.LoadSprite(Zone1UiSpritePaths.AssistantHoundTindalosPortrait)
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
