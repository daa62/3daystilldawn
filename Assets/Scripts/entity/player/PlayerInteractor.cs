using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    Camera cam;
    HUD hud;
    Inventory inventory;
    IInteractable currentTarget;
    int interactMask;

    void Start()
    {
        cam       = GetComponentInChildren<Camera>();
        hud       = FindAnyObjectByType<HUD>();
        inventory = GetComponent<Inventory>();

        int layer = LayerMask.NameToLayer(GameManager.INTERACTABLE_LAYER_NAME);
        interactMask = layer < 0 ? 0 : 1 << layer;
    }

    void Update()
    {
        // while the dialogue window owns input, hide the world prompt ("[E] Talk to Mia")
        // and don't scan — the E press belongs to the conversation, not the NPC behind it.
        if (dialogueSwallowsInput()) {
            setTarget(null);
            return;
        }

        scanForInteractable();

        if (currentTarget != null && Input.GetKeyDown(KeyCode.E)) {
            currentTarget.interact(this);
        }
    }

    // E also advances dialogue lines: while a conversation is open (or on the very
    // frame it closed) the press belongs to the dialogue window, not the world.
    bool dialogueSwallowsInput()
    {
        var dialogue = DialogueUI.Instance;
        if (dialogue == null) return false;
        return dialogue.IsOpen || dialogue.LastClosedFrame == Time.frameCount;
    }

    void scanForInteractable()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, GameManager.INTERACT_RANGE, interactMask)) {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null) {
                setTarget(interactable);
                return;
            }
        }

        setTarget(null);
    }

    void setTarget(IInteractable target)
    {
        currentTarget = target;
        hud?.setPrompt(target != null ? $"[E]  {target.getPrompt()}" : string.Empty);
    }

    public Inventory getInventory() => inventory;
}
