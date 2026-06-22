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
        scanForInteractable();

        if (currentTarget != null && Input.GetKeyDown(KeyCode.E)) {
            currentTarget.interact(this);
        }
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
