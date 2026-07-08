using System;
using UnityEngine;

// Lasting condition: max HP (zombie wounds vs medicine) and stamina capacity
// (hunger vs food). Static because Health/Stamina are per-scene components and
// would forget wounds at every door.
public static class PlayerCondition
{
    public static float MaxHealth  { get; private set; } = GameManager.PLAYER_MAX_HEALTH;
    public static float MaxStamina { get; private set; } = GameManager.STAMINA_START_MAX;

    public static event Action onChanged;

    // new game: unhurt, but already a little hungry
    public static void reset()
    {
        MaxHealth  = GameManager.PLAYER_MAX_HEALTH;
        MaxStamina = GameManager.STAMINA_START_MAX;
        onChanged?.Invoke();
    }

    public static void wound(float amount) =>
        setMaxHealth(MaxHealth - amount);

    public static void treat(float amount) =>
        setMaxHealth(MaxHealth + amount);

    public static void starve(float amount) =>
        setMaxStamina(MaxStamina - amount);

    public static void eat(float amount) =>
        setMaxStamina(MaxStamina + amount);

    static void setMaxHealth(float value)
    {
        float clamped = Mathf.Clamp(value, GameManager.PLAYER_MIN_MAX_HEALTH, GameManager.PLAYER_MAX_HEALTH);
        if (Mathf.Approximately(clamped, MaxHealth)) return;
        MaxHealth = clamped;
        onChanged?.Invoke();
    }

    static void setMaxStamina(float value)
    {
        float clamped = Mathf.Clamp(value, GameManager.STAMINA_MIN_MAX, GameManager.STAMINA_MAX);
        if (Mathf.Approximately(clamped, MaxStamina)) return;
        MaxStamina = clamped;
        onChanged?.Invoke();
    }
}
