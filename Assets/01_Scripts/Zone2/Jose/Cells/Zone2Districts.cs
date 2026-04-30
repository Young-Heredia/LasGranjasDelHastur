namespace LasGranjasDelHastur.Zone2.Jose
{
    /// <summary>Arquetipos lunares/estelares (Zona 2); 30 celdas rotan con id % 4.</summary>
    public enum Zone2DistrictType
    {
        LunarGarden = 0,
        CometMill = 1,
        PlanetaryCore = 2,
        StellarIncubator = 3,
    }

    public enum Zone2CellVisualState
    {
        Locked,
        Idle,
        Producing,
        Ready,
        Corrupted,
    }

    public static class Zone2DistrictPaths
    {
        public const string PackRoot = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/";

        public static string GetSpritePath(Zone2DistrictType t) => Zone2CellSpritePathResolver.ResolveDistrict(t);

        public static string LucasPackSpritePath(Zone2DistrictType t) =>
            t switch
            {
                Zone2DistrictType.LunarGarden => PackRoot + "Cells/Zone2/Zone2_Cell_CityCondenser.png",
                Zone2DistrictType.CometMill => PackRoot + "Cells/Zone2/Zone2_Cell_CultistTower.png",
                Zone2DistrictType.PlanetaryCore => PackRoot + "Cells/Zone2/Zone2_Cell_CursedMarket.png",
                _ => PackRoot + "Cells/Zone2/Zone2_Cell_YithArchive.png",
            };

        public static string GetDisplayName(Zone2DistrictType t) =>
            t switch
            {
                Zone2DistrictType.LunarGarden => "Huerto Lunar",
                Zone2DistrictType.CometMill => "Molino de Cometas",
                Zone2DistrictType.PlanetaryCore => "Núcleo Planetario",
                _ => "Incubadora Estelar",
            };
    }
}
