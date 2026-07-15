using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour, IInteractable
{
    [SerializeField] ItemData itemData;

    public string getPrompt() => $"Pick up {itemData.itemName}";

    public void interact(PlayerInteractor interactor)
    {
        if (interactor.getInventory().addItem(itemData)) {
            Sfx.play(Sfx.PICKUP);
            PickupFeed.push(itemData.itemName);
            BonusLoot.roll(interactor.getInventory(), itemData);
            Destroy(gameObject);
        } else {
            PickupFeed.notice("Inventory full");
        }
    }
}
