using UnityEngine;

// A readable note or examinable trace. Shows its text in the dialogue window;
// the first read can raise a GameState flag. Put it on the Interactable layer.
[RequireComponent(typeof(Collider))]
public class WorldReadable : MonoBehaviour, IInteractable
{
    [SerializeField] string title = "Note";
    [SerializeField] string promptVerb = "Read";                 // "Read" for notes, "Examine" for traces
    [TextArea(2, 6)] [SerializeField] string body = "";

    [Header("Optional narrative hooks")]
    [SerializeField] string discoverFlag = "";                   // GameState flag set on first read
    [SerializeField] bool removeAfterReading = false;            // e.g. a note the player pockets

    public string getPrompt() => promptVerb + " " + title;

    public void interact(PlayerInteractor interactor)
    {
        var dialogue = DialogueUI.Instance;
        if (dialogue == null || dialogue.IsOpen) return;

        var state = GameState.Instance;
        bool firstTime = state == null || string.IsNullOrEmpty(discoverFlag) || !state.getFlag(discoverFlag);

        Sfx.play(Sfx.PAPER);
        dialogue.show(title, body, () => dialogue.close());

        if (firstTime && state != null && !string.IsNullOrEmpty(discoverFlag))
            state.setFlag(discoverFlag);

        if (removeAfterReading)
            Destroy(gameObject);
    }
}
