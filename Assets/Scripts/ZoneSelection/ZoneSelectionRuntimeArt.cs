using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera iconos simples en runtime para las tarjetas de zonas.
/// Mantiene el selector legible aunque falten referencias manuales en la escena.
/// </summary>
public static class ZoneSelectionRuntimeArt
{
    static readonly Dictionary<int, Sprite> ZoneIcons = new();

    public static Sprite GetZoneIcon(int zoneNumber)
    {
        if (ZoneIcons.TryGetValue(zoneNumber, out var cached) && cached != null)
            return cached;

        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"zone_selector_icon_{zoneNumber}"
        };

        var clear = new Color32(0, 0, 0, 0);
        var pixels = new Color32[64 * 64];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = clear;
        tex.SetPixels32(pixels);

        var gold = new Color32(226, 198, 110, 255);
        var ember = new Color32(250, 212, 133, 255);
        var dark = new Color32(28, 18, 40, 255);
        var zone1 = new Color32(151, 104, 68, 255);
        var zone2 = new Color32(99, 117, 161, 255);
        var zone3 = new Color32(175, 116, 214, 255);

        FillCircle(tex, 32, 32, 28, new Color32(17, 11, 28, 245));
        DrawCircle(tex, 32, 32, 28, gold, thickness: 2);
        DrawCircle(tex, 32, 32, 22, dark, thickness: 1);

        switch (zoneNumber)
        {
            case 1:
                FillRect(tex, 18, 19, 28, 26, zone1);
                FillRect(tex, 24, 25, 16, 20, dark);
                DrawRectOutline(tex, 18, 19, 28, 26, ember, thickness: 2);
                DrawLine(tex, 18, 32, 46, 32, ember, thickness: 1);
                break;

            case 2:
                FillRect(tex, 18, 21, 10, 24, zone2);
                FillRect(tex, 29, 16, 8, 29, zone2);
                FillRect(tex, 39, 24, 8, 21, zone2);
                DrawRectOutline(tex, 18, 21, 10, 24, ember, thickness: 1);
                DrawRectOutline(tex, 29, 16, 8, 29, ember, thickness: 1);
                DrawRectOutline(tex, 39, 24, 8, 21, ember, thickness: 1);
                DrawLine(tex, 16, 45, 48, 45, ember, thickness: 2);
                break;

            default:
                FillCircle(tex, 29, 29, 12, zone3);
                FillCircle(tex, 34, 27, 11, new Color32(17, 11, 28, 245));
                DrawCircle(tex, 29, 29, 12, ember, thickness: 1);
                DrawStar(tex, 43, 20, 5, gold);
                DrawOrbit(tex, 32, 35, 15, 8, ember);
                break;
        }

        tex.Apply(false, false);
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = $"zone_selector_icon_{zoneNumber}";
        ZoneIcons[zoneNumber] = sprite;
        return sprite;
    }

    static void FillRect(Texture2D tex, int x, int y, int width, int height, Color32 color)
    {
        for (var py = y; py < y + height; py++)
        for (var px = x; px < x + width; px++)
            SafeSetPixel(tex, px, py, color);
    }

    static void DrawRectOutline(Texture2D tex, int x, int y, int width, int height, Color32 color, int thickness)
    {
        for (var i = 0; i < thickness; i++)
        {
            for (var px = x; px < x + width; px++)
            {
                SafeSetPixel(tex, px, y + i, color);
                SafeSetPixel(tex, px, y + height - 1 - i, color);
            }

            for (var py = y; py < y + height; py++)
            {
                SafeSetPixel(tex, x + i, py, color);
                SafeSetPixel(tex, x + width - 1 - i, py, color);
            }
        }
    }

    static void FillCircle(Texture2D tex, int cx, int cy, int radius, Color32 color)
    {
        var r2 = radius * radius;
        for (var y = -radius; y <= radius; y++)
        for (var x = -radius; x <= radius; x++)
            if (x * x + y * y <= r2)
                SafeSetPixel(tex, cx + x, cy + y, color);
    }

    static void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color32 color, int thickness)
    {
        var outer = radius * radius;
        var innerRadius = Mathf.Max(0, radius - thickness);
        var inner = innerRadius * innerRadius;
        for (var y = -radius; y <= radius; y++)
        for (var x = -radius; x <= radius; x++)
        {
            var d = x * x + y * y;
            if (d <= outer && d >= inner)
                SafeSetPixel(tex, cx + x, cy + y, color);
        }
    }

    static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color32 color, int thickness)
    {
        var dx = Mathf.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Mathf.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            for (var ox = -thickness; ox <= thickness; ox++)
            for (var oy = -thickness; oy <= thickness; oy++)
                SafeSetPixel(tex, x0 + ox, y0 + oy, color);

            if (x0 == x1 && y0 == y1)
                break;

            var e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    static void DrawStar(Texture2D tex, int cx, int cy, int radius, Color32 color)
    {
        DrawLine(tex, cx - radius, cy, cx + radius, cy, color, thickness: 1);
        DrawLine(tex, cx, cy - radius, cx, cy + radius, color, thickness: 1);
        DrawLine(tex, cx - radius + 1, cy - radius + 1, cx + radius - 1, cy + radius - 1, color, thickness: 0);
        DrawLine(tex, cx - radius + 1, cy + radius - 1, cx + radius - 1, cy - radius + 1, color, thickness: 0);
    }

    static void DrawOrbit(Texture2D tex, int cx, int cy, int rx, int ry, Color32 color)
    {
        for (var angle = 0; angle < 360; angle += 2)
        {
            var radians = angle * Mathf.Deg2Rad;
            var x = cx + Mathf.RoundToInt(Mathf.Cos(radians) * rx);
            var y = cy + Mathf.RoundToInt(Mathf.Sin(radians) * ry);
            SafeSetPixel(tex, x, y, color);
        }
    }

    static void SafeSetPixel(Texture2D tex, int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= tex.width || y >= tex.height)
            return;
        tex.SetPixel(x, y, color);
    }
}
