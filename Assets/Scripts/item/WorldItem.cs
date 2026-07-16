using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour, IInteractable
{
    [SerializeField] ItemData itemData;

    void Awake()
    {
        // picked up on an earlier day? the store stays empty — don't restock it.
        if (LootState.isCollected(lootId()))
            Destroy(gameObject);
    }

    public string getPrompt() => $"Pick up {itemData.itemName}";

    public void interact(PlayerInteractor interactor)
    {
        if (interactor.getInventory().addItem(itemData)) {
            LootState.markCollected(lootId());
            Sfx.play(Sfx.PICKUP);
            PickupFeed.push(itemData.itemName);
            Destroy(gameObject);
        } else {
            PickupFeed.notice("Inventory full");
        }
    }

    // Stable across the daily scene reloads: the scene restores every item to the same
    // spot, so scene + position identifies this pickup without any authored id.
    string lootId()
    {
        Vector3 p = transform.position;
        return $"{gameObject.scene.name}:{p.x:F2}:{p.y:F2}:{p.z:F2}";
    }
}
