using UnityEngine;
using UnityEngine.InputSystem;

// Lives in every map scene. At the start of a map it creates the behavior of the equipped augment,
// runs it every frame, and listens for the activation key.
public class AugmentController : MonoBehaviour
{
    public static AugmentController Instance { get; private set; }

    public AugmentAbility Current { get; private set; }

    private GameObject player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Current != null) Current.Shutdown();
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        AugmentBase equipped = AugmentProfile.Equipped;
        if (player == null || equipped == null) return;

        Current = equipped.CreateAbility();
        Current.Initialize(player, equipped);
    }

    void Update()
    {
        if (Current == null) return;

        if (player == null)   // the player died
        {
            Current.Shutdown();
            Current = null;
            return;
        }

        Current.Tick(Time.deltaTime);

        IActivatable activatable = Current as IActivatable;
        if (activatable != null && Keyboard.current != null &&
            Keyboard.current[activatable.ActivationKey].wasPressedThisFrame)
        {
            if (!activatable.TryActivate()) Current.PlayDeniedSound();   // still active or recharging
        }
    }

    // Other systems ask "does the current augment do X?" without knowing which augment it is
    public static T Get<T>() where T : class
    {
        return Instance != null ? Instance.Current as T : null;
    }
}
