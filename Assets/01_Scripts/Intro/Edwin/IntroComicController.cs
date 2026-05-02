using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Cómic de introducción (convención: scripts de esta feature en carpeta Edwin).
/// </summary>
public class IntroComicController : MonoBehaviour
{
    const string IntroSeenPrefKey = "IntroSeen";

    [Header("UI")]
    public Image comicImage;
    public TextMeshProUGUI narrationText;
    public Button nextButton;
    public Button skipButton;

    [Header("Contenido")]
    // Panel order (Cinematic pack): 1-YellowSky, 2-HasturThrone,
    // 3-EntitiesDemandingResources, 4-AbandonedFarmDungeon, 5-Contract, 6-FirstZoneUnlocked
    public Sprite[] panels;
    public string[] narrations;

    [Header("Texto — máquina de escribir")]
    [Tooltip("Segundos de espera entre cada carácter visible.")]
    [SerializeField] private float delayPerCharacter = 0.035f;
    [Tooltip("Si está activo y hay clip en AudioManager, suena cada N caracteres (0 = nunca).")]
    [SerializeField] private int narrationBlipEveryNChars;

    [Header("Escena destino")]
    public string mainMenuSceneName = "MainMenu";
    public bool saveIntroAsSeen = true;

    private int currentIndex;
    private string _fullLine;
    private bool _lineFullyShown = true;
    private Coroutine _typewriterCoroutine;

    private void Start()
    {
        if (PlayerPrefs.GetInt(IntroSeenPrefKey, 0) != 0)
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayIntroOpen();

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextPressed);

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipIntro);

        ShowPanel();
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextPressed);
        if (skipButton != null)
            skipButton.onClick.RemoveListener(SkipIntro);
    }

    /// <summary>
    /// Primer clic si aún se escribe: mostrar todo el texto. Si ya está completo: pasar de panel.
    /// </summary>
    public void OnNextPressed()
    {
        if (!_lineFullyShown && !string.IsNullOrEmpty(_fullLine))
        {
            CompleteTypewriterNow();
            return;
        }

        currentIndex++;
        if (currentIndex >= panels.Length)
        {
            FinishIntro();
            return;
        }

        ShowPanel();
    }

    private void CompleteTypewriterNow()
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }

        if (narrationText != null)
            narrationText.text = _fullLine;

        _lineFullyShown = true;
    }

    public void FinishIntro()
    {
        GoToMainMenu();
    }

    public void SkipIntro()
    {
        GoToMainMenu();
    }

    private void GoToMainMenu()
    {
        if (saveIntroAsSeen)
        {
            PlayerPrefs.SetInt(IntroSeenPrefKey, 1);
            PlayerPrefs.Save();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ShowPanel()
    {
        if (panels == null || panels.Length == 0)
        {
            Debug.LogWarning("No hay paneles asignados para la introducción.");
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, panels.Length - 1);

        if (comicImage != null)
            comicImage.sprite = panels[currentIndex];

        _fullLine = narrations != null && currentIndex < narrations.Length
            ? narrations[currentIndex]
            : "";

        if (narrationText != null)
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            narrationText.text = "";
            _lineFullyShown = string.IsNullOrEmpty(_fullLine);

            if (!_lineFullyShown)
                _typewriterCoroutine = StartCoroutine(TypewriterRoutine());
        }

        if (currentIndex > 0 && AudioManager.Instance != null)
            AudioManager.Instance.PlayIntroPanelChange(currentIndex);
    }

    private IEnumerator TypewriterRoutine()
    {
        _lineFullyShown = false;
        var line = _fullLine ?? "";
        narrationText.text = "";

        var wait = new WaitForSeconds(delayPerCharacter);
        for (int i = 0; i < line.Length; i++)
        {
            narrationText.text += line[i];

            if (narrationBlipEveryNChars > 0 &&
                AudioManager.Instance != null &&
                (i + 1) % narrationBlipEveryNChars == 0)
                AudioManager.Instance.PlayNarrationBlip();

            yield return wait;
        }

        _lineFullyShown = true;
        _typewriterCoroutine = null;
    }
}
