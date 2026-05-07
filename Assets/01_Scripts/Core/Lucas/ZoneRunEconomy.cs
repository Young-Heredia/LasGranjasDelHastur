using UnityEngine;
using UnityEngine.SceneManagement;

namespace LasGranjasDelHastur.Core
{
    /// <summary>
    /// Ajustes de economía por escena activa (recursos compartidos, reglas distintas por zona).
    /// Zona 2: producción por ciclo respecto a las mismas definiciones que Zona 1.
    /// </summary>
    public static class ZoneRunEconomy
    {
        public const float Zone2ProductionVsZone1Multiplier = 4f;
        public const float Zone3ProductionVsZone2Multiplier = 4f;
        public const float Zone3ProductionVsZone1Multiplier = Zone2ProductionVsZone1Multiplier * Zone3ProductionVsZone2Multiplier;

        /// <summary>√4: escala el término por celdas en el % efectivo sin disparar el impuesto linealmente ×4.</summary>
        public static float Zone2TaxPercentBracketWeight => Mathf.Sqrt(Zone2ProductionVsZone1Multiplier);

        public const float Zone2TaxFlatPerPurchasedCellWeight = Zone2ProductionVsZone1Multiplier;

        public static float GetProductionMultiplierForActiveScene() =>
            SceneManager.GetActiveScene().name switch
            {
                "Zone2_Cities" => Zone2ProductionVsZone1Multiplier,
                "Zone3_Celestial" => Zone3ProductionVsZone1Multiplier,
                _ => 1f
            };

        public static string GetHudZoneTitle() =>
            SceneManager.GetActiveScene().name switch
            {
                "Zone2_Cities" => "Zona 2 - Ciudades",
                "Zone3_Celestial" => "Zona 3 - Celestial",
                _ => "Zona 1 - Calabozos"
            };

        public static string GetCellPanelEconomySuffix() =>
            SceneManager.GetActiveScene().name switch
            {
                "Zone2_Cities" => $" (×{Zone2ProductionVsZone1Multiplier:n0} en esta zona)",
                "Zone3_Celestial" => $" (×{Zone3ProductionVsZone1Multiplier:n0} en esta zona)",
                _ => ""
            };

        /// <summary>Texto breve en el panel de alerta fiscal (solo Zona 2).</summary>
        public static string GetTaxPanelEconomyNote()
        {
            if (SceneManager.GetActiveScene().name != "Zone2_Cities")
                return "";
            return "\n\n<size=88%><color=#b8c4d4>Nota (Ciudades): la producción por ciclo es ×" +
                   $"{Zone2ProductionVsZone1Multiplier:n0} respecto a Calabozos. El monto del impuesto sigue las mismas reglas dinámicas, con factores ajustados a esta zona (no es un ×4 lineal sobre todo).</color></size>";
        }

        public static string GetActionHintLine() =>
            SceneManager.GetActiveScene().name switch
            {
                "Zone2_Cities" =>
                    "Tip: En Ciudades cada ciclo aporta más recursos (×4) que en Calabozos; abre una celda para ver montos y tiempos actualizados.",
                "Zone3_Celestial" =>
                    "Tip: En Celestial cada ciclo aporta ×4 respecto a Ciudades (×16 vs Calabozos).",
                _ =>
                    "Tip: Click en una celda para abrir panel y usar Producir / Recolectar / Mejorar."
            };
    }
}
