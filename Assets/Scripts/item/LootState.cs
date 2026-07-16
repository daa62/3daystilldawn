using System.Collections.Generic;

// Which world items have already been scavenged. Static so it survives the GameScene
// reloading each day: the store depletes across the three days instead of restocking.
// Plain static (like Inventory.carried) — no DontDestroyOnLoad needed; cleared on a
// new game via DayCycle.reset().
public static class LootState
{
    static readonly HashSet<string> collected = new();

    public static bool isCollected(string id) => collected.Contains(id);

    public static void markCollected(string id) => collected.Add(id);

    public static void reset() => collected.Clear();
}
