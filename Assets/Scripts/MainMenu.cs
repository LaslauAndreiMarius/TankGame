using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject mapPanel;
    public GameObject garagePanel;

    void Awake()
    {
        Time.timeScale = 1f;   // in case we arrive here from a frozen game-over screen
    }

    void Start()
    {
        ShowMain();
    }

    // ---- Panel switching ----
    public void ShowMain()   { SetPanels(true, false, false); }
    public void ShowMaps()   { SetPanels(false, true, false); }
    public void ShowGarage() { SetPanels(false, false, true); }

    void SetPanels(bool main, bool maps, bool garage)
    {
        mainPanel.SetActive(main);
        mapPanel.SetActive(maps);
        garagePanel.SetActive(garage);
    }

    // ---- Actions ----
    public void LoadMap(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;   // Application.Quit() does nothing in the editor
#else
        Application.Quit();
#endif
    }
}