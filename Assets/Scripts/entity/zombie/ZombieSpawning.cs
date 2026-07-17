using UnityEngine;

// Spawn helper shared by the night-fall and day-escalation spawners.
// With no prefab assigned it clones an existing scene zombie.
public static class ZombieSpawning
{
    public static void spawnAt(GameObject prefab, Transform[] points, int count, string context)
    {
        if (count <= 0 || points == null || points.Length == 0) return;

        if (prefab == null) {
            Zombie existing = Object.FindAnyObjectByType<Zombie>();
            if (existing == null) {
                Debug.LogWarning($"[{context}] No zombie prefab assigned and no scene zombie to clone.");
                return;
            }
            prefab = existing.gameObject;
        }

        // shuffle a copy so which points fire varies run to run — unshuffled, only the
        // first `count` entries would ever be used and every day looked identical
        Transform[] order = (Transform[])points.Clone();
        for (int i = order.Length - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        for (int i = 0; i < count; i++) {
            Transform point = order[i % order.Length];
            Object.Instantiate(prefab, point.position, point.rotation);
        }
    }
}
