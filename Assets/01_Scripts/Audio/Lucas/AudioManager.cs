using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using LasGranjasDelHastur.Zone1.Gacha;

/// <summary>
/// Asigna música y SFX en el Inspector. La música de escena se elige por nombre.
/// </summary>
public class AudioManager : MonoBehaviour
{
    const string Zone2EasterDefaultMusicAssetPath =
        "Assets/02_Sprites/Lucas/Zone2/EasterEgg/Yellow Claw_ Love & War (G-Funk Remix) [Extended Mix].mp3";
    const string Zone3EasterDefaultMusicAssetPath =
        "Assets/02_Sprites/Lucas/Zone3/EasterEgg/Junior H - LAS NOCHES [Official Visualizer].mp3";

    public static AudioManager Instance { get; private set; }

    public static AudioManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        var existing = FindFirstObjectByType<AudioManager>();
        if (existing != null)
            return existing;

        var go = new GameObject("AudioManager");
        return go.AddComponent<AudioManager>();
    }

    [Header("Fuentes (opcional; se crean hijos Music/SFX si faltan)")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;

    [Header("Música (asigna tus pistas al importar)")]
    public AudioClip introMusic;
    public AudioClip mainMenuMusic;
    public AudioClip zone1Music;
    public AudioClip zone2Music;
    public AudioClip zone3Music;

    [Header("Zone1 Easter Egg (ruta a mp3 dentro de Assets)")]
    [Tooltip("Ruta tipo Assets/... para cargar el mp3 en runtime sin referencia directa.")]
    [SerializeField] private string zone1EasterEggMusicPath =
        "Assets/03_Audio/Music/Lucas/Zone1/zone1_easter_carolina.mp3";

    [Header("Zone2 Easter Egg (Tindalos Pible — ruta a mp3 dentro de Assets)")]
    [Tooltip("Opcional: arrastra el .mp3 aquí para que funcione en builds; si está vacío, en Editor se usa la ruta de Assets.")]
    [SerializeField] private AudioClip zone2EasterEggMusicClip;
    [Tooltip("Ruta tipo Assets/... (fallback si no hay clip asignado; en Editor se carga como asset).")]
    [SerializeField] private string zone2EasterEggMusicPath = Zone2EasterDefaultMusicAssetPath;

    [Header("Zone3 Easter Egg (Flautista amorfo — ruta a mp3 dentro de Assets)")]
    [Tooltip("Opcional: arrastra el .mp3 aquí para que funcione en builds; si está vacío, en Editor se usa la ruta de Assets.")]
    [SerializeField] private AudioClip zone3EasterEggMusicClip;
    [Tooltip("Ruta tipo Assets/... (fallback si no hay clip asignado; en Editor se carga como asset).")]
    [SerializeField] private string zone3EasterEggMusicPath = Zone3EasterDefaultMusicAssetPath;

    [Header("Intro — hastur_sfx_pack")]
    public AudioClip introOpen;
    public AudioClip introPanelChange;
    public AudioClip introPanelChangeAlt;
    [Tooltip("Opcional: para tipo máquina de escribir en texto narrativo.")]
    public AudioClip narrationTextBlip;

    [Header("UI — hastur_sfx_pack")]
    public AudioClip uiHover;
    public AudioClip uiClick;
    [Tooltip("Zonas/UI bloqueados. Si está vacío, se usa uiClick.")]
    public AudioClip uiClickDenied;
    public AudioClip uiBack;
    public AudioClip uiOpenPanel;
    public AudioClip uiClosePanel;
    public AudioClip uiTabSwitch;
    public AudioClip uiConfirm;
    public AudioClip uiCancel;
    public AudioClip uiNotificationPop;

    [Header("Global / System — placeholders")]
    public AudioClip transitionWhooshShort;
    public AudioClip warningSoft;
    public AudioClip errorDeniedSoft;
    public AudioClip gameSaveOk;
    public AudioClip gameLoadOk;

    [Header("Zone Selection / Unlock / Minigames")]
    public AudioClip zoneCardHover;
    public AudioClip zoneCardSelect;
    public AudioClip zoneLocked;
    public AudioClip zoneUnlockSuccess;
    public AudioClip zoneUnlockFail;
    public AudioClip miniGameStart;
    public AudioClip miniGameHit;
    public AudioClip miniGameMiss;
    public AudioClip miniGameTimerWarning;
    public AudioClip miniGameComplete;
    public AudioClip miniGameFail;

    [Header("Zone1 — gameplay placeholders")]
    public AudioClip zone1CellClick;
    public AudioClip zone1Collect;
    public AudioClip zone1Buy;
    public AudioClip zone1Sell;
    public AudioClip zone1ProduceStart;
    public AudioClip zone1UpgradeStart;
    public AudioClip zone1UpgradeComplete;
    public AudioClip zone1Cleanse;
    public AudioClip zone1StorageFull;
    public AudioClip zone1StrikeGain;
    public AudioClip zone1TaxAlert;
    public AudioClip zone1TaxPay;
    public AudioClip zone1Corruption;
    public AudioClip zone1AssistantAssign;
    public AudioClip zone1AssistantUnassign;
    public AudioClip zone1BackToZones;

    [Header("Zone2 — gameplay placeholders")]
    public AudioClip zone2Action;
    public AudioClip zone2ProduceSupplies;
    public AudioClip zone2ProduceBlueprints;
    public AudioClip zone2Sell;
    public AudioClip zone2TierUp;
    public AudioClip zone2StorageFull;
    public AudioClip zone2TaxAlert;
    public AudioClip zone2TaxPay;
    public AudioClip zone2BackToZones;

    [Header("Zone3 — gameplay placeholders")]
    public AudioClip zone3Action;
    public AudioClip zone3ExtractResidue;
    public AudioClip zone3CondenseInk;
    public AudioClip zone3Sell;
    public AudioClip zone3TierUp;
    public AudioClip zone3TaxAlert;
    public AudioClip zone3TaxPay;
    public AudioClip zone3Prestige;
    public AudioClip zone3EndNarrative;
    public AudioClip zone3ObjectiveReached;
    public AudioClip zone3EndlessModeEnter;
    public AudioClip zone3CosmicWarning;
    public AudioClip zone3BackToZones;

    [Header("Zone1 — Gacha (WAV en Assets/02_Sprites/Lucas/Zone1/Gacha/zone1_gacha_sfx_pack)")]
    public AudioClip zone1GachaPanelOpen;
    public AudioClip zone1GachaPanelClose;
    public AudioClip zone1GachaButtonPull;
    public AudioClip zone1GachaButtonPressed;
    public AudioClip zone1GachaMachineReadyHumLoop;
    public AudioClip zone1GachaMachineSpinLoop;
    public AudioClip zone1GachaMachineStop;
    public AudioClip zone1GachaCapsuleDrop;
    public AudioClip zone1GachaCapsuleShake;
    public AudioClip zone1GachaCapsuleOpenCommon;
    public AudioClip zone1GachaCapsuleOpenCursed;
    public AudioClip zone1GachaCapsuleOpenJackpot;
    public AudioClip zone1GachaRewardCoinX2;
    public AudioClip zone1GachaRewardResourceX2;
    public AudioClip zone1GachaRewardCoinX5;
    public AudioClip zone1GachaPenaltyCoin100;
    public AudioClip zone1GachaPenaltyResource10;
    public AudioClip zone1GachaCurseSmoke;
    public AudioClip zone1GachaRevealGood;
    public AudioClip zone1GachaRevealBad;
    public AudioClip zone1GachaVfxSpinGlow;
    public AudioClip zone1GachaJackpotStinger;

    [Header("Random Events / Ambient")]
    public AudioClip eventYellowRiftSpawn;
    public AudioClip eventAshRainLoop;
    public AudioClip eventVoidVisitorAppear;
    public AudioClip eventHasturBlessing;
    public AudioClip eventTentaclePlague;
    public AudioClip eventDimensionalStormLoop;
    public AudioClip ambZone1DungeonLoop;
    public AudioClip ambZone2RuinedCityLoop;
    public AudioClip ambZone3CelestialLoop;
    public AudioClip ambTorchCrackleLoop;
    public AudioClip ambLowFogWindLoop;

    [Header("Intro — paneles con sonido alternativo (índice 0 = primer viñeta)")]
    [Tooltip("Por defecto: índices 3 (contrato) y 5 (última viñeta).")]
    public int[] altPanelIndices = { 3, 5 };

    [Header("Opciones")]
    [Tooltip("Reproduce introMusic en IntroComic y mainMenuMusic al cargar MainMenu (recomendado).")]
    [SerializeField] private bool autoPlayMusicByScene = true;

    // --- Music persistence per scene ---
    float _tIntro;
    float _tMenu;
    float _tZone1;
    float _tZone2;
    float _tZone3;

    AudioSource _gachaReadyLoopSource;
    AudioSource _gachaSpinLoopSource;

    // --- Zone1 easter egg state ---
    bool _zone1EasterActive;
    float _tZone1Easter;
    bool _easterWasPlaying;
    AudioClip _zone1EasterClip;
    Coroutine _loadEasterCoroutine;

    // --- Zone2 easter egg (Pible) ---
    bool _zone2EasterActive;
    float _tZone2Easter;
    bool _easter2WasPlaying;
    AudioClip _zone2EasterClip;
    Coroutine _loadZone2EasterCoroutine;
    bool _zone2EasterLoadInProgress;

    // --- Zone3 easter egg (Flautista) ---
    bool _zone3EasterActive;
    float _tZone3Easter;
    bool _easter3WasPlaying;
    AudioClip _zone3EasterClip;
    Coroutine _loadZone3EasterCoroutine;
    bool _zone3EasterLoadInProgress;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSources();
        SanitizeSharedAudioSource();
        AutofillTaxSfxFromResourcesIfMissing();
#if UNITY_EDITOR
        AutofillZone1GachaClipsFromPackIfMissing();
#endif
        ApplyVolumeSettings();

        if (autoPlayMusicByScene)
            PlayMusicForSceneName(SceneManager.GetActiveScene().name, resumeIfPossible: true);
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (!autoPlayMusicByScene)
            return;

        // Save current time for the scene we are leaving (pause, don't reset).
        SaveCurrentMusicTime(oldScene.name);

        // Resume the next scene music from where it was.
        PlayMusicForSceneName(newScene.name, resumeIfPossible: true);
    }

    private void PlayMusicForSceneName(string sceneName, bool resumeIfPossible)
    {
        if (sceneName == "IntroComic")
        {
            if (introMusic != null)
                PlayMusicWithResume(introMusic, loop: true, ref _tIntro, resumeIfPossible);
            return;
        }

        if (sceneName == "MainMenu" || sceneName == "ZoneSelection")
        {
            if (mainMenuMusic != null)
                PlayMusicWithResume(mainMenuMusic, loop: true, ref _tMenu, resumeIfPossible, continueIfSame: true);
            return;
        }

        if (sceneName == "Zone1_Dungeons")
        {
            if (_zone1EasterActive)
            {
                PlayZone1EasterMusic(resumeIfPossible);
                return;
            }

            if (zone1Music != null)
                PlayMusicWithResume(zone1Music, loop: true, ref _tZone1, resumeIfPossible);
            return;
        }

        if (sceneName == "Zone2_Cities")
        {
            if (_zone2EasterActive)
            {
                PlayZone2EasterMusic(resumeIfPossible);
                return;
            }

            if (zone2Music != null)
            {
                PlayMusicWithResume(zone2Music, loop: true, ref _tZone2, resumeIfPossible);
                return;
            }

            if (musicSource != null && mainMenuMusic != null && musicSource.clip == mainMenuMusic)
            {
                musicSource.Stop();
                Debug.LogWarning(
                    "[AudioManager] Zone2: asigna 'zone2Music' en el AudioManager. Se detuvo la pista del menú para no dejarla sonando en Ciudades.");
            }

            return;
        }

        if (sceneName == "Zone3_Celestial")
        {
            if (_zone3EasterActive)
            {
                PlayZone3EasterMusic(resumeIfPossible);
                return;
            }

            if (zone3Music != null)
                PlayMusicWithResume(zone3Music, loop: true, ref _tZone3, resumeIfPossible);
        }
    }

    /// <summary>
    /// MainMenu y ZoneSelection comparten la misma pista: no reiniciar al cambiar de escena.
    /// </summary>
    void PlayMainMenuMusicContinueIfSame()
    {
        if (musicSource == null || mainMenuMusic == null)
            return;

        if (musicSource.clip == mainMenuMusic)
        {
            musicSource.loop = true;
            if (!musicSource.isPlaying)
                musicSource.Play();
            return;
        }

        PlayMusic(mainMenuMusic, loop: true);
    }

    void PlayMusicWithResume(AudioClip clip, bool loop, ref float rememberedTime, bool resumeIfPossible, bool continueIfSame = false)
    {
        if (clip == null || musicSource == null)
            return;

        if (continueIfSame && musicSource.clip == clip)
        {
            musicSource.loop = loop;
            if (!musicSource.isPlaying)
            {
                musicSource.UnPause();
                if (!musicSource.isPlaying)
                    musicSource.Play();
            }

            return;
        }

        if (musicSource.clip == clip)
        {
            musicSource.loop = loop;
            if (!musicSource.isPlaying)
            {
                musicSource.UnPause();
                if (!musicSource.isPlaying)
                {
                    if (resumeIfPossible)
                        musicSource.time = Mathf.Clamp(rememberedTime, 0f, Mathf.Max(0f, clip.length - 0.05f));
                    musicSource.Play();
                }
            }

            return;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        if (resumeIfPossible)
            musicSource.time = Mathf.Clamp(rememberedTime, 0f, Mathf.Max(0f, clip.length - 0.05f));
        musicSource.Play();
    }

    void SaveCurrentMusicTime(string oldSceneName)
    {
        if (musicSource == null || musicSource.clip == null)
            return;

        var t = musicSource.time;

        // If we leave Zone1 while easter egg is active and playing, remember its time.
        if (_zone1EasterActive && _zone1EasterClip != null && musicSource.clip == _zone1EasterClip)
        {
            _tZone1Easter = t;
            musicSource.Pause();
            return;
        }

        if (_zone2EasterActive && _zone2EasterClip != null && musicSource.clip == _zone2EasterClip)
        {
            _tZone2Easter = t;
            musicSource.Pause();
            return;
        }

        if (_zone3EasterActive && _zone3EasterClip != null && musicSource.clip == _zone3EasterClip)
        {
            _tZone3Easter = t;
            musicSource.Pause();
            return;
        }

        switch (oldSceneName)
        {
            case "IntroComic":
                if (musicSource.clip == introMusic) _tIntro = t;
                break;
            case "MainMenu":
            case "ZoneSelection":
                if (musicSource.clip == mainMenuMusic) _tMenu = t;
                break;
            case "Zone1_Dungeons":
                if (musicSource.clip == zone1Music) _tZone1 = t;
                break;
            case "Zone2_Cities":
                if (musicSource.clip == zone2Music) _tZone2 = t;
                break;
            case "Zone3_Celestial":
                if (musicSource.clip == zone3Music) _tZone3 = t;
                break;
        }

        // Pause when leaving a zone so it resumes later.
        if (oldSceneName == "Zone1_Dungeons" || oldSceneName == "Zone2_Cities" || oldSceneName == "Zone3_Celestial")
            musicSource.Pause();
    }

    void Update()
    {
        TickZone1EasterEnd();
        TickZone2EasterEnd();
        TickZone3EasterEnd();
    }

    void TickZone1EasterEnd()
    {
        if (!_zone1EasterActive || musicSource == null)
            return;
        if (_zone1EasterClip == null)
            return;

        if (musicSource.clip != _zone1EasterClip)
        {
            _zone1EasterActive = false;
            return;
        }

        if (musicSource.isPlaying)
        {
            _easterWasPlaying = true;
            return;
        }

        if (SceneManager.GetActiveScene().name != "Zone1_Dungeons")
            return;

        if (!_easterWasPlaying)
            return;

        var len = _zone1EasterClip.length;
        var t = musicSource.time;
        if (t <= 0.02f || (len > 0.05f && t >= len - 0.08f))
            EndZone1EasterEgg();
    }

    void TickZone2EasterEnd()
    {
        if (!_zone2EasterActive || musicSource == null)
            return;
        if (_zone2EasterClip == null)
            return;

        if (musicSource.clip != _zone2EasterClip)
        {
            _zone2EasterActive = false;
            return;
        }

        if (musicSource.isPlaying)
        {
            _easter2WasPlaying = true;
            return;
        }

        if (SceneManager.GetActiveScene().name != "Zone2_Cities")
            return;

        if (!_easter2WasPlaying)
            return;

        var len = _zone2EasterClip.length;
        var t = musicSource.time;
        if (t <= 0.02f || (len > 0.05f && t >= len - 0.08f))
            EndZone2EasterEgg();
    }

    public bool IsZone1EasterEggActive => _zone1EasterActive;

    public bool IsZone2EasterEggActive => _zone2EasterActive;
    public bool IsZone3EasterEggActive => _zone3EasterActive;

    public void TriggerZone2EasterEgg()
    {
        if (SceneManager.GetActiveScene().name != "Zone2_Cities")
            return;

        if (_zone2EasterActive)
        {
            if (_zone2EasterLoadInProgress)
                return;

            var easterAudible = musicSource != null && _zone2EasterClip != null &&
                                musicSource.clip == _zone2EasterClip && musicSource.isPlaying;
            if (easterAudible)
                return;

            _zone2EasterActive = false;
        }

        _zone2EasterActive = true;
        _easter2WasPlaying = false;
        _tZone2Easter = 0f;
        PlayZone2EasterMusic(resumeIfPossible: false);
    }

    void EndZone2EasterEgg()
    {
        _zone2EasterActive = false;
        _easter2WasPlaying = false;
        _tZone2Easter = 0f;

        if (SceneManager.GetActiveScene().name == "Zone2_Cities" && zone2Music != null)
            PlayMusicWithResume(zone2Music, loop: true, ref _tZone2, resumeIfPossible: true);
    }

    void PlayZone2EasterMusic(bool resumeIfPossible)
    {
        if (musicSource == null)
            return;

        if (_zone2EasterClip != null)
        {
            musicSource.clip = _zone2EasterClip;
            musicSource.loop = false;
            if (resumeIfPossible)
                musicSource.time = Mathf.Clamp(_tZone2Easter, 0f, Mathf.Max(0f, _zone2EasterClip.length - 0.05f));
            musicSource.Play();
            return;
        }

        if (_loadZone2EasterCoroutine != null)
            return;
        _zone2EasterLoadInProgress = true;
        _loadZone2EasterCoroutine = StartCoroutine(LoadZone2EasterClipThenPlay(resumeIfPossible));
    }

    static IEnumerable<string> EnumerateZone2EasterMusicAssetPaths(string configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var t = configured.Trim();
            yield return t;
            if (string.Equals(t, Zone2EasterDefaultMusicAssetPath, StringComparison.OrdinalIgnoreCase))
                yield break;
        }
        yield return Zone2EasterDefaultMusicAssetPath;
    }

    static List<string> BuildLocalMp3UrlsForUnityWebRequest(string absolutePath)
    {
        var urls = new List<string>();
        absolutePath = Path.GetFullPath(absolutePath);
        try
        {
            var abs = new Uri(absolutePath).AbsoluteUri;
            if (!urls.Contains(abs))
                urls.Add(abs);
        }
        catch { /* ignore */ }

        var norm = absolutePath.Replace('\\', '/').Replace(" ", "%20");
        if (norm.Length >= 2 && norm[1] == ':')
        {
            if (!urls.Contains("file:///" + norm))
                urls.Add("file:///" + norm);
            if (!urls.Contains("file://" + norm))
                urls.Add("file://" + norm);
        }
        else if (!urls.Contains("file://" + norm))
            urls.Add("file://" + norm);
        return urls;
    }

    void PlayLoadedZone2EasterClip(bool resumeIfPossible)
    {
        if (!_zone2EasterActive || SceneManager.GetActiveScene().name != "Zone2_Cities" || musicSource == null)
            return;
        musicSource.clip = _zone2EasterClip;
        musicSource.loop = false;
        if (resumeIfPossible)
            musicSource.time = Mathf.Clamp(_tZone2Easter, 0f, Mathf.Max(0f, _zone2EasterClip.length - 0.05f));
        musicSource.Play();
    }

    IEnumerator LoadZone2EasterClipThenPlay(bool resumeIfPossible)
    {
        _loadZone2EasterCoroutine = null;
        try
        {
            var path = zone2EasterEggMusicPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                _zone2EasterActive = false;
                yield break;
            }

#if UNITY_EDITOR
            foreach (var assetPath in EnumerateZone2EasterMusicAssetPaths(path))
            {
                if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    continue;
                var editorClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (editorClip == null)
                    continue;
                _zone2EasterClip = editorClip;
                PlayLoadedZone2EasterClip(resumeIfPossible);
                yield break;
            }
#endif
            foreach (var assetPath in EnumerateZone2EasterMusicAssetPaths(path))
            {
                if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    continue;
                var full = ResolveRuntimeAssetFullPath(assetPath);
                if (!File.Exists(full))
                    continue;

                foreach (var url in BuildLocalMp3UrlsForUnityWebRequest(full))
                {
                    using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
                    yield return req.SendWebRequest();
                    if (req.result != UnityWebRequest.Result.Success)
                        continue;
                    _zone2EasterClip = DownloadHandlerAudioClip.GetContent(req);
                    if (_zone2EasterClip == null)
                        continue;
                    PlayLoadedZone2EasterClip(resumeIfPossible);
                    yield break;
                }
            }

            if (zone2EasterEggMusicClip != null)
            {
                _zone2EasterClip = zone2EasterEggMusicClip;
                PlayLoadedZone2EasterClip(resumeIfPossible);
                yield break;
            }

            Debug.LogWarning(
                "[AudioManager] Zone2 easter: no se pudo cargar el MP3 (ruta mal puesta, .mp3 fuera de Assets, o import no Audio). " +
                $"Revisa '{zone2EasterEggMusicPath}' o asigna zone2EasterEggMusicClip. Fallback probado: {Zone2EasterDefaultMusicAssetPath}.");
            _zone2EasterActive = false;
            if (SceneManager.GetActiveScene().name == "Zone2_Cities" && zone2Music != null && musicSource != null)
                PlayMusicWithResume(zone2Music, loop: true, ref _tZone2, resumeIfPossible: true);
        }
        finally
        {
            _zone2EasterLoadInProgress = false;
        }
    }

    void TickZone3EasterEnd()
    {
        if (!_zone3EasterActive || musicSource == null)
            return;
        if (_zone3EasterClip == null)
            return;

        if (musicSource.clip != _zone3EasterClip)
        {
            _zone3EasterActive = false;
            return;
        }

        if (musicSource.isPlaying)
        {
            _easter3WasPlaying = true;
            return;
        }

        if (SceneManager.GetActiveScene().name != "Zone3_Celestial")
            return;

        if (!_easter3WasPlaying)
            return;

        var len = _zone3EasterClip.length;
        var t = musicSource.time;
        if (t <= 0.02f || (len > 0.05f && t >= len - 0.08f))
            EndZone3EasterEgg();
    }

    public void TriggerZone3EasterEgg()
    {
        if (SceneManager.GetActiveScene().name != "Zone3_Celestial")
            return;

        if (_zone3EasterActive)
        {
            if (_zone3EasterLoadInProgress)
                return;

            var easterAudible = musicSource != null && _zone3EasterClip != null &&
                                musicSource.clip == _zone3EasterClip && musicSource.isPlaying;
            if (easterAudible)
                return;

            _zone3EasterActive = false;
        }

        _zone3EasterActive = true;
        _easter3WasPlaying = false;
        _tZone3Easter = 0f;
        PlayZone3EasterMusic(resumeIfPossible: false);
    }

    void EndZone3EasterEgg()
    {
        _zone3EasterActive = false;
        _easter3WasPlaying = false;
        _tZone3Easter = 0f;

        if (SceneManager.GetActiveScene().name == "Zone3_Celestial" && zone3Music != null)
            PlayMusicWithResume(zone3Music, loop: true, ref _tZone3, resumeIfPossible: true);
    }

    void PlayZone3EasterMusic(bool resumeIfPossible)
    {
        if (musicSource == null)
            return;

        if (_zone3EasterClip != null)
        {
            musicSource.clip = _zone3EasterClip;
            musicSource.loop = false;
            if (resumeIfPossible)
                musicSource.time = Mathf.Clamp(_tZone3Easter, 0f, Mathf.Max(0f, _zone3EasterClip.length - 0.05f));
            musicSource.Play();
            return;
        }

        if (_loadZone3EasterCoroutine != null)
            return;
        _zone3EasterLoadInProgress = true;
        _loadZone3EasterCoroutine = StartCoroutine(LoadZone3EasterClipThenPlay(resumeIfPossible));
    }

    static IEnumerable<string> EnumerateZone3EasterMusicAssetPaths(string configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var t = configured.Trim();
            yield return t;
            if (string.Equals(t, Zone3EasterDefaultMusicAssetPath, StringComparison.OrdinalIgnoreCase))
                yield break;
        }
        yield return Zone3EasterDefaultMusicAssetPath;
    }

    void PlayLoadedZone3EasterClip(bool resumeIfPossible)
    {
        if (!_zone3EasterActive || SceneManager.GetActiveScene().name != "Zone3_Celestial" || musicSource == null)
            return;
        musicSource.clip = _zone3EasterClip;
        musicSource.loop = false;
        if (resumeIfPossible)
            musicSource.time = Mathf.Clamp(_tZone3Easter, 0f, Mathf.Max(0f, _zone3EasterClip.length - 0.05f));
        musicSource.Play();
    }

    IEnumerator LoadZone3EasterClipThenPlay(bool resumeIfPossible)
    {
        _loadZone3EasterCoroutine = null;
        try
        {
            var path = zone3EasterEggMusicPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                _zone3EasterActive = false;
                yield break;
            }

            if (zone3EasterEggMusicClip != null)
            {
                _zone3EasterClip = zone3EasterEggMusicClip;
                PlayLoadedZone3EasterClip(resumeIfPossible);
                yield break;
            }

#if UNITY_EDITOR
            foreach (var assetPath in EnumerateZone3EasterMusicAssetPaths(path))
            {
                if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    continue;
                var editorClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (editorClip == null)
                    continue;
                _zone3EasterClip = editorClip;
                PlayLoadedZone3EasterClip(resumeIfPossible);
                yield break;
            }
#endif
            foreach (var assetPath in EnumerateZone3EasterMusicAssetPaths(path))
            {
                if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    continue;
                var full = ResolveRuntimeAssetFullPath(assetPath);
                if (!File.Exists(full))
                    continue;

                foreach (var url in BuildLocalMp3UrlsForUnityWebRequest(full))
                {
                    using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
                    yield return req.SendWebRequest();
                    if (req.result != UnityWebRequest.Result.Success)
                        continue;
                    _zone3EasterClip = DownloadHandlerAudioClip.GetContent(req);
                    if (_zone3EasterClip == null)
                        continue;
                    PlayLoadedZone3EasterClip(resumeIfPossible);
                    yield break;
                }
            }

            Debug.LogWarning(
                "[AudioManager] Zone3 easter: no se pudo cargar el MP3 (ruta mal puesta, .mp3 fuera de Assets, o import no Audio). " +
                $"Revisa '{zone3EasterEggMusicPath}' o asigna zone3EasterEggMusicClip. Fallback probado: {Zone3EasterDefaultMusicAssetPath}.");
            _zone3EasterActive = false;
            if (SceneManager.GetActiveScene().name == "Zone3_Celestial" && zone3Music != null && musicSource != null)
                PlayMusicWithResume(zone3Music, loop: true, ref _tZone3, resumeIfPossible: true);
        }
        finally
        {
            _zone3EasterLoadInProgress = false;
        }
    }

    public void TriggerZone1EasterEgg()
    {
        if (_zone1EasterActive)
            return;
        _zone1EasterActive = true;
        _easterWasPlaying = false;
        _tZone1Easter = 0f;
        if (SceneManager.GetActiveScene().name == "Zone1_Dungeons")
            PlayZone1EasterMusic(resumeIfPossible: true);
    }

    void EndZone1EasterEgg()
    {
        _zone1EasterActive = false;
        _easterWasPlaying = false;
        _tZone1Easter = 0f;

        // Restore Zone1 base track if we're in Zone1, otherwise keep paused state.
        if (SceneManager.GetActiveScene().name == "Zone1_Dungeons" && zone1Music != null)
            PlayMusicWithResume(zone1Music, loop: true, ref _tZone1, resumeIfPossible: true);
    }

    void PlayZone1EasterMusic(bool resumeIfPossible)
    {
        if (musicSource == null)
            return;

        if (_zone1EasterClip != null)
        {
            musicSource.clip = _zone1EasterClip;
            musicSource.loop = false;
            if (resumeIfPossible)
                musicSource.time = Mathf.Clamp(_tZone1Easter, 0f, Mathf.Max(0f, _zone1EasterClip.length - 0.05f));
            musicSource.Play();
            return;
        }

        if (_loadEasterCoroutine != null)
            return;
        _loadEasterCoroutine = StartCoroutine(LoadEasterClipThenPlay(resumeIfPossible));
    }

    IEnumerator LoadEasterClipThenPlay(bool resumeIfPossible)
    {
        _loadEasterCoroutine = null;
        var path = zone1EasterEggMusicPath;
        if (string.IsNullOrWhiteSpace(path))
            yield break;

        var full = ResolveRuntimeAssetFullPath(path);
        if (!File.Exists(full))
            yield break;
        var url = "file://" + full.Replace("\\", "/");
        using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
            yield break;

        _zone1EasterClip = DownloadHandlerAudioClip.GetContent(req);
        if (_zone1EasterClip == null)
            yield break;

        if (_zone1EasterActive && SceneManager.GetActiveScene().name == "Zone1_Dungeons")
        {
            musicSource.clip = _zone1EasterClip;
            musicSource.loop = false;
            if (resumeIfPossible)
                musicSource.time = Mathf.Clamp(_tZone1Easter, 0f, Mathf.Max(0f, _zone1EasterClip.length - 0.05f));
            musicSource.Play();
        }
    }

    static string ResolveRuntimeAssetFullPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return string.Empty;

        var normalized = assetPath.Replace('\\', '/');
        var relative = normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
            ? normalized.Substring("Assets/".Length)
            : normalized;

        if (Application.isEditor)
            return Path.GetFullPath(Path.Combine(Application.dataPath, relative));

        var fromStreaming = Path.Combine(
            Application.streamingAssetsPath,
            "RuntimeArtCache",
            relative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fromStreaming))
            return fromStreaming;

        return Path.GetFullPath(Path.Combine(Application.dataPath, relative));
    }

    private void EnsureSources()
    {
        if (musicSource == null)
        {
            var go = new GameObject("Music");
            go.transform.SetParent(transform, false);
            musicSource = go.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
        }

        if (sfxSource == null)
        {
            var go = new GameObject("SFX");
            go.transform.SetParent(transform, false);
            sfxSource = go.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
        }

        if (uiSource == null)
        {
            var go = new GameObject("UI");
            go.transform.SetParent(transform, false);
            uiSource = go.AddComponent<AudioSource>();
            uiSource.loop = false;
            uiSource.playOnAwake = false;
            uiSource.spatialBlend = 0f;
        }

        EnsureGachaLoopSources();
    }

    void AutofillTaxSfxFromResourcesIfMissing()
    {
        // Fuente de verdad de Edwin: Assets/03_Audio/SFX/Edwin/Tax (no mezclar con Resources salvo copia espejo opcional para builds sin referencias en Inspector).
        const string taxDir = "Assets/03_Audio/SFX/Edwin/Tax/";
        TryAutofillTaxClip(ref zone1TaxAlert, taxDir + "zone1_tax_alert-cthulhu-calabozo.wav", "SFX/Edwin/Tax/zone1_tax_alert-cthulhu-calabozo");
        TryAutofillTaxClip(ref zone1TaxPay, taxDir + "zone1_tax_pay-pago.wav", "SFX/Edwin/Tax/zone1_tax_pay-pago");
        TryAutofillTaxClip(ref zone2TaxAlert, taxDir + "zone2_tax_alert-kthanid-ciudad.wav", "SFX/Edwin/Tax/zone2_tax_alert-kthanid-ciudad");
        TryAutofillTaxClip(ref zone2TaxPay, taxDir + "zone2_tax_pay-pago-urbano.wav", "SFX/Edwin/Tax/zone2_tax_pay-pago-urbano");
        TryAutofillTaxClip(ref zone3TaxAlert, taxDir + "zone3_tax_alert-azathoth-cosmos.wav", "SFX/Edwin/Tax/zone3_tax_alert-azathoth-cosmos");
        TryAutofillTaxClip(ref zone3TaxPay, taxDir + "zone3_tax_pay-tributo-aceptado.wav", "SFX/Edwin/Tax/zone3_tax_pay-tributo-aceptado");
    }

    void TryAutofillTaxClip(ref AudioClip clip, string projectAssetPath, string resourcesSubPathNoExtension)
    {
        if (clip != null)
            return;
#if UNITY_EDITOR
        clip = AssetDatabase.LoadAssetAtPath<AudioClip>(projectAssetPath);
#endif
        if (clip == null && !string.IsNullOrEmpty(resourcesSubPathNoExtension))
            clip = Resources.Load<AudioClip>(resourcesSubPathNoExtension);
    }

    void EnsureGachaLoopSources()
    {
        if (_gachaReadyLoopSource != null && _gachaSpinLoopSource != null)
            return;

        var holder = transform.Find("GachaLoops");
        if (holder == null)
        {
            var h = new GameObject("GachaLoops");
            h.transform.SetParent(transform, false);
            holder = h.transform;
        }

        if (_gachaReadyLoopSource == null)
        {
            var go = new GameObject("GachaReadyHum");
            go.transform.SetParent(holder, false);
            _gachaReadyLoopSource = go.AddComponent<AudioSource>();
            _gachaReadyLoopSource.loop = true;
            _gachaReadyLoopSource.playOnAwake = false;
            _gachaReadyLoopSource.spatialBlend = 0f;
        }

        if (_gachaSpinLoopSource == null)
        {
            var go = new GameObject("GachaSpin");
            go.transform.SetParent(holder, false);
            _gachaSpinLoopSource = go.AddComponent<AudioSource>();
            _gachaSpinLoopSource.loop = true;
            _gachaSpinLoopSource.playOnAwake = false;
            _gachaSpinLoopSource.spatialBlend = 0f;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Si los campos del gacha están vacíos, carga los WAV del pack desde <see cref="Zone1GachaArtPaths"/>.
    /// Solo en Editor (AssetDatabase); en build hay que asignar clips en el prefab o usar otro pipeline.
    /// </summary>
    void AutofillZone1GachaClipsFromPackIfMissing()
    {
        if (zone1GachaPanelOpen == null)
            zone1GachaPanelOpen = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxPanelOpen);
        if (zone1GachaPanelClose == null)
            zone1GachaPanelClose = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxPanelClose);
        if (zone1GachaButtonPull == null)
            zone1GachaButtonPull = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxButtonPull);
        if (zone1GachaButtonPressed == null)
            zone1GachaButtonPressed = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxButtonPressed);
        if (zone1GachaMachineReadyHumLoop == null)
            zone1GachaMachineReadyHumLoop = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxReadyHumLoop);
        if (zone1GachaMachineSpinLoop == null)
            zone1GachaMachineSpinLoop = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxSpinLoop);
        if (zone1GachaMachineStop == null)
            zone1GachaMachineStop = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxMachineStop);
        if (zone1GachaCapsuleDrop == null)
            zone1GachaCapsuleDrop = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxCapsuleDrop);
        if (zone1GachaCapsuleShake == null)
            zone1GachaCapsuleShake = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxCapsuleShake);
        if (zone1GachaCapsuleOpenCommon == null)
            zone1GachaCapsuleOpenCommon = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxCapsuleOpenCommon);
        if (zone1GachaCapsuleOpenCursed == null)
            zone1GachaCapsuleOpenCursed = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxCapsuleOpenCursed);
        if (zone1GachaCapsuleOpenJackpot == null)
            zone1GachaCapsuleOpenJackpot = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxCapsuleOpenJackpot);
        if (zone1GachaRewardCoinX2 == null)
            zone1GachaRewardCoinX2 = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxRewardCoinX2);
        if (zone1GachaRewardResourceX2 == null)
            zone1GachaRewardResourceX2 = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxRewardResourceX2);
        if (zone1GachaRewardCoinX5 == null)
            zone1GachaRewardCoinX5 = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxRewardCoinX5);
        if (zone1GachaPenaltyCoin100 == null)
            zone1GachaPenaltyCoin100 = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxPenaltyCoin100);
        if (zone1GachaPenaltyResource10 == null)
            zone1GachaPenaltyResource10 = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxPenaltyResource10);
        if (zone1GachaCurseSmoke == null)
            zone1GachaCurseSmoke = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxCurseSmoke);
        if (zone1GachaRevealGood == null)
            zone1GachaRevealGood = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxRevealGood);
        if (zone1GachaRevealBad == null)
            zone1GachaRevealBad = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxRevealBad);
        if (zone1GachaVfxSpinGlow == null)
            zone1GachaVfxSpinGlow = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxVfxSpinGlow);
        if (zone1GachaJackpotStinger == null)
            zone1GachaJackpotStinger = AssetDatabase.LoadAssetAtPath<AudioClip>(Zone1GachaArtPaths.SfxJackpotStinger);
    }
