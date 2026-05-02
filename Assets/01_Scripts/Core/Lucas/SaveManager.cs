using System;
using System.IO;
using LasGranjasDelHastur.Zone1;
using LasGranjasDelHastur.Zone2;
using LasGranjasDelHastur.Zone3;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LasGranjasDelHastur.Core
{
    [DisallowMultipleComponent]
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        public const int CurrentSaveVersion = 2;

        [Header("Auto Save")]
        [SerializeField] private bool autoSave = true;
        [SerializeField] private float autoSaveEverySeconds = 20f;

        public bool ShouldRestoreFromSave { get; private set; }
        public SaveGameData CachedData { get; private set; } = new();

        float _autoSaveTimer;
        string SaveFilePath => Path.Combine(Application.persistentDataPath, "savegame.json");
        string BackupFilePath => Path.Combine(Application.persistentDataPath, "savegame.bak");
        string TempFilePath => Path.Combine(Application.persistentDataPath, "savegame.tmp");
        public string GetSaveFilePath() => SaveFilePath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureInstance()
        {
            if (Instance != null)
                return;
            var go = new GameObject("SaveManager");
            go.AddComponent<SaveManager>();
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
            LoadFromDisk();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Update()
        {
            if (!autoSave)
                return;

            _autoSaveTimer += Time.unscaledDeltaTime;
            if (_autoSaveTimer < Mathf.Max(5f, autoSaveEverySeconds))
                return;
            _autoSaveTimer = 0f;
            SaveNow();
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveNow();
        }

        void OnApplicationQuit()
        {
            SaveNow();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (CachedData == null)
                CachedData = new SaveGameData();
            CachedData.lastSceneName = scene.name;
        }

        public bool HasSaveFile()
        {
            return File.Exists(SaveFilePath);
        }

        public void RequestRestoreOnNextGameplayScene()
        {
            ShouldRestoreFromSave = true;
            LoadFromDisk();
        }

        public void MarkRestoreConsumed()
        {
            ShouldRestoreFromSave = false;
        }

        public void SaveNow()
        {
            if (CachedData == null)
                CachedData = new SaveGameData();

            CachedData.saveVersion = CurrentSaveVersion;

            var zone1 = FindFirstObjectByType<Zone1Manager>();
            if (zone1 != null)
            {
                CachedData.zone1 = zone1.CaptureSaveData();
                CachedData.zone1Available = CachedData.zone1 != null && CachedData.zone1.valid;
            }

            var zone2 = FindFirstObjectByType<LasGranjasDelHastur.Zone2.Jose.Zone2PrototypeGame>();
            if (zone2 != null)
            {
                CachedData.zone2 = zone2.CaptureSaveData();
                CachedData.zone2Available = CachedData.zone2 != null && CachedData.zone2.valid;
            }

            var zone3 = FindFirstObjectByType<Zone3PrototypeGame>();
            if (zone3 != null)
            {
                CachedData.zone3 = zone3.CaptureSaveData();
                CachedData.zone3Available = CachedData.zone3 != null && CachedData.zone3.valid;
            }

            CachedData.savedAtUtc = DateTime.UtcNow.ToString("o");
            WriteToDisk(CachedData);
        }

        public void DeleteSaveFile()
        {
            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);
            if (File.Exists(BackupFilePath))
                File.Delete(BackupFilePath);
            if (File.Exists(TempFilePath))
                File.Delete(TempFilePath);
            CachedData = new SaveGameData();
            ShouldRestoreFromSave = false;
        }

        public void StartNewGame()
        {
            ResetAllProgress(resetIntroSeen: true);
        }

        public void ResetAllProgress(bool resetIntroSeen = true)
        {
            DeleteSaveFile();

            // Save baseline so "Continue" and restore flow start from a clean state.
            CachedData = new SaveGameData
            {
                saveVersion = CurrentSaveVersion,
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                lastSceneName = "MainMenu",
                globalTaxStrikes = 0,
                zone1 = new Zone1SaveData(),
                zone2 = new Zone2SaveData(),
                zone3 = new Zone3SaveData(),
                zone1Available = false,
                zone2Available = false,
                zone3Available = false,
            };
            WriteToDisk(CachedData);

            // Global progression gates and one-shot flags.
            global::ZoneProgressState.SetZoneUnlocked(2, false);
            global::ZoneProgressState.SetZoneUnlocked(3, false);
            PlayerPrefs.DeleteKey("LasGranjas_Zone2_Minigame_Completed");
            PlayerPrefs.DeleteKey("LasGranjas_Zone3_Minigame_Completed");
            PlayerPrefs.DeleteKey("LasGranjas_Zone3_PrestigePoints");
            if (resetIntroSeen)
                PlayerPrefs.DeleteKey("IntroSeen");
            PlayerPrefs.Save();
        }

        void LoadFromDisk()
        {
            if (!File.Exists(SaveFilePath))
            {
                CachedData = new SaveGameData();
                return;
            }

            try
            {
                var json = File.ReadAllText(SaveFilePath);
                var loaded = JsonUtility.FromJson<SaveGameData>(json);
                CachedData = MigrateIfNeeded(loaded ?? new SaveGameData());
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: failed to read save file. {e.Message}");
                if (TryLoadBackup(out var backupData))
                    CachedData = backupData;
                else
                    CachedData = new SaveGameData();
            }
        }

        SaveGameData MigrateIfNeeded(SaveGameData data)
        {
            if (data == null)
                data = new SaveGameData();

            if (data.saveVersion <= 0)
                data.saveVersion = 1;

            // Future-proof migration chain.
            while (data.saveVersion < CurrentSaveVersion)
            {
                switch (data.saveVersion)
                {
                    case 1:
                        var z1s = data.zone1 != null ? data.zone1.strikes : 0;
                        var z2s = data.zone2 != null ? data.zone2.strikes : 0;
                        var z3s = data.zone3 != null ? data.zone3.strikes : 0;
                        data.globalTaxStrikes = Mathf.Max(Mathf.Max(Mathf.Max(data.globalTaxStrikes, z1s), z2s), z3s);
                        data.saveVersion = 2;
                        break;
                    default:
                        data.saveVersion = CurrentSaveVersion;
                        break;
                }
            }

            // Ensure safe defaults after migration.
            data.lastSceneName = string.IsNullOrWhiteSpace(data.lastSceneName) ? "MainMenu" : data.lastSceneName;
            data.zone1 ??= new Zone1SaveData();
            data.zone2 ??= new Zone2SaveData();
            data.zone3 ??= new Zone3SaveData();
            return data;
        }

        void WriteToDisk(SaveGameData data)
        {
            try
            {
                var json = JsonUtility.ToJson(data, true);
                File.WriteAllText(TempFilePath, json);

                if (File.Exists(SaveFilePath))
                    File.Copy(SaveFilePath, BackupFilePath, overwrite: true);

                File.Copy(TempFilePath, SaveFilePath, overwrite: true);
                File.Delete(TempFilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: failed to write save file. {e.Message}");
            }
        }

        bool TryLoadBackup(out SaveGameData data)
        {
            data = null;
            if (!File.Exists(BackupFilePath))
                return false;

            try
            {
                var json = File.ReadAllText(BackupFilePath);
                var loaded = JsonUtility.FromJson<SaveGameData>(json);
                data = MigrateIfNeeded(loaded ?? new SaveGameData());
                Debug.LogWarning("SaveManager: loaded backup save after primary read failure.");
                return true;
            }
            catch (Exception backupError)
            {
                Debug.LogWarning($"SaveManager: failed to read backup save. {backupError.Message}");
                return false;
            }
        }
    }
}

