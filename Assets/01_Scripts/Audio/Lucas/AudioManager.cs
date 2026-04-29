using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// Asigna música y SFX en el Inspector. La música de escena se elige por nombre.
/// </summary>
public class AudioManager : MonoBehaviour
{
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

    // --- Zone1 easter egg state ---
    bool _zone1EasterActive;
    float _tZone1Easter;
    bool _easterWasPlaying;
    AudioClip _zone1EasterClip;
    Coroutine _loadEasterCoroutine;

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
            if (zone2Music != null)
                PlayMusicWithResume(zone2Music, loop: true, ref _tZone2, resumeIfPossible);
            return;
        }

        if (sceneName == "Zone3_Celestial")
        {
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
                musicSource.UnPause();
            return;
        }

        if (musicSource.clip == clip)
        {
            musicSource.loop = loop;
            if (!musicSource.isPlaying)
                musicSource.UnPause();
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
        // Easter egg: when the mp3 finishes, return to normal Zone1 music.
        if (!_zone1EasterActive || musicSource == null)
            return;
        if (_zone1EasterClip == null || musicSource.clip != _zone1EasterClip)
            return;

        if (musicSource.isPlaying)
        {
            _easterWasPlaying = true;
            return;
        }

        // If it's paused (e.g. left Zone1), keep state.
        if (SceneManager.GetActiveScene().name != "Zone1_Dungeons")
            return;

        // Finished typically resets time to 0; only end after we had started.
        if (_easterWasPlaying && musicSource.time <= 0.02f)
            EndZone1EasterEgg();
    }

    public bool IsZone1EasterEggActive => _zone1EasterActive;

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

        var full = System.IO.Path.Combine(Application.dataPath, path.Replace("Assets/", ""));
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
    }

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
}
