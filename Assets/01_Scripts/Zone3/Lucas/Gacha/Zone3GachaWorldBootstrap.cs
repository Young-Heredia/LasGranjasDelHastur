using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone1.Gacha;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LasGranjasDelHastur.Zone3.Lucas
{
    public static class Zone3GachaWorldBootstrap
    {
        public const string CelestialGachaSheetPath = "Assets/02_Sprites/Lucas/Zone3/Gacha/GACHAFORZONE3.png";
        const string RootName = "Zone3GachaCelestialWheels";
        const float WheelPixelsPerUnit = 240f;
        const float WheelInteractRadius = 0.52f;

        public static void EnsureCelestialGacha(Transform worldRoot)
        {
            if (worldRoot == null || !worldRoot)
                return;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Zone3_Celestial")
                return;

            if (worldRoot.Find(RootName) != null)
                return;

            // Este sheet trae 9 frames útiles (3x3) centrados.
            var frames = Zone1ArtProvider.LoadSheetUniformGrid(CelestialGachaSheetPath, 3, 3, WheelPixelsPerUnit);
            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning("[Zone3 Gacha] No se pudieron cargar frames desde '" + CelestialGachaSheetPath + "'.");
                return;
            }

            var root = new GameObject(RootName);
            root.transform.SetParent(worldRoot, false);

            foreach (var p in WheelWorldPositions())
            {
                var go = new GameObject("GachaCelestialWheel");
                go.transform.SetParent(root.transform, false);
                go.transform.position = p;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = frames[0];
                sr.sortingOrder = 42;

                var col = go.AddComponent<CircleCollider2D>();
                col.radius = WheelInteractRadius;

                go.AddComponent<GachaWorldInteract>();
                var anim = go.AddComponent<GachaCelestialWheelAnimator>();
                anim.SetFrames(frames);
            }
        }

        public static Vector3[] WheelWorldPositions()
        {
            const float originX = -3.3f;
            const float originY = 1.8f;
            const float spacingX = 2.2f;
            const float spacingY = 2.2f;
            const int columns = 4;
            const int rows = 3;

            var minX = originX - 2.35f;
            var maxX = originX + (columns - 1) * spacingX + 2.35f;
            var maxY = originY + 2.4f;
            var minY = originY - (rows - 1) * spacingY - 2.4f;
            var midX = (minX + maxX) * 0.5f;
            var midY = (minY + maxY) * 0.5f;

            return new[]
            {
                new Vector3(minX - 0.95f, midY, 0f),
                new Vector3(maxX + 0.95f, midY, 0f),
                new Vector3(midX, maxY + 1.05f, 0f),
                new Vector3(midX, minY - 1.05f, 0f)
            };
        }
    }
}
