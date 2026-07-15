using UnityEngine;
using UnityEngine.SceneManagement;

// Final-day pressure. DayZombieSpawner's escalation is (day-1)*ZOMBIES_PER_EXTRA_DAY,
// which lands at +4 on the last day; this tops it up to +5 for a harder finale
// without changing the spawner or the shared constant (day 2 stays at +2).
public static class FinalDayEscalation
{
    const int   TOPUP         = 1;
    const float JITTER_RADIUS = 1.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void hook()
    {
        SceneManager.sceneLoaded += (scene, mode) => spawn();
    }

    static void spawn()
    {
        if (DayCycle.CurrentDay < GameManager.TOTAL_DAYS) return;
        if (Object.FindAnyObjectByType<DayZombieSpawner>() == null) return;   // store scene only

        Zombie[] zombies = Object.FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        if (zombies.Length == 0) return;

        for (int i = 0; i < TOPUP; i++) {
            Transform source = zombies[Random.Range(0, zombies.Length)].transform;
            Vector2 jitter = Random.insideUnitCircle * JITTER_RADIUS;
            Object.Instantiate(source.gameObject,
                source.position + new Vector3(jitter.x, 0f, jitter.y), source.rotation);
        }
    }
}
