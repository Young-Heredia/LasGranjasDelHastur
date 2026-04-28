using UnityEngine;

namespace LasGranjasDelHastur.Creatures
{
    public readonly struct TindalosHoundDef
    {
        public readonly string DisplayName;
        public readonly Color Body;
        public readonly Color Head;
        public readonly Vector3 BodyScale;
        public readonly Vector3 HeadOffset;
        public readonly Vector3 HeadScale;
        public readonly int SortingOrder;
        public readonly float BobHz;
        public readonly float BobAmp;
        public readonly bool ShadowPulse;
        public readonly bool ElderSlow;

        TindalosHoundDef(
            string name, Color body, Color head, Vector3 bodyScale, Vector3 headOff, Vector3 headScale,
            int sort, float bobHz, float bobAmp, bool shadowPulse, bool elderSlow)
        {
            DisplayName = name;
            Body = body;
            Head = head;
            BodyScale = bodyScale;
            HeadOffset = headOff;
            HeadScale = headScale;
            SortingOrder = sort;
            BobHz = bobHz;
            BobAmp = bobAmp;
            ShadowPulse = shadowPulse;
            ElderSlow = elderSlow;
        }

        public static TindalosHoundDef For(TindalosHoundKind k) =>
            k switch
            {
                TindalosHoundKind.Pup => new TindalosHoundDef(
                    "Cachorro de Tíndalos",
                    new Color(0.55f, 0.9f, 0.75f, 0.95f),
                    new Color(0.7f, 0.95f, 0.9f, 1f),
                    new Vector3(0.38f, 0.22f, 1f),
                    new Vector3(0.1f, 0.1f, 0f), new Vector3(0.18f, 0.16f, 1f),
                    12, 2.0f, 0.04f, false, false),
                TindalosHoundKind.Adolescent => new TindalosHoundDef(
                    "Adolescente de Tíndalos",
                    new Color(0.4f, 0.75f, 0.9f, 0.95f),
                    new Color(0.55f, 0.88f, 1f, 1f),
                    new Vector3(0.5f, 0.3f, 1f),
                    new Vector3(0.12f, 0.12f, 0f), new Vector3(0.2f, 0.18f, 1f),
                    13, 1.6f, 0.05f, false, false),
                TindalosHoundKind.Adult => new TindalosHoundDef(
                    "Adulto de Tíndalos",
                    new Color(0.35f, 0.45f, 0.9f, 0.95f),
                    new Color(0.5f, 0.55f, 0.85f, 1f),
                    new Vector3(0.7f, 0.4f, 1f),
                    new Vector3(0.16f, 0.14f, 0f), new Vector3(0.25f, 0.2f, 1f),
                    14, 1.2f, 0.05f, false, false),
                TindalosHoundKind.ElderDog => new TindalosHoundDef(
                    "Perro Anciano",
                    new Color(0.5f, 0.55f, 0.7f, 0.9f),
                    new Color(0.65f, 0.68f, 0.78f, 0.95f),
                    new Vector3(0.6f, 0.32f, 1f),
                    new Vector3(0.12f, 0.1f, 0f), new Vector3(0.2f, 0.16f, 1f),
                    11, 0.75f, 0.03f, false, true),
                _ => new TindalosHoundDef(
                    "Perro Sombrío",
                    new Color(0.2f, 0.1f, 0.4f, 0.75f),
                    new Color(0.4f, 0.2f, 0.5f, 0.55f),
                    new Vector3(0.64f, 0.36f, 1f),
                    new Vector3(0.14f, 0.12f, 0f), new Vector3(0.22f, 0.18f, 1f),
                    10, 0.9f, 0.04f, true, false),
            };
    }
}
