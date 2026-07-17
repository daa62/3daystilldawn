using UnityEngine;

// Demo hotkeys: jump straight to day-3 morning with meters staged for a chosen
// ending, a stocked inventory, and days 1-2 marked as played. Self-installing —
// no scene wiring. Works in any gameplay scene (needs GameState).
//
//   F9  — TURNS trajectory      (health 40, bond 60, neglected overnight)
//   F10 — BOTH SAVED trajectory (health 100, bond 60, cared for)
//   F11 — SLIPS AWAY trajectory (health 100, bond 20, cared for)
//
// Health decays -50 on the final rest, so F10 ends ~50 (saved), F9 ends 0 (turns)
// unless the demo driver intervenes — which is the point: the demo can still play
// items to swing the outcome live.
public class DebugCheats : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void bootstrap()
    {
        var go = new GameObject("DebugCheats");
        DontDestroyOnLoad(go);
        go.AddComponent<DebugCheats>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))  jumpToFinalDay(health: 40,  bond: 60, cared: false, "TURNS");
        if (Input.GetKeyDown(KeyCode.F10)) jumpToFinalDay(health: 100, bond: 60, cared: true,  "BOTH SAVED");
        if (Input.GetKeyDown(KeyCode.F11)) jumpToFinalDay(health: 100, bond: 20, cared: true,  "SLIPS AWAY");
    }

    void jumpToFinalDay(int health, int bond, bool cared, string trajectory)
    {
        var state = GameState.Instance;
        if (state == null) {
            Debug.LogWarning("[DebugCheats] No GameState — jump only works in gameplay scenes.");
            return;
        }

        // the meters that decide the ending
        state.setCounter(GameManager.COUNTER_FRIEND_HEALTH, health);
        state.setCounter(GameManager.COUNTER_BOND, bond);
        state.setCounter(GameManager.COUNTER_LAST_RUN_BOND, 0);

        // pretend days 1-2 happened: intro seen, mornings talked, last night's care state
        state.setFlag(GameManager.FLAG_FRIEND_MET);
        state.setFlag(GameManager.MORNING_TALKED_PREFIX + 1);
        state.setFlag(GameManager.MORNING_TALKED_PREFIX + 2);
        if (cared) state.setFlag(GameManager.FLAG_CARED_OVERNIGHT);
        else       state.clearFlag(GameManager.FLAG_CARED_OVERNIGHT);
        state.clearFlag(GameManager.FLAG_NIGHT_FELL);

        // a demo-worthy pack: enough to care for him or to swing the ending live
        Inventory.debugSeedCarried(
            Resources.Load<ItemData>("Items/CanFood"),
            Resources.Load<ItemData>("Items/BottleWater"),
            Resources.Load<ItemData>("Items/Medkit"),
            Resources.Load<ItemData>("Items/Antibiotics"),
            Resources.Load<ItemData>("Items/PlushBear"));

        DayCycle.debugSetDay(GameManager.TOTAL_DAYS);
        Debug.Log($"[DebugCheats] Jumped to day {GameManager.TOTAL_DAYS} morning — {trajectory} trajectory " +
                  $"(health {health}, bond {bond}, cared {cared}).");
        SceneLoader.load(GameManager.SCENE_SAFE_ROOM);
    }
}
