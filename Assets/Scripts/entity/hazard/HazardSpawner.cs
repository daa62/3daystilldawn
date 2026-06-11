using UnityEngine;

public class HazardSpawner : MonoBehaviour
{
    public GameObject hazardPrefab;

    private BaseMap map;
    private bool spawned = false;

    void Update()
    {
        if (spawned) return;
        if (GameManager.Instance == null || !GameManager.Instance.isPlaying()) return;

        map = FindAnyObjectByType<BaseMap>();
        if (map == null) return;

        spawnHazards();
        spawned = true;
    }

    private void spawnHazards()
    {
        for (int i = 0; i < GameManager.HAZARD_COUNT; i++) {
            Vector2Int pos = randomWalkablePos();
            GameObject obj = Instantiate(hazardPrefab, transform);
            var hazard = obj.GetComponent<HazardEntity>();
            if (hazard != null) {
                hazard.setSpawnPosition(pos);
            }
        }
    }

    private Vector2Int randomWalkablePos()
    {
        for (int attempt = 0; attempt < 100; attempt++) {
            var pos = new Vector2Int(
                Random.Range(1, map.width - 1),
                Random.Range(GameManager.MAP_SAFE_ROWS, map.height)
            );
            if (map.canMoveTo(pos)) return pos;
        }
        return new Vector2Int(map.width / 2, GameManager.MAP_SAFE_ROWS);
    }
}
