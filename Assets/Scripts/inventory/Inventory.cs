using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Inventory : MonoBehaviour, IInventory
{
    [SerializeField] int maxSlots = GameManager.INVENTORY_MAX_SLOTS;

    readonly List<ItemData> items = new List<ItemData>();
    int usedSlots;

    public UnityEvent<List<ItemData>> onChanged = new UnityEvent<List<ItemData>>();

    public bool addItem(ItemData item)
    {
        if (usedSlots + item.slotSize > maxSlots) {
            Debug.Log($"Inventory full — cannot add {item.itemName}");
            return false;
        }

        items.Add(item);
        usedSlots += item.slotSize;
        onChanged.Invoke(items);
        return true;
    }

    public bool removeItem(ItemData item)
    {
        if (!items.Remove(item)) return false;
        usedSlots -= item.slotSize;
        onChanged.Invoke(items);
        return true;
    }

    public bool useItem(ItemData item)
    {
        if (!items.Contains(item)) return false;

        bool used = item.use(gameObject);
        if (used && item.consumable) removeItem(item);
        return used;
    }

    public bool hasItem(ItemData item)                => items.Contains(item);
    public IReadOnlyList<ItemData> getItems()         => items;
    public int getUsedSlots()                         => usedSlots;
    public int getMaxSlots()                          => maxSlots;
}
