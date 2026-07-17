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
    [SerializeField] bool sparkle = true;                        // shimmer until read, so clues stand out

    public string getPrompt() => promptVerb + " " + title;

    void Start()
    {
        if (sparkle && !alreadyRead())
            gameObject.AddComponent<ClueSparkle>();
    }

    // survives the daily scene reloads (LootState, same position-id trick as pickups),
    // so a note read on day 1 doesn't start shimmering again on day 2
    bool alreadyRead()
    {
        if (LootState.isCollected(readId())) return true;
        var state = GameState.Instance;
        return state != null && !string.IsNullOrEmpty(discoverFlag) && state.getFlag(discoverFlag);
    }

    string readId()
    {
        Vector3 p = transform.position;
        return $"read:{gameObject.scene.name}:{p.x:F2}:{p.y:F2}:{p.z:F2}";
    }

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

        LootState.markCollected(readId());   // no re-sparkle after the daily scene reload

        var glow = GetComponent<ClueSparkle>();
        if (glow != null) Destroy(glow);

        if (removeAfterReading)
            Destroy(gameObject);
    }
}
