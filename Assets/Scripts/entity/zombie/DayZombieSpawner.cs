using UnityEngine;

// Day escalation: spawns extra zombies on scene load based on the current day.
// None on day 1 — the hand-placed set is the baseline.
public class DayZombieSpawner : MonoBehaviour
{
    [Tooltip("Where escalation zombies appear. Spread these away from the store entrance.")]
    [SerializeField] Transform[] spawnPoints;

    [Tooltip("Zombie prefab. If unset, an existing scene zombie is cloned.")]
    [SerializeField] GameObject zombiePrefab;

    void Start()
    {
        int extras = (DayCycle.CurrentDay - 1) * GameManager.ZOMBIES_PER_EXTRA_DAY;
        ZombieSpawning.spawnAt(zombiePrefab, spawnPoints, extras, "DayZombieSpawner");
    }
}
