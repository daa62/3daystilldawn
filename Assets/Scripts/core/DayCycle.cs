using System;
using UnityEngine;

// The spine of the gameplay loop (spec: two scenes, one timer, three days).
// Tracks which day it is and which phase the player is in; everything else
// (night check-in, friend decay, the ending) hangs off these transitions.
// Static so it survives scene loads without any scene wiring — same pattern
// as Inventory's carried list. Reset from the title screen on a new game.
public static class DayCycle
{
    public enum Phase { Morning, Scavenging, Night }

    public static int CurrentDay { get; private set; } = 1;
    public static Phase CurrentPhase { get; private set; } = Phase.Morning;

    public static event Action onChanged;

    // The title scene has no GameState object, so a reset made there can't seed the
    // counters yet; GameState.Awake collects the seed when the first gameplay scene loads.
    static bool seedPending;

    // New game: day 1 morning, friend stats at their spec starting points.
    public static void reset()
    {
        CurrentDay = 1;
        CurrentPhase = Phase.Morning;
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

        // narrative flags survive returns to the title screen (GameState is
        // DontDestroyOnLoad) — a new game must not inherit the previous run's story
        state.clearFlag(GameManager.FLAG_FRIEND_MET);
        state.clearFlag(GameManager.FLAG_FRIEND_RESTING);
        state.clearFlag(GameManager.FLAG_REASSURED);
        state.clearFlag(GameManager.FLAG_DIED);
        state.clearFlag(GameManager.FLAG_NIGHT_FELL);
        state.clearFlag(GameManager.FLAG_CARED_OVERNIGHT);
        for (int day = 1; day <= GameManager.TOTAL_DAYS; day++)
            state.clearFlag(GameManager.MORNING_TALKED_PREFIX + day);
    }

    // Player heads out the safe-room door to scavenge.
    public static void startRun()
    {
        CurrentPhase = Phase.Scavenging;
        onChanged?.Invoke();
    }

    // Player comes back through the store door: evening at home.
    public static void endRun()
    {
        CurrentPhase = Phase.Night;
        // whether tonight counts as "cared for" is decided by tonight's actions
        GameState.Instance?.clearFlag(GameManager.FLAG_CARED_OVERNIGHT);
        onChanged?.Invoke();
    }

    // Night actions are done (currently: talking to the friend). The friend's body
    // fights the infection overnight — decay applies here so the decline is what the
    // player wakes up to. After the last night, the ending cascade takes over.
    public static void resolveNight()
    {
        var state = GameState.Instance;
        if (state != null) {
            int health = state.getCounter(GameManager.COUNTER_FRIEND_HEALTH);
            state.setCounter(GameManager.COUNTER_FRIEND_HEALTH,
                             Mathf.Max(0, health - GameManager.FRIEND_HEALTH_DECAY));
        }

        if (CurrentDay >= GameManager.TOTAL_DAYS) {
            SceneLoader.load(GameManager.SCENE_ENDING);
            return;
        }

        CurrentDay++;
        CurrentPhase = Phase.Morning;
        onChanged?.Invoke();
    }
}
