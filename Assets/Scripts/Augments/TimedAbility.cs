using UnityEngine;
using UnityEngine.InputSystem;

// A ready-made base for abilities that you switch on, that run for a while, and then recharge.
// A subclass only says how long they last and what happens (see AugmentRapidFire.cs).
//
//   READY  --(key pressed)-->  ACTIVE  --(duration over)-->  COOLING DOWN  --(cooldown over)-->  READY
public abstract class TimedAbility : AugmentAbility, IActivatable
{
    protected abstract float Duration { get; }    // seconds the effect lasts
    protected abstract float Cooldown { get; }    // seconds to recharge after it ended

    protected virtual void OnActivated() { }
    protected virtual void OnActiveTick(float deltaTime) { }
    protected virtual void OnEnded() { }

    private enum State { Ready, Active, Cooling }
    private State state = State.Ready;
    private float timer;

    public virtual Key ActivationKey { get { return Key.E; } }

    public bool IsReady { get { return state == State.Ready; } }
    public bool IsActive { get { return state == State.Active; } }

    public float CooldownProgress
    {
        get
        {
            if (state == State.Ready) return 1f;
            if (state == State.Active) return 0f;
            return 1f - timer / Cooldown;
        }
    }

    public float ActiveProgress { get { return state == State.Active ? timer / Duration : 0f; } }
    public float SecondsLeft { get { return state == State.Ready ? 0f : timer; } }

    public bool TryActivate()
    {
        if (state != State.Ready) return false;

        state = State.Active;
        timer = Duration;
        PlayUseSound();
        OnActivated();
        return true;
    }

    public override void Tick(float deltaTime)
    {
        if (state == State.Active)
        {
            OnActiveTick(deltaTime);
            timer -= deltaTime;
            if (timer <= 0f)
            {
                OnEnded();
                state = State.Cooling;
                timer = Cooldown;
            }
        }
        else if (state == State.Cooling)
        {
            timer -= deltaTime;
            if (timer <= 0f)
            {
                state = State.Ready;
                PlayReadySound();
            }
        }
    }

    public override void Shutdown()
    {
        if (state == State.Active) OnEnded();   // never leave an effect switched on
        state = State.Ready;
    }
}
