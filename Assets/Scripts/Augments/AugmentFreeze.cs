using System.Collections.Generic;
using UnityEngine;

// AUGMENT: Freeze. Press E to freeze every enemy on the map for a few seconds.
public class AugmentFreeze : AugmentBase
{
    public override string Id => "Freeze";
    public override string DisplayName => "Freeze";
    public override string CardName => "FREEZE";
    public override string Description => "Press E to freeze every enemy on the map. They cannot move or shoot until it wears off.";
    public override string Details => "TYPE: PRESS E\nLASTS " + FreezeAbility.DurationSeconds + " S    RECHARGE " + FreezeAbility.CooldownSeconds + " S";
    public override int Price => 2000;

    public override AugmentAbility CreateAbility() { return new FreezeAbility(); }
}

public class FreezeAbility : TimedAbility, IScreenEffect
{
    public const float DurationSeconds = 10f;
    public const float CooldownSeconds = 120f;

    protected override float Duration { get { return DurationSeconds; } }
    protected override float Cooldown { get { return CooldownSeconds; } }

    private readonly HashSet<EnemyAI> frozen = new HashSet<EnemyAI>();
    private float startTime;

    protected override void OnActivated()
    {
        startTime = Time.time;
        FreezeNewEnemies();
    }

    protected override void OnActiveTick(float deltaTime)
    {
        FreezeNewEnemies();   // enemies that spawn during the freeze are caught too
    }

    protected override void OnEnded()
    {
        foreach (EnemyAI enemy in frozen)
        {
            if (enemy == null) continue;   // it was destroyed in the meantime
            FrozenEffect effect = enemy.GetComponent<FrozenEffect>();
            if (effect != null) effect.Remove();
        }
        frozen.Clear();
    }

    void FreezeNewEnemies()
    {
        for (int i = 0; i < EnemyAI.All.Count; i++)
        {
            EnemyAI enemy = EnemyAI.All[i];
            if (frozen.Add(enemy))   // Add returns true only the first time we see this enemy
            {
                FrozenEffect effect = enemy.gameObject.AddComponent<FrozenEffect>();
                effect.Begin(SecondsLeft);
            }
        }
    }

    // ---- IScreenEffect: a bright icy flash when it starts, then a faint blue glow ----
    public Color ScreenColor { get { return new Color(0.5f, 0.85f, 1f); } }

    public float ScreenIntensity
    {
        get
        {
            if (!IsActive) return 0f;
            float flash = Mathf.Clamp01(1f - (Time.time - startTime) / 0.7f);
            return Mathf.Max(0.22f, flash);
        }
    }
}
