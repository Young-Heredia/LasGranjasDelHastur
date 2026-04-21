using UnityEngine;

/// <summary>
/// Progreso de desbloqueo de zonas (PlayerPrefs). Zona 1 siempre disponible.
/// Más adelante: llamar a <see cref="SetZoneUnlocked"/> desde el gameplay de la zona anterior.
/// </summary>
public static class ZoneProgressState
{
    const string KeyZone2 = "LasGranjas_Zone2_Unlocked";
    const string KeyZone3 = "LasGranjas_Zone3_Unlocked";

    public static bool IsZoneUnlocked(int zoneNumber)
    {
        switch (zoneNumber)
        {
            case 1:
                return true;
            case 2:
                return PlayerPrefs.GetInt(KeyZone2, 0) == 1;
            case 3:
                return PlayerPrefs.GetInt(KeyZone3, 0) == 1;
            default:
                return false;
        }
    }

    public static void SetZoneUnlocked(int zoneNumber, bool unlocked)
    {
        if (zoneNumber == 2)
            PlayerPrefs.SetInt(KeyZone2, unlocked ? 1 : 0);
        else if (zoneNumber == 3)
            PlayerPrefs.SetInt(KeyZone3, unlocked ? 1 : 0);

        PlayerPrefs.Save();
    }
}
