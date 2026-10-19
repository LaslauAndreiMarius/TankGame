using UnityEngine;

// The DATA of an augment: its name, price, picture and sounds. One small subclass per augment.
// The behavior lives in a separate AugmentAbility, which this class creates.
public abstract class AugmentBase : ScriptableObject
{
    [Header("Set by Tools > Tank Game > Setup Augments")]
    public Sprite icon;
    public AudioClip useSound;      // played when the augment is used
    public AudioClip readySound;    // played when it has recharged
    public AudioClip deniedSound;   // played when you press the key too early

    public abstract string Id { get; }
    public abstract string DisplayName { get; }   // shown in the garage
    public abstract string CardName { get; }      // short version for the card (about 10 letters at most)
    public abstract string Description { get; }
    public abstract string Details { get; }       // extra lines such as duration and cooldown
    public abstract int Price { get; }

    // Creates the object that actually does the work during a map
    public abstract AugmentAbility CreateAbility();
}
