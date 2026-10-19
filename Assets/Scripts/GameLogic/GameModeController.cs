using UnityEngine;


// It switches the scene to the mode chosen in the menu: Classic keeps the normal enemy spawner,
// Mayhem turns it off and starts the wave system instead.
[DefaultExecutionOrder(-300)]
public class GameModeController : MonoBehaviour
{
    public MayhemManager mayhem;
    public EnemySpawner classicSpawner;
    public GameObject mayhemHud;

    void Awake()
    {
        bool isMayhem = GameSettings.Mode == GameMode.Mayhem;

        if (classicSpawner == null) classicSpawner = FindFirstObjectByType<EnemySpawner>();
        if (mayhem == null) mayhem = FindFirstObjectByType<MayhemManager>();

        if (classicSpawner != null) classicSpawner.enabled = !isMayhem;
        if (mayhem != null) mayhem.enabled = isMayhem;
        if (mayhemHud != null) mayhemHud.SetActive(isMayhem);
    }
}
