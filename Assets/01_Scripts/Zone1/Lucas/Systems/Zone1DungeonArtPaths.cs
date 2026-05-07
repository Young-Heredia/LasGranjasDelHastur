namespace LasGranjasDelHastur.Zone1
{
    /// <summary>
    /// Rutas opcionales de arte de mazmorra (Zona 1). Si el PNG no existe, <see cref="Zone1Bootstrap"/> mantiene el fallback de bloques de color.
    /// </summary>
    public static class Zone1DungeonArtPaths
    {
        const string TilesDir = "Assets/02_Sprites/Lucas/Zone1/Tiles/";
        const string OverlaysDir = "Assets/02_Sprites/Lucas/Zone1/Overlays/";
        const string PropsDir = "Assets/02_Sprites/Lucas/Zone1/Props/";

        public static readonly string OptionalWallTopBand = TilesDir + "zone1_wall_stone_top_band.png";
        public static readonly string OptionalWallSideBand = TilesDir + "zone1_wall_stone_side_band.png";
        public static readonly string OptionalWallLintel = TilesDir + "zone1_wall_stone_lintel.png";
        public static readonly string OptionalWallBottomBand = TilesDir + "zone1_wall_stone_bottom_band.png";
        public static readonly string OptionalWallBrokenBreach = TilesDir + "zone1_wall_stone_broken_breach.png";

        /// <summary>Marco decorativo (borde del calabozo) sobre el área de juego.</summary>
        public static readonly string OptionalDungeonFrameOverlay = OverlaysDir + "zone1_overlay_dungeon_frame.png";

        /// <summary>Runas dedicadas amarillas; si falta, se reutiliza <c>zone1_overlay_runes.png</c> con tinte cálido.</summary>
        public static readonly string OptionalRunesYellow = OverlaysDir + "zone1_overlay_runes_yellow.png";

        /// <summary>Rubble opcional junto al hueco inferior.</summary>
        public static readonly string OptionalWallRubbleProp = PropsDir + "zone1_prop_wall_rubble_cluster.png";
    }
}
