using System.Collections.Generic;

public interface IInventory
{
    bool addItem(ItemData item);
    bool removeItem(ItemData item);
    bool hasItem(ItemData item);
    IReadOnlyList<ItemData> getItems();
    int getUsedSlots();
    int getMaxSlots();
}
