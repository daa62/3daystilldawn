using UnityEngine;

// Door between the safe room and the store. Works in both directions — each
// scene's door points at the other. Needs a collider on the Interactable layer.
// Returning to the safe room ends the day's run: the player confirms first, and
// the safe-room door refuses to reopen until morning.
public class TransitionDoor : MonoBehaviour, IInteractable
{
    [SerializeField] string targetScene = GameManager.SCENE_MAIN;
    [SerializeField] string targetSpawnId = SpawnPoint.STORE_DOOR;
    [SerializeField] string prompt = "Open door";

    public string getPrompt() =>
        targetScene == GameManager.SCENE_MAIN && DayCycle.CurrentPhase == DayCycle.Phase.Night
            ? "The store can wait until morning"
            : prompt;

    public void interact(PlayerInteractor interactor)
    {
        if (targetScene == GameManager.SCENE_SAFE_ROOM) {
            confirmReturn();
            return;
        }

        if (targetScene == GameManager.SCENE_MAIN) {
            // the day's run is over — no second trip until morning
            if (DayCycle.CurrentPhase == DayCycle.Phase.Night) {
                var dialogue = DialogueUI.Instance;
                if (dialogue != null && !dialogue.IsOpen)
                    dialogue.show("",
                        "(You're in for the evening. Whatever is still out there can wait until morning.)",
                        dialogue.close);
                return;
            }
            DayCycle.startRun();
        }

        travel();
    }

    // heading in for the evening is a commitment — make sure the player means it.
    // once night has fallen there's no daylight left to lose, so just head in
    void confirmReturn()
    {
        var timer = DaylightTimer.Instance;
        if (timer == null || timer.NightFell) {
            recordEarlyReturn();   // records 0 after nightfall — keeps the counter honest
            DayCycle.endRun();
            travel();
            return;
        }

        var dialogue = DialogueUI.Instance;
        if (dialogue == null || dialogue.IsOpen) return;

        dialogue.showChoice("",
            "Head inside for the evening? Once you're in, you cannot go back out until morning.",
            new[] { "Go inside — done for today.", "Stay out a little longer." },
            pick =>
            {
                dialogue.close();
                if (pick != 0) return;

                recordEarlyReturn();
                DayCycle.endRun();
                travel();
            });
    }

    void travel()
    {
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
