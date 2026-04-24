using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LasGranjasDelHastur.Core;

public class MainMenuController : MonoBehaviour
{
    public string introSceneName = "IntroComic";
    public string zoneSelectionSceneName = "ZoneSelection";
    public GameObject optionsPanel;
    public Button continueButton;

    void Start()
    {
        if (continueButton != null)
        {
            var hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSaveFile();
            continueButton.interactable = hasSave;
        }
    }

    public void PlayGame()
    {
        if (PlayerPrefs.GetInt("IntroSeen", 0) == 0)
            SceneManager.LoadScene(introSceneName);
        else
            SceneManager.LoadScene(zoneSelectionSceneName);
    }

    public void OpenOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    public void ResetIntro()
    {
        PlayerPrefs.DeleteKey("IntroSeen");
        PlayerPrefs.Save();
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ContinueGame()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasSaveFile())
        {
            // Graceful fallback if no save.
            PlayGame();
            return;
        }

        SaveManager.Instance.RequestRestoreOnNextGameplayScene();
        var targetScene = SaveManager.Instance.CachedData != null
            ? SaveManager.Instance.CachedData.lastSceneName
            : zoneSelectionSceneName;

        if (string.IsNullOrWhiteSpace(targetScene) || targetScene == "MainMenu" || targetScene == "IntroComic")
            targetScene = zoneSelectionSceneName;

        SceneManager.LoadScene(targetScene);
    }

    public void NewGame()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.StartNewGame();

        // Optional reset of one-time intro gating for full restart.
        PlayerPrefs.DeleteKey("IntroSeen");
        PlayerPrefs.Save();

        SceneManager.LoadScene(introSceneName);
    }
}
