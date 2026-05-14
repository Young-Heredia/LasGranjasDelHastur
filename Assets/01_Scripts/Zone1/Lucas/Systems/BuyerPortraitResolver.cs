using System.Collections.Generic;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    /// <summary>
    /// Rutas de arte para compradores: primero <c>Resources/Edwin/Buyers/buyer-*-portrait-preview.png</c>,
    /// luego Lucas/Zone1, luego nombres legacy <c>edwin-buyer-*</c>.
    /// Los íconos de lista intentan el mismo preview, luego <c>-icon</c>, luego el retrato vía <see cref="GetPortraitSprite"/>.
    /// </summary>
    public static class BuyerPortraitResolver
    {
        const string EdwinRoot = "Assets/Resources/Edwin/Buyers/";

        static readonly Dictionary<string, string[]> PortraitPathsByName = new()
        {
            ["Los Profundos"] = new[]
            {
                EdwinRoot + "buyer-los-profundos-portrait-preview.png",
                "Assets/02_Sprites/Lucas/Zone1/Portraits/zone1_buyer_deepone_portrait.png",
                EdwinRoot + "edwin-buyer-los-profundos-portrait.png",
            },
            ["Yekuvian"] = new[]
            {
                EdwinRoot + "buyer-yekuvian-portrait-preview.png",
                "Assets/02_Sprites/Lucas/Zone1/Portraits/zone1_buyer_yekuvian_portrait.png",
                EdwinRoot + "edwin-buyer-yekuvian-portrait.png",
            },
            ["Ángeles Caídos"] = new[]
            {
                EdwinRoot + "buyer-angeles-caidos-portrait-preview.png",
                "Assets/02_Sprites/Lucas/Zone1/Portraits/zone1_buyer_fallenangel_portrait.png",
                EdwinRoot + "edwin-buyer-angeles-caidos-portrait.png",
            },
            ["Nyarlathotep"] = new[]
            {
                EdwinRoot + "buyer-nyarlathotep-portrait-preview.png",
                EdwinRoot + "edwin-buyer-nyarlathotep-portrait.png",
                "Assets/02_Sprites/Lucas/Zone1/Portraits/zone1_buyer_nyarlathotep_portrait.png",
            },
            ["Gran Raza de Yith"] = new[]
            {
                EdwinRoot + "buyer-gran-raza-yith-portrait-preview.png",
                EdwinRoot + "edwin-buyer-gran-raza-yith-portrait.png",
                "Assets/02_Sprites/Lucas/Zone1/Portraits/zone1_buyer_yith_portrait.png",
            },
            ["Mirin Igri"] = new[]
            {
                EdwinRoot + "buyer-mirin-igri-portrait-preview.png",
                EdwinRoot + "edwin-buyer-mirin-igri-portrait.png",
                "Assets/02_Sprites/Lucas/Zone1/Portraits/zone1_buyer_mirin_igri_portrait.png",
            },
            ["Ubo Satla"] = new[]
            {
                EdwinRoot + "buyer-ubo-satla-portrait-preview.png",
                EdwinRoot + "edwin-buyer-ubo-satla-portrait.png",
                "Assets/02_Sprites/Lucas/Zone1/Portraits/zone1_buyer_ubo_satla_portrait.png",
            },
            ["Yog-Sothoth"] = new[]
            {
                EdwinRoot + "buyer-yog-sothoth-portrait-preview.png",
                EdwinRoot + "edwin-buyer-yog-sothoth-portrait.png",
                "Assets/02_Sprites/Lucas/Zone1/Portraits/zone1_buyer_yog_sothoth_portrait.png",
            },
            ["Shoggoth"] = new[]
            {
                EdwinRoot + "buyer-shoggoth-portrait-preview.png",
                EdwinRoot + "edwin-buyer-shoggoth-portrait.png",
                "Assets/02_Sprites/Lucas/Zone1/Portraits/zone1_buyer_shoggoth_portrait.png",
            },
        };

        static readonly Dictionary<string, string[]> IconPathsByName = new()
        {
            ["Los Profundos"] = new[]
            {
                EdwinRoot + "buyer-los-profundos-portrait-preview.png",
                EdwinRoot + "buyer-los-profundos-icon-preview.png",
                EdwinRoot + "edwin-buyer-los-profundos-icon.png",
                "Assets/02_Sprites/Lucas/Zone1/Icons/zone1_buyer_deepone_icon.png",
            },
            ["Yekuvian"] = new[]
            {
                EdwinRoot + "buyer-yekuvian-portrait-preview.png",
                EdwinRoot + "buyer-yekuvian-icon-preview.png",
                EdwinRoot + "edwin-buyer-yekuvian-icon.png",
                "Assets/02_Sprites/Lucas/Zone1/Icons/zone1_buyer_yekuvian_icon.png",
            },
            ["Ángeles Caídos"] = new[]
            {
                EdwinRoot + "buyer-angeles-caidos-portrait-preview.png",
                EdwinRoot + "buyer-angeles-caidos-icon-preview.png",
                EdwinRoot + "edwin-buyer-angeles-caidos-icon.png",
                "Assets/02_Sprites/Lucas/Zone1/Icons/zone1_buyer_fallenangel_icon.png",
            },
            ["Nyarlathotep"] = new[]
            {
                EdwinRoot + "buyer-nyarlathotep-portrait-preview.png",
                EdwinRoot + "buyer-nyarlathotep-icon-preview.png",
                EdwinRoot + "edwin-buyer-nyarlathotep-icon.png",
                "Assets/02_Sprites/Lucas/Zone1/Icons/zone1_buyer_nyarlathotep_icon.png",
            },
            ["Gran Raza de Yith"] = new[]
            {
                EdwinRoot + "buyer-gran-raza-yith-portrait-preview.png",
                EdwinRoot + "buyer-gran-raza-yith-icon-preview.png",
                EdwinRoot + "edwin-buyer-gran-raza-yith-icon.png",
                "Assets/02_Sprites/Lucas/Zone1/Icons/zone1_buyer_yith_icon.png",
            },
            ["Mirin Igri"] = new[]
            {
                EdwinRoot + "buyer-mirin-igri-portrait-preview.png",
                EdwinRoot + "buyer-mirin-igri-icon-preview.png",
                EdwinRoot + "edwin-buyer-mirin-igri-icon.png",
                "Assets/02_Sprites/Lucas/Zone1/Icons/zone1_buyer_mirin_igri_icon.png",
            },
            ["Ubo Satla"] = new[]
            {
                EdwinRoot + "buyer-ubo-satla-portrait-preview.png",
                EdwinRoot + "buyer-ubo-satla-icon-preview.png",
                EdwinRoot + "edwin-buyer-ubo-satla-icon.png",
                "Assets/02_Sprites/Lucas/Zone1/Icons/zone1_buyer_ubo_satla_icon.png",
            },
            ["Yog-Sothoth"] = new[]
            {
                EdwinRoot + "buyer-yog-sothoth-portrait-preview.png",
                EdwinRoot + "buyer-yog-sothoth-icon-preview.png",
                EdwinRoot + "edwin-buyer-yog-sothoth-icon.png",
                "Assets/02_Sprites/Lucas/Zone1/Icons/zone1_buyer_yog_sothoth_icon.png",
            },
            ["Shoggoth"] = new[]
            {
                EdwinRoot + "buyer-shoggoth-portrait-preview.png",
                EdwinRoot + "buyer-shoggoth-icon-preview.png",
                EdwinRoot + "edwin-buyer-shoggoth-icon.png",
                "Assets/02_Sprites/Lucas/Zone1/Icons/zone1_buyer_shoggoth_icon.png",
            },
        };

        public static Sprite GetPortraitSprite(string buyerName) =>
            LoadFirstAvailable(PortraitPathsByName, buyerName);

        /// <summary>Ícono para filas compactas; si no hay PNG de ícono, usa el retrato (incl. <c>buyer-*-portrait-preview</c>).</summary>
        public static Sprite GetListIconSprite(string buyerName)
        {
            var icon = LoadFirstAvailable(IconPathsByName, buyerName);
            return icon ?? GetPortraitSprite(buyerName);
        }

        static Sprite LoadFirstAvailable(Dictionary<string, string[]> map, string buyerName)
        {
            if (string.IsNullOrEmpty(buyerName) || !map.TryGetValue(buyerName, out var paths))
                return null;
            foreach (var path in paths)
            {
                var resourcesPath = global::LasGranjasDelHastur.TaxCollectorArtPaths.ToResourcesLoadPath(path);
                if (!string.IsNullOrEmpty(resourcesPath))
                {
                    var fromResources = Resources.Load<Sprite>(resourcesPath);
                    if (fromResources != null)
                        return fromResources;
                }

                var s = Zone1ArtProvider.LoadSprite(path);
                if (s != null)
                    return s;
            }

            return null;
        }
    }
}
