using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Items the player dropped on the floor, remembered per scene. Static so drops
// survive the daily scene reloads (like LootState); each scene load respawns the
// drops that belong to it. Cleared on a new game via DayCycle.reset().
public static class DroppedItems
{
    class Entry
    {
        public string scene;
        public ItemData item;
        public Vector3 position;
    }

    static readonly List<Entry> entries = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void hook()
    {
        SceneManager.sceneLoaded += (scene, mode) => respawn(scene.name);
    }

    public static void record(ItemData item, Vector3 position)
    {
        entries.Add(new Entry {
            scene = SceneManager.GetActiveScene().name, item = item, position = position });
    }

    // called when a dropped item is picked back up
    public static void forget(ItemData item, Vector3 position)
    {
        for (int i = 0; i < entries.Count; i++) {
            if (entries[i].item == item &&
                (entries[i].position - position).sqrMagnitude < 0.01f) {
                entries.RemoveAt(i);
                return;
            }
        }
    }

    public static void reset() => entries.Clear();

    static void respawn(string sceneName)
    {
        foreach (Entry e in entries)
            if (e.scene == sceneName)
                WorldItem.spawnDropped(e.item, e.position, record: false);
    }
}
