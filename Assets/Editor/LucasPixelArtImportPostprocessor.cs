using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Configura import settings para pixel art del asset pack de Lucas.
/// Evita blur, mipmaps y compresión que arruinan lectura en UI/2D.
/// </summary>
public sealed class LucasPixelArtImportPostprocessor : AssetPostprocessor
{
    const string PackRoot = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/";

    void OnPreprocessTexture()
    {
        if (string.IsNullOrEmpty(assetPath))
            return;

        var normalized = assetPath.Replace('\\', '/');
        if (!normalized.StartsWith(PackRoot))
            return;

        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.sRGBTexture = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.isReadable = false;

        // Valores razonables y consistentes; se pueden ajustar por sprite si hace falta.
        ti.spritePixelsPerUnit = 32f;

        // Para mantener estilo pixel art sin reescalado agresivo.
        ti.maxTextureSize = 2048;

        // Heurística: sheets suelen querer tamaño mayor y siguen siendo sprites.
        if (Path.GetFileNameWithoutExtension(normalized).Contains("_Sheet"))
            ti.maxTextureSize = 4096;
    }
}

