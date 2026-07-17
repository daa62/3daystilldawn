using UnityEngine;

// A barricade the player can smash through once they've found the right tool
// (default: the fire axe). Breaking it is LOUD — the crash rings out on the Noise
// bus, so nearby zombies come to investigate the shortcut you just opened.
// Cleared state persists across the daily scene reloads via the flag; a cleared
// barricade removes itself on load. Put it on the blockade root (with a collider
// covering the pile), Interactable layer.
[RequireComponent(typeof(Collider))]
public class BlockedPath : MonoBehaviour, IInteractable
{
    [SerializeField] string pathName = "the blocked passage";
    [Tooltip("GameState flag the player must have to break through.")]
    [SerializeField] string requiredFlag = GameManager.FLAG_HAS_FIRE_AXE;
    [Tooltip("GameState flag set once broken — keeps it open for the rest of the run.")]
    [SerializeField] string clearedFlag = GameManager.FLAG_PATH_CLEARED;
    [Tooltip("How far the crash carries on the noise bus. Sprinting is 30.")]
    [SerializeField] float noiseRadius = 30f;
    [TextArea(2, 4)]
    [SerializeField] string lockedText =
        "(Shelving and debris, wedged tight. Hands alone won't shift it — something with a heavy blade might.)";
    [Tooltip("Optional: flag showing the player has learned where the tool is (e.g. read the safety notice). Empty = always use lockedText.")]
    [SerializeField] string hintFlag = "read_axe_notice";
    [TextArea(2, 4)]
    [SerializeField] string hintedLockedText =
        "(Wedged tight, but an axe would make short work of it. The brown shelves — that safety notice said the axe lives there.)";
    [TextArea(2, 4)]
    [SerializeField] string breakText =
        "(The axe bites through the jammed shelving. A few swings and the way is open — but the noise rings out across the store.)";

    void Start()
    {
        // already smashed on an earlier visit — the way stays open
        var state = GameState.Instance;
        if (state != null && state.getFlag(clearedFlag))
            Destroy(gameObject);
    }

    public string getPrompt()
    {
        var state = GameState.Instance;
        bool hasTool = state != null && state.getFlag(requiredFlag);
        return (hasTool ? "Break through " : "Examine ") + pathName;
    }

    public void interact(PlayerInteractor interactor)
    {
        var dialogue = DialogueUI.Instance;
        if (dialogue == null || dialogue.IsOpen) return;

        var state = GameState.Instance;
        if (state == null || !state.getFlag(requiredFlag)) {
            // once they've read where the tool lives, the examine text remembers it
            bool hinted = state != null && !string.IsNullOrEmpty(hintFlag) && state.getFlag(hintFlag);
            dialogue.show("", hinted ? hintedLockedText : lockedText, dialogue.close);
            return;
        }

        state.setFlag(clearedFlag);
        Sfx.play(Sfx.CRASH);   // 2d: the player is standing right here, and a crash should fill the room
        Noise.emit(transform.position, noiseRadius);   // every zombie in earshot heads this way

        dialogue.show("", breakText, () => {
            dialogue.close();
            Destroy(gameObject);
        });
    }
}
