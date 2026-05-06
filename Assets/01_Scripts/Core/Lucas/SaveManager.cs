using System;
using System.IO;
using LasGranjasDelHastur.Zone1;
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
            else
            {
                // Z2/Z3 can reuse the Zone1 stack without Zone1Manager; capture a Zone1-like snapshot directly.
                var rm = FindFirstObjectByType<ResourceManager>();
                var pm = FindFirstObjectByType<ProgressionManager>();
                var cm = FindFirstObjectByType<CellManager>();
                var am = FindFirstObjectByType<AssistantManager>();
                var tm = FindFirstObjectByType<TaxManager>();
                if (rm != null && pm != null && cm != null && am != null && tm != null)
                {
                    CachedData.zone1 = CaptureZone1Like(rm, pm, cm, am, tm);
                    CachedData.zone1Available = CachedData.zone1 != null && CachedData.zone1.valid;
                }
            }

            // No longer depend on legacy prototype games.
            var zone2Mgr = FindFirstObjectByType<LasGranjasDelHastur.Zone2.Jose.Zone2Manager>();
            if (zone2Mgr != null)
            {
                CachedData.zone2Available = true;
                CachedData.zone2 ??= new Zone2SaveData();
                CachedData.zone2.valid = true;
                CachedData.zone2.darkCoins = CachedData.zone1 != null ? CachedData.zone1.darkCoins : CachedData.zone2.darkCoins;
            }

            var zone3Mgr = FindFirstObjectByType<LasGranjasDelHastur.Zone3.Zone3Manager>();
            if (zone3Mgr != null)
            {
                CachedData.zone3Available = true;
                CachedData.zone3 ??= new Zone3SaveData();
                CachedData.zone3.valid = true;
                CachedData.zone3.darkCoins = CachedData.zone1 != null ? CachedData.zone1.darkCoins : CachedData.zone3.darkCoins;
            }

            CachedData.savedAtUtc = DateTime.UtcNow.ToString("o");
            WriteToDisk(CachedData);
        }

        static Zone1SaveData CaptureZone1Like(ResourceManager rm, ProgressionManager pm, CellManager cm, AssistantManager am, TaxManager tm)
        {
            var data = new Zone1SaveData
            {
                valid = true,
                darkCoins = rm.Get(ResourceType.DarkCoins),
                weakSouls = rm.Get(ResourceType.WeakSouls),
                pureEnergy = rm.Get(ResourceType.PureEnergy),
                memoryShards = rm.Get(ResourceType.MemoryShards),
                unstableSouls = rm.Get(ResourceType.UnstableSouls),
                level = pm.Level,
                xp = pm.Xp,
                strikes = GlobalTaxLedger.GetStrikes(),
                fineDebt = tm.FineDebt,
                timeToNextTaxSeconds = tm.TimeToNextTaxSeconds,
                taxAlertActive = tm.IsAlertActive,
                payWindowRemainingSeconds = tm.PayWindowRemainingSeconds,
                cells = cm.CaptureSaveData(),
                assistantTotal = am.TotalAssistants,
                assistants = am.CaptureSaveData(),
            };
            return data;
        }

        /// <summary>
        /// Escribe CachedData tal cual está (sin capturar estado desde escena). Útil para invalidar snapshots parciales sin depender de managers ya inicializados.
        /// </summary>
        public void WriteCachedDataNow()
        {
            if (CachedData == null)
                CachedData = new SaveGameData();

            CachedData.saveVersion = CurrentSaveVersion;
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
            PlayerPrefs.DeleteKey(Zone1GuidedTutorial.PlayerPrefsKey);
            Zone1GuidedTutorial.ClearStaticForNewGame();
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Reinicia solo el calabozo de Zona 1 en memoria y en disco (stats como run nueva), sin borrar Z2/Z3 ni menús.
        /// Útil para QA del tutorial y economía inicial; equivale a “snapshot Z1 vacío” + flags del tutorial limpios.
        /// </summary>
        public void ResetZone1DungeonProgressKeepOtherZones()
        {
            CachedData ??= new SaveGameData();
            CachedData.zone1 = new Zone1SaveData();
            CachedData.zone1Available = false;
            GlobalTaxLedger.ClearStrikes();

            PlayerPrefs.DeleteKey(Zone1GuidedTutorial.PlayerPrefsKey);
            Zone1GuidedTutorial.ClearStaticForNewGame();
            PlayerPrefs.Save();

            CachedData.savedAtUtc = DateTime.UtcNow.ToString("o");
            CachedData.saveVersion = CurrentSaveVersion;
            WriteToDisk(CachedData);
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

