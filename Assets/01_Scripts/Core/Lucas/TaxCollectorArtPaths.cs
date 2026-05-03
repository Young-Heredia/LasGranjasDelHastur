using System;

namespace LasGranjasDelHastur
{
    /// <summary>Rutas de arte de impuesto / recaudador (PNG bajo Resources, legibles también vía <see cref="Zone1.Zone1ArtProvider"/>).</summary>
    public static class TaxCollectorArtPaths
    {
        const string EdwinTaxDir = "Assets/Resources/Edwin/Tax/";
        const string ResourcesPrefix = "Assets/Resources/";

        public const string Z2Panel = EdwinTaxDir + "zone2-ui-panel-tax-kthanid-preview.png";
        public const string Z2Portrait = EdwinTaxDir + "zone2-tax-kthanid-portrait-preview.png";
        public const string Z3Panel = EdwinTaxDir + "zone3-ui-panel-tax-azathoth-preview.png";
        public const string Z3Seal = EdwinTaxDir + "zone3-tax-azathoth-seal-preview.png";
        public const string ArrivalSheet4Frames = EdwinTaxDir + "tax-collector-arrival-sheet-4f.png";
        public const int ArrivalFrameCount = 4;

        /// <summary>Ruta para <see cref="UnityEngine.Resources.Load{T}(string)"/> (sin extensión), o null si no está bajo Assets/Resources/.</summary>
        public static string ToResourcesLoadPath(string assetPathUnderResources)
        {
            if (string.IsNullOrEmpty(assetPathUnderResources))
                return null;
            if (!assetPathUnderResources.StartsWith(ResourcesPrefix, StringComparison.OrdinalIgnoreCase))
                return null;
            var rel = assetPathUnderResources.Substring(ResourcesPrefix.Length);
            var dot = rel.LastIndexOf('.');
            if (dot > 0)
                rel = rel.Substring(0, dot);
            return rel;
        }
    }
}
