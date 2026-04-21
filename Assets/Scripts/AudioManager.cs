using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Asigna música y SFX en el Inspector. La música de escena se elige por nombre: IntroComic → introMusic, MainMenu → mainMenuMusic.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Fuentes (opcional; se crean hijos Music/SFX si faltan)")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Música (asigna tus pistas al importar)")]
    public AudioClip introMusic;
    public AudioClip mainMenuMusic;

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

    [Header("Intro — paneles con sonido alternativo (índice 0 = primer viñeta)")]
    [Tooltip("Por defecto: índices 3 (contrato) y 5 (última viñeta).")]
    public int[] altPanelIndices = { 3, 5 };

    [Header("Opciones")]
    [Tooltip("Reproduce introMusic en IntroComic y mainMenuMusic al cargar MainMenu (recomendado).")]
    [SerializeField] private bool autoPlayMusicByScene = true;

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
            PlayMusicForSceneName(SceneManager.GetActiveScene().name);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!autoPlayMusicByScene)
            return;
        PlayMusicForSceneName(scene.name);
    }

    private void PlayMusicForSceneName(string sceneName)
    {
        if (sceneName == "IntroComic")
        {
            if (introMusic != null)
                PlayMusic(introMusic, loop: true);
            return;
        }

        if (sceneName == "MainMenu" || sceneName == "ZoneSelection")
        {
            if (mainMenuMusic != null)
                PlayMainMenuMusicContinueIfSame();
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

    public void PlayUIHover() => PlaySFX(uiHover);
    public void PlayUIClick() => PlaySFX(uiClick);
    public void PlayUIClickDenied() => PlaySFX(uiClickDenied != null ? uiClickDenied : uiClick);
    public void PlayUIBack() => PlaySFX(uiBack);
}
