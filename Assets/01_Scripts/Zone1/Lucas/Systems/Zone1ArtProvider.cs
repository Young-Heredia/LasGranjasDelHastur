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

        public static Sprite LoadSprite(string relativeAssetPath)
        {
            if (string.IsNullOrEmpty(relativeAssetPath))
                return null;

            if (SpriteCache.TryGetValue(relativeAssetPath, out var cached))
                return cached;

            var tex = LoadTexture(relativeAssetPath);
            if (tex == null)
                return null;

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
            SpriteCache[relativeAssetPath] = sprite;
            return sprite;
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

        /// <summary>Tira horizontal: N frames del mismo alto que la textura, ancho tex.width/N.</summary>
        public static Sprite[] LoadHorizontalStrip(string relativeAssetPath, int frameCount)
        {
            if (string.IsNullOrEmpty(relativeAssetPath) || frameCount <= 0)
                return null;

            var key = $"{relativeAssetPath}|hstrip|{frameCount}";
            if (SheetCache.TryGetValue(key, out var cached))
                return cached;

            var tex = LoadTexture(relativeAssetPath);
            if (tex == null)
                return null;

            var fw = tex.width / frameCount;
            var fh = tex.height;
            if (fw <= 0 || fh <= 0)
                return null;

            var list = new List<Sprite>(frameCount);
            for (var i = 0; i < frameCount; i++)
            {
                var rect = new Rect(i * fw, 0f, fw, fh);
                list.Add(Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 32f));
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

