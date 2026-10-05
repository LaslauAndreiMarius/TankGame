using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;   // lets other scripts call GameManager.Instance

    public TextMeshProUGUI killsText;     // the "Kills: 0" counter
    public TextMeshProUGUI deathText;     // the "YOU DIED" message (hidden until you die)
    public float restartDelay = 3f;       // seconds before the map reloads

    private int kills = 0;
    private bool playerDead = false;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;   // safety: never start a map frozen
    }

    void Start()
    {
        if (deathText != null) deathText.gameObject.SetActive(false);
        UpdateUI();
    }

    public void AddKill()
    {
        if (playerDead) return;   // kills after you die don't count
        kills++;
        UpdateUI();
    }

    public void PlayerDied()
    {
        if (playerDead) return;
        playerDead = true;
        StartCoroutine(RestartAfterDelay());
    }

    IEnumerator RestartAfterDelay()
    {
        if (deathText != null)
        {
            deathText.text = "YOU DIED\nKills: " + kills;
            deathText.gameObject.SetActive(true);
        }

        // Realtime wait, so it still works if time is ever frozen
        yield return new WaitForSecondsRealtime(restartDelay);

        // Reloading the scene recreates everything, so the kills counter returns to 0
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void UpdateUI()
    {
        if (killsText != null) killsText.text = "Kills: " + kills;
    }
}