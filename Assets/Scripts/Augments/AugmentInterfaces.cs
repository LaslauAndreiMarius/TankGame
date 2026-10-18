using UnityEngine;
using UnityEngine.InputSystem;

// Small "contracts" that an augment can choose to sign. The rest of the game only talks to these
// interfaces, never to a specific augment, so a new augment never forces changes anywhere else.

// An ability the player switches on with a key and then waits to recharge (Rapid Fire, Freeze...)
public interface IActivatable
{
    Key ActivationKey { get; }
    bool IsReady { get; }
    bool IsActive { get; }
    float CooldownProgress { get; }   // 0 = just used, 1 = recharged
    float ActiveProgress { get; }     // 1 = just started, 0 = over
    float SecondsLeft { get; }        // seconds until it ends (while active) or recharges (while cooling down)
    bool TryActivate();
}

// An augment that wants to tint the edges of the screen (the HUD draws it)
public interface IScreenEffect
{
    Color ScreenColor { get; }
    float ScreenIntensity { get; }    // 0 = nothing, 1 = strongest
}

// An augment that changes how health packs work
public interface IHealthPackModifier
{
    float ModifyRegenDuration(float duration);   // return a new regeneration time
    void OnHealthPackCollected(float amount);
}

// An augment that reacts when the player picks up a power-up box
public interface IPowerUpListener
{
    void OnPowerUpCollected(PowerUpType type);
}

// Anything that can be stopped in place. Enemy tanks implement it.
public interface IFreezable
{
    void SetFrozen(bool frozen);
}
