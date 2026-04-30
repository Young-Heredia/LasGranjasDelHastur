using LasGranjasDelHastur;
using UnityEngine;

namespace LasGranjasDelHastur.Zone2.Jose
{
    /// <summary>
    /// Fondo 2D espacial: estrellas, nebulosa, lunas, plataformas flotantes y "máquinas cósmicas";
    /// enmarca el área de juego para las 30 celdas (6×5, alineada a Systems.Zone2CellManager).
    /// Código bajo <c>Assets/01_Scripts/Zone2/Jose/</c> (paralelo a Zone1/Jose).
    /// </summary>
    public static class Zone2SpaceScenery
    {
        const int Seed = 2042;
        public static readonly Vector2 CellGridCenter = new(0f, -0.1f);
        public static readonly Vector2 CellGridSize = new(17.4f, 10.2f);

        public static void PopulateIfMissing(Transform worldRoot)
        {
            if (worldRoot == null)
                return;
            if (worldRoot.Find("Layer_SpaceFar/SpaceStarfieldZ2") != null)
                return;

            var rng = new System.Random(Seed);
            var spaceFar = EnsureChildLayer(worldRoot, "Layer_SpaceFar");
            spaceFar.SetAsFirstSibling();

            var starfield = new GameObject("SpaceStarfieldZ2");
            starfield.transform.SetParent(spaceFar, false);
            starfield.AddComponent<Zone2SpaceStarfieldDrift>();
            for (var i = 0; i < 180; i++)
            {
                var a = 0.18f + (float)rng.NextDouble() * 0.82f;
                var s = 0.018f + (float)rng.NextDouble() * 0.1f;
                var x = (float)rng.NextDouble() * 44f - 22f;
                var y = (float)rng.NextDouble() * 22f - 9f;
                AddDot(starfield.transform, "Star" + i, new Vector3(x, y, 0f), new Vector3(s, s, 1f),
                    new Color(0.75f, 0.9f, 1f, a * 0.75f + 0.12f * (i % 3)), -52);
            }

            for (var n = 0; n < 2; n++)
            {
                var w = 18f + (float)rng.NextDouble() * 16f;
                var h = 8f + (float)rng.NextDouble() * 10f;
                var x = (float)rng.NextDouble() * 20f - 10f;
                var y = (float)rng.NextDouble() * 6f;
                var hue = n == 0 ? new Color(0.15f, 0.1f, 0.4f, 0.1f) : new Color(0.2f, 0.05f, 0.3f, 0.08f);
                AddDot(spaceFar, "Nebula_" + n, new Vector3(x, y, 0f), new Vector3(w, h, 1f), hue, -48);
            }

            var moons = new GameObject("SpaceMoonsZ2");
            moons.transform.SetParent(spaceFar, false);
            var moonData = new[]
            {
                (new Vector3(-11.2f, 5.0f, 0f), 0.9f, new Color(0.55f, 0.6f, 0.75f, 0.95f)),
                (new Vector3(10.4f, 3.2f, 0f), 0.6f, new Color(0.5f, 0.55f, 0.6f, 0.5f)),
                (new Vector3(4.0f, 5.5f, 0f), 0.45f, new Color(0.65f, 0.58f, 0.45f, 0.35f)),
            };
            for (var m = 0; m < moonData.Length; m++)
            {
                var p = moonData[m].Item1;
                var sc = moonData[m].Item2;
                var col = moonData[m].Item3;
                AddDot(moons.transform, "Moon_" + m, p, new Vector3(sc, sc, 1f), col, -46);
            }

            var floor = worldRoot.Find("Layer_Floor");
            if (floor != null)
            {
                if (floor.Find("SpaceFloatingPlatforms") == null)
                {
                    var fp = new GameObject("SpaceFloatingPlatforms");
                    fp.transform.SetParent(floor, false);
                    var platPos = new[]
                    {
                        (new Vector3(-9f, -5.2f, 0f), new Vector3(3.2f, 0.5f, 1f)),
                        (new Vector3(-1.5f, -6.1f, 0f), new Vector3(4.0f, 0.45f, 1f)),
                        (new Vector3(4.0f, -5.4f, 0f), new Vector3(2.6f, 0.4f, 1f)),
                        (new Vector3(9.5f, -5.0f, 0f), new Vector3(2.2f, 0.5f, 1f)),
                        (new Vector3(-4.0f, -7.0f, 0f), new Vector3(5.0f, 0.35f, 1f)),
                    };
                    for (var p = 0; p < platPos.Length; p++)
                    {
                        AddBlock(fp.transform, "Plat_Body_" + p, platPos[p].Item1, platPos[p].Item2, new Color(0.05f, 0.08f, 0.12f, 0.95f), 1);
                        AddLine(fp.transform, "Plat_Edge_" + p, platPos[p].Item1 + new Vector3(0f, platPos[p].Item2.y * 0.5f, 0f),
                            new Vector3(platPos[p].Item2.x, 0.04f, 1f), new Color(0.15f, 0.5f, 0.6f, 0.5f), 2);
                    }
                }
            }

            var decor = worldRoot.Find("Layer_Decor");
            if (decor != null && decor.Find("SpaceCosmicMachines") == null)
            {
                var machines = new GameObject("SpaceCosmicMachines");
                machines.transform.SetParent(decor, false);
                AddCosmicMachine(machines.transform, "CM_A", new Vector3(-12f, 0.0f, 0f), 0.85f, 1);
                AddCosmicMachine(machines.transform, "CM_B", new Vector3(12.0f, -0.2f, 0f), 0.8f, 1);
                AddCosmicMachine(machines.transform, "CM_C", new Vector3(-7.0f, 2.0f, 0f), 0.6f, 0);
                AddCosmicMachine(machines.transform, "CM_D", new Vector3(7.2f, 1.2f, 0f), 0.6f, 0);
            }

            var cellArea = worldRoot.Find("Layer_CellArea");
            if (cellArea != null && cellArea.Find("CellGridSpaceFrameZ2") == null)
                AddCellGridFrame(cellArea, CellGridCenter, CellGridSize, new Color(0.2f, 0.5f, 0.6f, 0.4f), 3);

            DarkenBaseWorldTints(worldRoot);
        }