#endif

    /// <summary>Si música y SFX apuntan al mismo AudioSource, crea un SFX aparte para no cortar la música.</summary>
    private void SanitizeSharedAudioSource()
    {
        if (musicSource == null || sfxSource == null)
            return;
        if (!ReferenceEquals(musicSource, sfxSource))
            return;

        sfxSource = null;
        var go = new GameObject("SFX");
        go.transform.SetParent(transform, false);
        sfxSource = go.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    public void ApplyVolumeSettings()
    {
        float m = Mathf.Clamp01(PlayerPrefs.GetFloat("MusicVolume", 1f));
        float s = Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 1f));
        if (musicSource != null)
            musicSource.volume = m;
        if (sfxSource != null)
            sfxSource.volume = s;
        if (uiSource != null)
            uiSource.volume = s;
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null)
            return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null)
            return;
        float s = Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 1f));
        sfxSource.PlayOneShot(clip, s * volumeScale);
    }

    public void PlayUI(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;
        float s = Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 1f));
        if (uiSource != null)
            uiSource.PlayOneShot(clip, s * volumeScale);
        else
            PlaySFX(clip, volumeScale);
    }

    public void PlayIntroOpen()
    {
        PlaySFX(introOpen);
    }

    public void PlayIntroPanelChange(int panelIndex)
    {
        if (panelIndex <= 0)
            return;

        bool alt = false;
        if (altPanelIndices != null && altPanelIndices.Length > 0)
        {
            foreach (int i in altPanelIndices)
            {
                if (i == panelIndex)
                {
                    alt = true;
                    break;
                }
            }
        }

        PlaySFX(alt ? introPanelChangeAlt : introPanelChange);
    }

    public void PlayNarrationBlip()
    {
        PlaySFX(narrationTextBlip);
    }

    public void PlayUIHover() => PlayUI(uiHover);
    public void PlayUIClick() => PlayUI(uiClick);
    public void PlayUIClickDenied() => PlayUI(uiClickDenied != null ? uiClickDenied : uiClick);
    public void PlayUIBack() => PlayUI(uiBack);
    public void PlayUIOpenPanel() => PlayUI(uiOpenPanel != null ? uiOpenPanel : uiClick);
    public void PlayUIClosePanel() => PlayUI(uiClosePanel != null ? uiClosePanel : uiBack);
    public void PlayUITabSwitch() => PlayUI(uiTabSwitch != null ? uiTabSwitch : uiClick);
    public void PlayUIConfirm() => PlayUI(uiConfirm != null ? uiConfirm : uiClick);
    public void PlayUICancel() => PlayUI(uiCancel != null ? uiCancel : uiBack);
    public void PlayUINotification() => PlayUI(uiNotificationPop != null ? uiNotificationPop : uiHover);

    public void PlayTransitionWhoosh() => PlayUI(transitionWhooshShort);
    public void PlayWarningSoft() => PlaySFX(warningSoft);
    public void PlayErrorDeniedSoft() => PlayUI(errorDeniedSoft != null ? errorDeniedSoft : uiClickDenied != null ? uiClickDenied : uiClick);
    public void PlaySaveOk() => PlayUI(gameSaveOk != null ? gameSaveOk : uiConfirm != null ? uiConfirm : uiClick);
    public void PlayLoadOk() => PlayUI(gameLoadOk != null ? gameLoadOk : uiConfirm != null ? uiConfirm : uiClick);

    public void PlayZoneCardHover() => PlayUI(zoneCardHover != null ? zoneCardHover : uiHover);
    public void PlayZoneCardSelect() => PlayUI(zoneCardSelect != null ? zoneCardSelect : uiClick);
    public void PlayZoneLocked() => PlayUI(zoneLocked != null ? zoneLocked : uiClickDenied != null ? uiClickDenied : uiClick);
    public void PlayZoneUnlockSuccess() => PlaySFX(zoneUnlockSuccess);
    public void PlayZoneUnlockFail() => PlaySFX(zoneUnlockFail);
    public void PlayMiniGameStart() => PlaySFX(miniGameStart);
    public void PlayMiniGameHit() => PlaySFX(miniGameHit);
    public void PlayMiniGameMiss() => PlaySFX(miniGameMiss);
    public void PlayMiniGameTimerWarning() => PlaySFX(miniGameTimerWarning);
    public void PlayMiniGameComplete() => PlaySFX(miniGameComplete);
    public void PlayMiniGameFail() => PlaySFX(miniGameFail);

    public void PlayZone1ProduceStart() => PlaySFX(zone1ProduceStart != null ? zone1ProduceStart : zone1CellClick);
    public void PlayZone1UpgradeStart() => PlaySFX(zone1UpgradeStart != null ? zone1UpgradeStart : zone1Buy);
    public void PlayZone1UpgradeComplete() => PlaySFX(zone1UpgradeComplete != null ? zone1UpgradeComplete : zone1Buy);
    public void PlayZone1Cleanse() => PlaySFX(zone1Cleanse);
    public void PlayZone1StorageFull() => PlaySFX(zone1StorageFull != null ? zone1StorageFull : warningSoft);
    public void PlayZone1StrikeGain() => PlaySFX(zone1StrikeGain != null ? zone1StrikeGain : zone1Corruption);
    public void PlayZone1AssistantAssign() => PlaySFX(zone1AssistantAssign);
    public void PlayZone1AssistantUnassign() => PlaySFX(zone1AssistantUnassign);
    public void PlayZone1BackToZones() => PlayUI(zone1BackToZones != null ? zone1BackToZones : uiBack);
    public void PlayZone1TaxAlert() => PlaySFX(zone1TaxAlert);
    public void PlayZone1TaxPay() => PlaySFX(zone1TaxPay);

    public void PlayZone2Action() => PlaySFX(zone2Action);
    public void PlayZone2ProduceSupplies() => PlaySFX(zone2ProduceSupplies != null ? zone2ProduceSupplies : zone2Action);
    public void PlayZone2ProduceBlueprints() => PlaySFX(zone2ProduceBlueprints != null ? zone2ProduceBlueprints : zone2Action);
    public void PlayZone2Sell() => PlaySFX(zone2Sell);
    public void PlayZone2TierUp() => PlaySFX(zone2TierUp);
    public void PlayZone2StorageFull() => PlaySFX(zone2StorageFull != null ? zone2StorageFull : warningSoft);
    public void PlayZone2TaxAlert() => PlaySFX(zone2TaxAlert);
    public void PlayZone2TaxPay() => PlaySFX(zone2TaxPay);
    public void PlayZone2BackToZones() => PlayUI(zone2BackToZones != null ? zone2BackToZones : uiBack);

    public void PlayZone3Action() => PlaySFX(zone3Action);
    public void PlayZone3ExtractResidue() => PlaySFX(zone3ExtractResidue != null ? zone3ExtractResidue : zone3Action);
    public void PlayZone3CondenseInk() => PlaySFX(zone3CondenseInk != null ? zone3CondenseInk : zone3Action);
    public void PlayZone3Sell() => PlaySFX(zone3Sell);
    public void PlayZone3TierUp() => PlaySFX(zone3TierUp);
    public void PlayZone3TaxAlert() => PlaySFX(zone3TaxAlert);
    public void PlayZone3TaxPay() => PlaySFX(zone3TaxPay);
    public void PlayZone3Prestige() => PlaySFX(zone3Prestige);
    public void PlayZone3EndNarrative() => PlaySFX(zone3EndNarrative);
    public void PlayZone3ObjectiveReached() => PlaySFX(zone3ObjectiveReached != null ? zone3ObjectiveReached : zone3EndNarrative);
    public void PlayZone3EndlessModeEnter() => PlaySFX(zone3EndlessModeEnter);
    public void PlayZone3CosmicWarning() => PlaySFX(zone3CosmicWarning);
    public void PlayZone3BackToZones() => PlayUI(zone3BackToZones != null ? zone3BackToZones : uiBack);

    public void PlayEventYellowRiftSpawn() => PlaySFX(eventYellowRiftSpawn);
    public void PlayEventAshRainLoop() => PlaySFX(eventAshRainLoop);
    public void PlayEventVoidVisitorAppear() => PlaySFX(eventVoidVisitorAppear);
    public void PlayEventHasturBlessing() => PlaySFX(eventHasturBlessing);
    public void PlayEventTentaclePlague() => PlaySFX(eventTentaclePlague);
    public void PlayEventDimensionalStormLoop() => PlaySFX(eventDimensionalStormLoop);

    public void PlayZone1GachaPanelOpen() => PlayUI(zone1GachaPanelOpen != null ? zone1GachaPanelOpen : uiOpenPanel);
    public void PlayZone1GachaPanelClose() => PlayUI(zone1GachaPanelClose != null ? zone1GachaPanelClose : uiClosePanel);
    public void PlayZone1GachaButtonPull() => PlayUI(zone1GachaButtonPull != null ? zone1GachaButtonPull : uiClick);
    public void PlayZone1GachaButtonPressed() => PlayUI(zone1GachaButtonPressed != null ? zone1GachaButtonPressed : uiClick);
    public void PlayZone1GachaCapsuleDrop() => PlaySFX(zone1GachaCapsuleDrop);
    public void PlayZone1GachaCapsuleShake() => PlaySFX(zone1GachaCapsuleShake);
    public void PlayZone1GachaMachineStop() => PlaySFX(zone1GachaMachineStop);
    public void PlayZone1GachaCapsuleOpenCommon() => PlaySFX(zone1GachaCapsuleOpenCommon);
    public void PlayZone1GachaCapsuleOpenCursed() => PlaySFX(zone1GachaCapsuleOpenCursed);
    public void PlayZone1GachaCapsuleOpenJackpot() => PlaySFX(zone1GachaCapsuleOpenJackpot);
    public void PlayZone1GachaRewardCoinX2() => PlaySFX(zone1GachaRewardCoinX2);
    public void PlayZone1GachaRewardResourceX2() => PlaySFX(zone1GachaRewardResourceX2);
    public void PlayZone1GachaRewardCoinX5() => PlaySFX(zone1GachaRewardCoinX5);
    public void PlayZone1GachaPenaltyCoin100() => PlaySFX(zone1GachaPenaltyCoin100);
    public void PlayZone1GachaPenaltyResource10() => PlaySFX(zone1GachaPenaltyResource10);
    public void PlayZone1GachaCurseSmoke() => PlaySFX(zone1GachaCurseSmoke);
    public void PlayZone1GachaRevealGood() => PlaySFX(zone1GachaRevealGood);
    public void PlayZone1GachaRevealBad() => PlaySFX(zone1GachaRevealBad);
    public void PlayZone1GachaVfxSpinGlow(float volumeScale = 1f) => PlaySFX(zone1GachaVfxSpinGlow, volumeScale);
    public void PlayZone1GachaJackpotStinger() => PlaySFX(zone1GachaJackpotStinger);

    public void StartZone1GachaReadyHumLoop()
    {
        EnsureGachaLoopSources();
        if (_gachaReadyLoopSource == null || zone1GachaMachineReadyHumLoop == null)
            return;
        float s = Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 1f));
        _gachaReadyLoopSource.clip = zone1GachaMachineReadyHumLoop;
        _gachaReadyLoopSource.volume = s * 0.55f;
        _gachaReadyLoopSource.loop = true;
        if (!_gachaReadyLoopSource.isPlaying)
            _gachaReadyLoopSource.Play();
    }

    public void StopZone1GachaHumLoop()
    {
        if (_gachaReadyLoopSource != null && _gachaReadyLoopSource.isPlaying)
            _gachaReadyLoopSource.Stop();
    }

    public void StartZone1GachaSpinLoop()
    {
        EnsureGachaLoopSources();
        if (_gachaSpinLoopSource == null || zone1GachaMachineSpinLoop == null)
            return;
        float s = Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 1f));
        _gachaSpinLoopSource.clip = zone1GachaMachineSpinLoop;
        _gachaSpinLoopSource.volume = s * 0.65f;
        _gachaSpinLoopSource.loop = true;
        if (!_gachaSpinLoopSource.isPlaying)
            _gachaSpinLoopSource.Play();
    }

    public void StopZone1GachaSpinLoop()
    {
        if (_gachaSpinLoopSource != null && _gachaSpinLoopSource.isPlaying)
            _gachaSpinLoopSource.Stop();
    }

    public void StopZone1GachaLoops()
    {
        StopZone1GachaHumLoop();
        StopZone1GachaSpinLoop();
    }
}
