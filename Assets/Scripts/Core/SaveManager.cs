using System;
using System.IO;
using LasGranjasDelHastur.Zone1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LasGranjasDelHastur.Core
{
    [DisallowMultipleComponent]
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        public const int CurrentSaveVersion = 1;

        [Header("Auto Save")]
        [SerializeField] private bool autoSave = true;
        [SerializeField] private float autoSaveEverySeconds = 20f;

        public bool ShouldRestoreFromSave { get; private set; }
        public SaveGameData CachedData { get; private set; } = new();

        float _autoSaveTimer;
        string SaveFilePath => Path.Combine(Application.persistentDataPath, "savegame.json");
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

            CachedData.savedAtUtc = DateTime.UtcNow.ToString("o");
            WriteToDisk(CachedData);
        }

        public void DeleteSaveFile()
        {
            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);
            CachedData = new SaveGameData();
            ShouldRestoreFromSave = false;
        }

        public void StartNewGame()
        {
            DeleteSaveFile();
            CachedData = new SaveGameData
            {
                saveVersion = CurrentSaveVersion,
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                lastSceneName = "MainMenu",
            };
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
                    // Example structure for upcoming versions:
                    // case 1: data = MigrateV1ToV2(data); data.saveVersion = 2; break;
                    default:
                        data.saveVersion = CurrentSaveVersion;
                        break;
                }
            }

            // Ensure safe defaults after migration.
            data.lastSceneName = string.IsNullOrWhiteSpace(data.lastSceneName) ? "MainMenu" : data.lastSceneName;
            data.zone1 ??= new Zone1SaveData();
            return data;
        }

        void WriteToDisk(SaveGameData data)
        {
            try
            {
                var json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: failed to write save file. {e.Message}");
            }
        }
    }
}