        static void AddCellGridFrame(Transform parent, Vector2 center, Vector2 size, Color edge, int order)
        {
            var root = new GameObject("CellGridSpaceFrameZ2");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(center.x, center.y, 0f);
            var hx = size.x * 0.5f;
            var hy = size.y * 0.5f;
            var t = 0.04f;
            AddLine(root.transform, "F_T", Vector3.up * hy, new Vector3(size.x, t, 1f), edge, order);
            AddLine(root.transform, "F_B", Vector3.down * hy, new Vector3(size.x, t, 1f), edge, order);
            AddLine(root.transform, "F_L", Vector3.left * hx, new Vector3(t, size.y, 1f), edge, order);
            AddLine(root.transform, "F_R", Vector3.right * hx, new Vector3(t, size.y, 1f), edge, order);
        }

        static void AddCosmicMachine(Transform parent, string name, Vector3 localPos, float scale, int variant)
        {
            if (parent.Find(name) != null)
                return;
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localScale = new Vector3(scale, scale, 1f);
            AddBlock(root.transform, "Chassis", Vector3.up * 0.6f, new Vector3(0.5f, 1.1f, 1f), new Color(0.07f, 0.1f, 0.16f, 0.9f), 6);
            AddBlock(root.transform, "Core", new Vector3(0f, 0.15f, 0f), new Vector3(0.7f, 0.2f, 1f), new Color(0.12f, 0.2f, 0.3f, 0.8f), 6);
            for (var i = 0; i < 3; i++)
            {
                var ox = -0.2f + i * 0.2f;
                AddBlock(root.transform, "Lamp" + i, new Vector3(ox, 0.25f, 0f), new Vector3(0.1f, 0.1f, 1f), new Color(0.2f, 0.8f, 0.9f, 0.7f * scale), 7);
            }

            if (variant == 1)
            {
                AddBlock(root.transform, "ArmL", new Vector3(-0.5f, 0.4f, 0f), new Vector3(0.2f, 0.3f, 1f), new Color(0.1f, 0.12f, 0.2f, 0.8f), 5);
                AddBlock(root.transform, "ArmR", new Vector3(0.5f, 0.3f, 0f), new Vector3(0.2f, 0.4f, 1f), new Color(0.1f, 0.12f, 0.2f, 0.8f), 5);
            }
        }

        static void DarkenBaseWorldTints(Transform worldRoot)
        {
            TweakChildSprite(worldRoot, "Layer_Floor/CityFloorPlate", c => c * new Color(0.3f, 0.32f, 0.4f, 0.9f), a => Mathf.Min(1f, a * 0.9f));
            TweakChildSprite(worldRoot, "Layer_WallsBack/CondensedSkyline_Back", c => c * new Color(0.4f, 0.4f, 0.5f, 0.6f), a => a * 0.5f);
            TweakChildSprite(worldRoot, "Layer_Fog/UrbanFog", c => new Color(c.r * 0.35f, c.g * 0.4f, c.b * 0.6f, c.a), _ => 0.14f);
            TweakChildSprite(worldRoot, "Layer_Atmosphere/UrbanVignette", c => c, a => Mathf.Min(0.5f, a * 1.2f));
            TweakChildSprite(worldRoot, "Layer_CellArea/UrbanCellField", c => c * new Color(0.5f, 0.6f, 0.7f, 0.35f + 0.15f * c.a), _ => 0.45f);
        }

        static void TweakChildSprite(Transform worldRoot, string path, System.Func<Color, Color> colorF, System.Func<float, float> alphaF)
        {
            var t = worldRoot == null ? null : worldRoot.Find(path);
            if (t == null)
                return;
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null)
                return;
            var a = alphaF(sr.color.a);
            sr.color = colorF(sr.color);
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }

        static Transform EnsureChildLayer(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null)
                return t;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
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
            if (parent != null && parent.Find(name) != null)
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

        static void AddDot(Transform parent, string name, Vector3 worldOrLocal, Vector3 localScale, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = worldOrLocal;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSpriteFactory.OpaqueWhiteSprite;
            sr.color = color;
            sr.sortingOrder = order;
        }
    }

    [DisallowMultipleComponent]
    sealed class Zone2SpaceStarfieldDrift : MonoBehaviour
    {
        Vector3 _base;

        void OnEnable() => _base = transform.localPosition;

        void LateUpdate()
        {
            var t = Time.unscaledTime;
            transform.localPosition = _base + new Vector3(
                Mathf.Sin(t * 0.1f) * 0.12f,
                Mathf.Cos(t * 0.07f) * 0.1f,
                0f);
        }
    }
}
