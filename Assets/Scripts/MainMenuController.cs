using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public string introSceneName = "IntroComic";
    public string zoneSelectionSceneName = "ZoneSelection";
    public GameObject optionsPanel;

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
}
