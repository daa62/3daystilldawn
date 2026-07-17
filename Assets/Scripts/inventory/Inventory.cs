using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Inventory : MonoBehaviour, IInventory
{
    [SerializeField] int maxSlots = GameManager.INVENTORY_MAX_SLOTS;

    readonly List<ItemData> items = new List<ItemData>();
    int usedSlots;

    // carried items survive door transitions here; the next scene's Inventory
    // refills itself from this list in Awake
    static readonly List<ItemData> carried = new List<ItemData>();

    public UnityEvent<List<ItemData>> onChanged = new UnityEvent<List<ItemData>>();

    void Awake()
    {
        foreach (ItemData item in carried) {
            items.Add(item);
            usedSlots += item.slotSize;
        }
    }

    public static void clearCarried() => carried.Clear();

    public bool addItem(ItemData item)
    {
        if (usedSlots + item.slotSize > maxSlots) {
            Debug.Log($"Inventory full — cannot add {item.itemName}");
            return false;
        }

        items.Add(item);
        usedSlots += item.slotSize;
        syncCarried();
        onChanged.Invoke(items);
        return true;
    }

    public bool removeItem(ItemData item)
    {
        if (!items.Remove(item)) return false;
        usedSlots -= item.slotSize;
        syncCarried();
        onChanged.Invoke(items);
        return true;
    }

    // drop onto the floor just ahead of the player as a re-pickupable world item
    public bool dropItem(ItemData item)
    {
        if (!removeItem(item)) return false;

        Vector3 spot = transform.position + transform.forward * 1.1f;
        // settle onto whatever surface is below the drop point
        if (Physics.Raycast(spot + Vector3.up, Vector3.down, out RaycastHit hit, 3f))
            spot = hit.point + Vector3.up * 0.03f;

        WorldItem.spawnDropped(item, spot);
        Sfx.play(Sfx.PICKUP, 0.5f);
        PickupFeed.notice($"Dropped {item.itemName}");
        return true;
    }

    public bool useItem(ItemData item)
    {
        if (!items.Contains(item)) return false;

        bool used = item.use(gameObject);
        if (used) {
            Sfx.play(Sfx.ITEM_USE);
            if (item.consumable) removeItem(item);
        }
        return used;
    }

    void syncCarried()
    {
        carried.Clear();
        carried.AddRange(items);
    }

    public ItemData firstOfType(ItemType type)
    {
        foreach (ItemData item in items)
            if (item.type == type) return item;
        return null;
    }

    public bool hasItem(ItemData item)                => items.Contains(item);
    public IReadOnlyList<ItemData> getItems()         => items;
    public int getUsedSlots()                         => usedSlots;
    public int getMaxSlots()                          => maxSlots;
}
