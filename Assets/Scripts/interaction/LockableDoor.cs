using UnityEngine;

// The storefront doors. Examining them without the key just sets up the goal; once
// the player has found the store key (FLAG_HAS_STORE_KEY), one interaction throws the
// deadbolt for the rest of the run — DaylightTimer skips its night spawns while
// FLAG_STORE_LOCKED is set. Put it on the door mesh, Interactable layer.
[RequireComponent(typeof(Collider))]
public class LockableDoor : MonoBehaviour, IInteractable
{
    [SerializeField] string doorName = "the storefront";

    public string getPrompt()
    {
        var state = GameState.Instance;
        if (state != null && state.getFlag(GameManager.FLAG_STORE_LOCKED))
            return "Check the lock";
        if (state != null && state.getFlag(GameManager.FLAG_HAS_STORE_KEY))
            return "Lock " + doorName;
        return "Examine " + doorName;
    }

    public void interact(PlayerInteractor interactor)
    {
        var dialogue = DialogueUI.Instance;
        if (dialogue == null || dialogue.IsOpen) return;
        var state = GameState.Instance;

        if (state != null && state.getFlag(GameManager.FLAG_STORE_LOCKED)) {
            dialogue.show("", "(The deadbolt is thrown. Nothing is getting in through here tonight.)",
                          dialogue.close);
            return;
        }

        if (state != null && state.getFlag(GameManager.FLAG_HAS_STORE_KEY)) {
            Sfx.play(Sfx.DOOR);
            state.setFlag(GameManager.FLAG_STORE_LOCKED);
            dialogue.show("", "(The key turns stiffly, and the deadbolt slides home. The storefront is sealed.)",
                          dialogue.close);
            return;
        }

        dialogue.show("", "(The doors hang loose on their rails — they won't stay shut on their own. " +
                          "Somewhere in this store there must be a key.)",
                      dialogue.close);
    }
}
