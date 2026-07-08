using UnityEngine;

// Door between the safe room and the store. Works in both directions — each
// scene's door points at the other. Needs a collider on the Interactable layer.
public class TransitionDoor : MonoBehaviour, IInteractable
{
    [SerializeField] string targetScene = GameManager.SCENE_MAIN;
    [SerializeField] string targetSpawnId = SpawnPoint.STORE_DOOR;
    [SerializeField] string prompt = "Open door";

    public string getPrompt() => prompt;

    public void interact(PlayerInteractor interactor)
    {
        if (targetScene == GameManager.SCENE_SAFE_ROOM) {
            recordEarlyReturn();
            DayCycle.endRun();
        }
        else if (targetScene == GameManager.SCENE_MAIN) {
            DayCycle.startRun();
        }

        Sfx.play(Sfx.DOOR);
        SpawnPoint.nextSpawnId = targetSpawnId;
        SceneLoader.load(targetScene);
    }

    // Only records the early-return bump — FriendNpc banks it on "Rest until morning".
    // Each return overwrites the record, so the day's last return is what counts.
    void recordEarlyReturn()
    {
        GameState state = GameState.Instance;
        if (state == null) return;

        DaylightTimer timer = DaylightTimer.Instance;
        int bump = 0;
        if (timer != null && !timer.NightFell) {
            int minutesLeft = Mathf.FloorToInt(timer.RemainingSeconds / 60f);
            bump = minutesLeft * GameManager.BOND_PER_EARLY_MINUTE;
        }

        state.setCounter(GameManager.COUNTER_LAST_RUN_BOND, bump);
    }
}
