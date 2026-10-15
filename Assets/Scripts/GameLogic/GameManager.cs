using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;   // lets other scripts call GameManager.Instance

    [Header("HUD")]
    public TextMeshProUGUI killsText;     // the number next to the skull icon
    public UIPunch killsPunch;            // optional: makes the number "pop" on every kill

    [Header("Death screen")]
    public GameObject deathPanel;         // dark overlay + "YOU DIED" box (hidden until you die)
    public TextMeshProUGUI deathText;
    public float restartDelay = 3f;       // seconds until the map reloads
    public float deathFadeTime = 0.5f;

    // An extra line for the death screen. Mayhem puts the wave you reached here.
    [HideInInspector] public string deathSummary = "";

    private int kills = 0;
    private bool playerDead = false;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;   // never start a map frozen
    }

    void Start()
    {
        if (deathPanel != null) deathPanel.SetActive(false);
        else if (deathText != null) deathText.gameObject.SetActive(false);

        UpdateUI();
    }

    public void AddKill()
    {
        if (playerDead) return;   // kills after you die don't count
        kills++;
        UpdateUI();
        if (killsPunch != null) killsPunch.Punch();
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
            string summary = string.IsNullOrEmpty(deathSummary) ? "" : "\n" + deathSummary;
            deathText.text = "YOU DIED\n<size=48><color=#F2E6C0>Kills: " + kills + summary + "</color></size>";
        }

        float elapsed = 0f;

        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
            CanvasGroup group = deathPanel.GetComponent<CanvasGroup>();

            while (group != null && elapsed < deathFadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / deathFadeTime);
                yield return null;
            }
            if (group != null) group.alpha = 1f;
        }
        else if (deathText != null)
        {
            deathText.gameObject.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, restartDelay - elapsed));

        // Reloading the scene recreates everything, so the kill counter returns to 0
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void UpdateUI()
    {
        if (killsText != null) killsText.text = kills.ToString();
    }
}
