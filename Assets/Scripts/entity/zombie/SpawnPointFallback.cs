using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runtime fallback for unwired spawner point arrays. When a scene's
// DayZombieSpawner/DaylightTimer has no spawn points assigned, points are
// generated from the hand-placed zombies (known-good floor spots). Wiring
// real points in the editor always wins — non-empty arrays are left alone.
public static class SpawnPointFallback
{
    const float JITTER_RADIUS = 1.5f;   // spread so spawns don't stack on the original zombie

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void hook()
    {
        SceneManager.sceneLoaded += (scene, mode) => inject();
    }

    // sceneLoaded fires after Awake but before Start, so points land
    // before either spawner reads its array
    static void inject()
    {
        var day   = Object.FindAnyObjectByType<DayZombieSpawner>();
        var night = Object.FindAnyObjectByType<DaylightTimer>();
        if (day == null && night == null) return;

        Transform[] points = null;   // built once, shared by both spawners

        if (day   != null) points = fillIfEmpty(day, "spawnPoints", points);
        if (night != null) fillIfEmpty(night, "nightSpawnPoints", points);
    }

    static Transform[] fillIfEmpty(Component target, string fieldName, Transform[] points)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) return points;

        var current = field.GetValue(target) as Transform[];
        if (current != null && current.Length > 0) return points;

        if (points == null) points = buildPoints();
        if (points.Length > 0) field.SetValue(target, points);
        return points;
    }

    static Transform[] buildPoints()
    {
        Zombie[] zombies = Object.FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        if (zombies.Length == 0) return new Transform[0];

        Transform root = new GameObject("FallbackSpawnPoints").transform;
        var points = new Transform[zombies.Length];
        for (int i = 0; i < zombies.Length; i++) {
            Transform point = new GameObject("Point" + i).transform;
            point.SetParent(root);
            point.position = groundNear(zombies[i].transform.position);
            points[i] = point;
        }
        return points;
    }

    static Vector3 groundNear(Vector3 origin)
    {
        Vector2 jitter = Random.insideUnitCircle * JITTER_RADIUS;
        Vector3 pos = origin + new Vector3(jitter.x, 0f, jitter.y);
        if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f))
            return hit.point;
        return origin;   // jittered spot had no floor (off a ledge etc.)
    }
}
