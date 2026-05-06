using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    public static class Zone1ArtProvider
    {
        static readonly Dictionary<string, Sprite> SpriteCache = new();
        static readonly Dictionary<string, Texture2D> TextureCache = new();
        static readonly Dictionary<string, Sprite[]> SheetCache = new();

        /// <summary>
        /// Alto/ancho objetivo en unidades mundo para edificios Z2 cargados como PNG sueltos (el tamaño real lo marca la textura).
        /// Debe ser compatible con <see cref="CellManager"/> (<c>BoxCollider2D</c> ~1×1 y paso de rejilla ~2.14).
        /// </summary>
        const float Zone2BuildingSpriteMaxExtentWorld = 2.05f;

        public static Sprite LoadSprite(string relativeAssetPath)
        {
            if (string.IsNullOrEmpty(relativeAssetPath))
                return null;

            if (SpriteCache.TryGetValue(relativeAssetPath, out var cached))
                return cached;

            var tex = LoadTexture(relativeAssetPath);
            if (tex == null)
                return null;

            var ppu = ResolvePixelsPerUnit(relativeAssetPath, tex);
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
            SpriteCache[relativeAssetPath] = sprite;
            return sprite;
        }

        static float ResolvePixelsPerUnit(string relativeAssetPath, Texture2D tex)
        {
            var norm = relativeAssetPath.Replace('\\', '/');
            if (norm.IndexOf("Lucas/Zone2/Cells/Buildings/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var dim = Mathf.Max(tex.width, tex.height);
                return Mathf.Max(32f, dim / Zone2BuildingSpriteMaxExtentWorld);
            }

            return 32f;
        }

        public static Sprite[] LoadSheet(string relativeAssetPath, int frameWidth, int frameHeight)
        {
            var key = $"{relativeAssetPath}|{frameWidth}x{frameHeight}";
            if (SheetCache.TryGetValue(key, out var cached))
                return cached;

            var tex = LoadTexture(relativeAssetPath);
            if (tex == null || frameWidth <= 0 || frameHeight <= 0)
                return null;

            var cols = Mathf.Max(1, tex.width / frameWidth);
            var rows = Mathf.Max(1, tex.height / frameHeight);
            var list = new List<Sprite>(cols * rows);

            for (var y = rows - 1; y >= 0; y--)
            {
                for (var x = 0; x < cols; x++)
                {
                    var rect = new Rect(x * frameWidth, y * frameHeight, frameWidth, frameHeight);
                    var sp = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 32f);
                    list.Add(sp);
                }
            }

            var result = list.ToArray();
            SheetCache[key] = result;
            return result;
        }

        /// <summary>Corta una textura en una rejilla uniforme (ej. spritesheet 3×3). Orden de frames: fila superior→inferior, izquierda→derecha.</summary>
        public static Sprite[] LoadSheetUniformGrid(string relativeAssetPath, int columns, int rows, float pixelsPerUnit = 32f)
        {
            columns = Mathf.Max(1, columns);
            rows = Mathf.Max(1, rows);
            var key = $"{relativeAssetPath}|ug|{columns}x{rows}|{pixelsPerUnit}";
            if (SheetCache.TryGetValue(key, out var cached))
                return cached;

            var tex = LoadTexture(relativeAssetPath);
            if (tex == null)
                return null;

            var fw = tex.width / columns;
            var fh = tex.height / rows;
            if (fw <= 0 || fh <= 0)
                return null;

            var list = new List<Sprite>(columns * rows);
            for (var y = rows - 1; y >= 0; y--)
            {
                for (var x = 0; x < columns; x++)
                {
                    var rect = new Rect(x * fw, y * fh, fw, fh);
                    list.Add(Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), Mathf.Max(1f, pixelsPerUnit)));
                }
            }

            var result = list.ToArray();
            SheetCache[key] = result;
            return result;
        }

        static Texture2D LoadTexture(string relativeAssetPath)
        {
            if (TextureCache.TryGetValue(relativeAssetPath, out var cached))
                return cached;

            var fullPath = Path.Combine(Application.dataPath, relativeAssetPath.Replace("Assets/", ""));
            if (!File.Exists(fullPath))
                return null;

            var bytes = File.ReadAllBytes(fullPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes))
                return null;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = Path.GetFileNameWithoutExtension(relativeAssetPath);
            TextureCache[relativeAssetPath] = tex;
            return tex;
        }
    }
}

