using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone1.Gacha;
using LasGranjasDelHastur.Zone2.Jose;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LasGranjasDelHastur.Zone2.Lucas
{
    /// <summary>
    /// Coloca 4 escudos estelares (spritesheet) fuera de la rejilla de farmeo; clic abre el mismo <see cref="Zone1GachaController"/> que Zona 1.
    /// </summary>
    public static class Zone2GachaWorldBootstrap
    {
        public const string StellarShieldSheetPath = "Assets/02_Sprites/Lucas/Zone2/Gacha/GachaForZone2.png";
        const string RootName = "Zone2GachaStellarShields";

        /// <summary>PPU alto = sprite más pequeño en mundo; collider compacto para no pisar celdas.</summary>
        const float ShieldPixelsPerUnit = 240f;

        /// <summary>Radio del clic en unidades mundo (~mitad del ancho visible del escudo).</summary>
        const float ShieldInteractRadius = 0.52f;

        public static void EnsureStellarShields(Transform worldRoot)
        {
            if (worldRoot == null || !worldRoot)
                return;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Zone2_Cities")
                return;

            if (worldRoot.Find(RootName) != null)
                return;

            var frames = Zone1ArtProvider.LoadSheetUniformGrid(StellarShieldSheetPath, 3, 3, ShieldPixelsPerUnit);
            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning("[Zone2 Gacha] No se pudieron cargar frames desde '" + StellarShieldSheetPath + "'. ¿Existe el PNG en Assets?");
                return;
            }

            var root = new GameObject(RootName);
            root.transform.SetParent(worldRoot, false);

            foreach (var p in ShieldWorldPositions())
            {
                var go = new GameObject("GachaStellarShield");
                go.transform.SetParent(root.transform, false);
                go.transform.position = p;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = frames[0];
                sr.sortingOrder = 42;

                var col = go.AddComponent<CircleCollider2D>();
                col.radius = ShieldInteractRadius;

                go.AddComponent<GachaWorldInteract>();
                var anim = go.AddComponent<GachaStellarShieldAnimator>();
                anim.SetFrames(frames);
            }
        }

        /// <summary>Cuatro puntos alrededor de la rejilla 4×3 (<see cref="Zone2CellGridLayout"/>), alejados del área de celdas.</summary>
        public static Vector3[] ShieldWorldPositions()
        {
            var minX = Zone2CellGridLayout.Origin.x - 2.35f;
            var maxX = Zone2CellGridLayout.Origin.x + (Zone2CellGridLayout.Columns - 1) * Zone2CellGridLayout.Spacing.x + 2.35f;
            var maxY = Zone2CellGridLayout.Origin.y + 2.4f;
            var minY = Zone2CellGridLayout.Origin.y - (Zone2CellGridLayout.Rows - 1) * Zone2CellGridLayout.Spacing.y - 2.4f;
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
