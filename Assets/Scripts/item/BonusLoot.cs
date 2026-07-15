using UnityEngine;

// Variable-ratio reward on pickups: a chance of 1-2 bonus items on top of what
// was grabbed, drawn from the same item or the Resources/Items pool. Odds and
// count shrink as days pass — loot-rich day 1, lean final day.
public static class BonusLoot
{
    // indexed by CurrentDay (clamped); slot 0 unused
    static readonly float[] CHANCE_BY_DAY = { 0f, 0.75f, 0.5f, 0.25f };
    static readonly int[]   MAX_BY_DAY    = { 0, 2, 1, 1 };

    const float SAME_ITEM_CHANCE = 0.5f;   // otherwise any item from the pool

    static ItemData[] pool;

    public static void roll(IInventory inventory, ItemData found)
    {
        int day = Mathf.Clamp(DayCycle.CurrentDay, 1, CHANCE_BY_DAY.Length - 1);
        if (Random.value > CHANCE_BY_DAY[day]) return;

        int count = Random.Range(1, MAX_BY_DAY[day] + 1);
        for (int i = 0; i < count; i++) {
            ItemData bonus = Random.value < SAME_ITEM_CHANCE ? found : randomFromPool(found);
            if (bonus == null || !inventory.addItem(bonus)) return;   // full — quietly stop
            PickupFeed.push(bonus.itemName);
        }
    }

    static ItemData randomFromPool(ItemData fallback)
    {
        if (pool == null) pool = Resources.LoadAll<ItemData>("Items");
        return pool.Length > 0 ? pool[Random.Range(0, pool.Length)] : fallback;
    }
}
