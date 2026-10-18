using UnityEngine;

// The BEHAVIOR of an augment while a map is running. A new instance is created every time a map starts,
// so it can keep its own timers without touching the shared augment data.
public abstract class AugmentAbility
{
    public GameObject Player { get; private set; }
    public AugmentBase Definition { get; private set; }

    public void Initialize(GameObject player, AugmentBase definition)
    {
        Player = player;
        Definition = definition;
        OnInitialize();
    }

    protected virtual void OnInitialize() { }        // the map just started
    public virtual void Tick(float deltaTime) { }    // every frame
    public virtual void Shutdown() { }               // the map ended or the player died: undo anything you changed

    protected void PlayUseSound()     { Play(Definition.useSound); }
    public void PlayReadySound()      { Play(Definition.readySound); }
    public void PlayDeniedSound()     { Play(Definition.deniedSound); }

    void Play(AudioClip clip)
    {
        if (clip != null && Player != null) Sfx.Play(clip, Player.transform.position);
    }
}
