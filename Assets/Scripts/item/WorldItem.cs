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
            Destroy(gameObject);
        } else {
            Debug.Log("Inventory full!");
        }
    }
}
