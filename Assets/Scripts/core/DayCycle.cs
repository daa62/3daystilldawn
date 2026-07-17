using System;
using UnityEngine;

// Tracks which day it is and which phase the player is in.
// Static so it survives scene loads; reset from the title screen on a new game.
public static class DayCycle
{
    public enum Phase { Morning, Scavenging, Night }

    public static int CurrentDay { get; private set; } = 1;
    public static Phase CurrentPhase { get; private set; } = Phase.Morning;

    public static event Action onChanged;

    // the title scene has no GameState, so a reset there can't seed counters yet;
    // GameState.Awake picks the seed up when the first gameplay scene loads
    static bool seedPending;

    public static void reset()
    {
        CurrentDay = 1;
        CurrentPhase = Phase.Morning;
        PlayerCondition.reset();     // unhurt and well-fed on a new game
        DaylightTimer.resetClock();  // fresh daylight budget
        LootState.reset();           // restock the store for a fresh run
        seedPending = true;
        applyPendingSeed();
        onChanged?.Invoke();
    }

    public static void applyPendingSeed()
    {
        var state = GameState.Instance;
        if (!seedPending || state == null) return;

        seedPending = false;
        state.setCounter(GameManager.COUNTER_FRIEND_HEALTH, GameManager.FRIEND_HEALTH_START);
        state.setCounter(GameManager.COUNTER_BOND, GameManager.FRIEND_BOND_START);
        state.setCounter(GameManager.COUNTER_LAST_RUN_BOND, 0);

        // GameState survives returning to the title screen, so wipe the old run's story
        state.clearFlag(GameManager.FLAG_FRIEND_MET);
        state.clearFlag(GameManager.FLAG_FRIEND_RESTING);
        state.clearFlag(GameManager.FLAG_REASSURED);
        state.clearFlag(GameManager.FLAG_DIED);
        state.clearFlag(GameManager.FLAG_NIGHT_FELL);
        state.clearFlag(GameManager.FLAG_CARED_OVERNIGHT);
        state.clearFlag(GameManager.FLAG_HAS_STORE_KEY);
        state.clearFlag(GameManager.FLAG_STORE_LOCKED);
        for (int day = 1; day <= GameManager.TOTAL_DAYS; day++)
            state.clearFlag(GameManager.MORNING_TALKED_PREFIX + day);
    }

    // player heads out the safe-room door to scavenge
    public static void startRun()
    {
        CurrentPhase = Phase.Scavenging;
        onChanged?.Invoke();
    }

    // player comes back through the store door
    public static void endRun()
    {
        CurrentPhase = Phase.Night;
        // whether tonight counts as "cared for" is decided by tonight's actions
        GameState.Instance?.clearFlag(GameManager.FLAG_CARED_OVERNIGHT);
        onChanged?.Invoke();
    }

    // called when the player rests. friend decay applies overnight so the decline
    // is what the player wakes up to; after the last night, load the ending
    public static void resolveNight()
    {
        var state = GameState.Instance;
        if (state != null) {
            int health = state.getCounter(GameManager.COUNTER_FRIEND_HEALTH);
            state.setCounter(GameManager.COUNTER_FRIEND_HEALTH,
                             Mathf.Max(0, health - GameManager.FRIEND_HEALTH_DECAY));
        }

        // the player gets hungrier too — stamina capacity shrinks unless they eat
        PlayerCondition.starve(GameManager.HUNGER_STAMINA_DECAY);

        if (CurrentDay >= GameManager.TOTAL_DAYS) {
            SceneLoader.load(GameManager.SCENE_ENDING);
            return;
        }

        CurrentDay++;
        CurrentPhase = Phase.Morning;
        // yesterday's early-return record belongs to yesterday
        state?.setCounter(GameManager.COUNTER_LAST_RUN_BOND, 0);
        onChanged?.Invoke();
    }
}
