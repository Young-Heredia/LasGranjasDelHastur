using LasGranjasDelHastur.Core;
using UnityEngine;

/// <summary>
/// Centraliza reglas de desbloqueo de zonas y minijuegos.
/// </summary>
[DisallowMultipleComponent]
public class ZoneManager : MonoBehaviour
{
    const string Zone2UnlockMiniGameKey = "LasGranjas_Zone2_Minigame_Completed";
    const string Zone3UnlockMiniGameKey = "LasGranjas_Zone3_Minigame_Completed";
    public const string Zone2UnlockMiniGameId = "zone2_unlock_trial";
    public const string Zone3UnlockMiniGameId = "zone3_alignment_trial";

    public static ZoneManager Instance { get; private set; }

    [Header("Zone Unlock Rules")]
    [SerializeField, Min(1)] private int levelRequiredForZone2 = 1;
    [SerializeField, Min(1)] private int levelRequiredForZone3 = 2;

    public int LevelRequiredForZone2 => levelRequiredForZone2;
    public int LevelRequiredForZone3 => levelRequiredForZone3;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureInstance()
    {
        if (Instance != null)
            return;
        var go = new GameObject("ZoneManager");
        go.AddComponent<ZoneManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsZoneUnlocked(int zoneNumber)
    {
        return ZoneProgressState.IsZoneUnlocked(zoneNumber);
    }

    public bool IsMiniGameCompleted(string miniGameId)
    {
        if (miniGameId == Zone2UnlockMiniGameId)
            return PlayerPrefs.GetInt(Zone2UnlockMiniGameKey, 0) == 1;
        if (miniGameId == Zone3UnlockMiniGameId)
            return PlayerPrefs.GetInt(Zone3UnlockMiniGameKey, 0) == 1;
        return false;
    }

    public int GetCurrentPlayerLevel()
    {
        return GetCurrentZone1Level();
    }

    public bool CanAttemptZone2Unlock(out string reason)
    {
        if (ZoneProgressState.IsZoneUnlocked(2))
        {
            reason = "";
            return true;
        }

        var level = GetCurrentZone1Level();
        if (level < levelRequiredForZone2)
        {
            reason = $"Requiere nivel {levelRequiredForZone2}. Nivel actual: {level}.";
            return false;
        }

        reason = "";
        return true;
    }

    public void CompleteZone2Unlock()
    {
        PlayerPrefs.SetInt(Zone2UnlockMiniGameKey, 1);
        ZoneProgressState.SetZoneUnlocked(2, true);
        PlayerPrefs.Save();
    }

    public bool CanAttemptZone3Unlock(out string reason)
    {
        if (ZoneProgressState.IsZoneUnlocked(3))
        {
            reason = "";
            return true;
        }

        if (!ZoneProgressState.IsZoneUnlocked(2))
        {
            reason = "Requiere desbloquear Zona 2 primero.";
            return false;
        }

        var level = GetCurrentZone1Level();
        if (level < levelRequiredForZone3)
        {
            reason = $"Requiere nivel {levelRequiredForZone3}. Nivel actual: {level}.";
            return false;
        }

        reason = "";
        return true;
    }

    public void CompleteZone3Unlock()
    {
        PlayerPrefs.SetInt(Zone3UnlockMiniGameKey, 1);
        ZoneProgressState.SetZoneUnlocked(3, true);
        PlayerPrefs.Save();
    }

    public void ResetAllUnlocksForDebug()
    {
        ZoneProgressState.SetZoneUnlocked(2, false);
        ZoneProgressState.SetZoneUnlocked(3, false);
        PlayerPrefs.DeleteKey(Zone2UnlockMiniGameKey);
        PlayerPrefs.DeleteKey(Zone3UnlockMiniGameKey);
        PlayerPrefs.Save();
    }

    int GetCurrentZone1Level()
    {
        var zone1Runtime = FindFirstObjectByType<LasGranjasDelHastur.Zone1.Zone1Manager>();
        if (zone1Runtime != null && zone1Runtime.Progression != null)
            return Mathf.Max(1, zone1Runtime.Progression.Level);

        if (SaveManager.Instance != null && SaveManager.Instance.CachedData != null &&
            SaveManager.Instance.CachedData.zone1 != null && SaveManager.Instance.CachedData.zone1.valid)
            return Mathf.Max(1, SaveManager.Instance.CachedData.zone1.level);

        return 1;
    }
}
