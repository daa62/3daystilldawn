using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{
    public GameObject collectiblePrefab;

    private BaseMap map;
    private bool spawned = false;

    void Update()
    {
        if (spawned) return;
        if (GameManager.Instance == null || !GameManager.Instance.isPlaying()) return;

        map = FindAnyObjectByType<BaseMap>();
        if (map == null) return;

        spawnCollectibles();
        spawned = true;
    }

    private void spawnCollectibles()
    {
        for (int i = 0; i < GameManager.COLLECTIBLE_COUNT; i++) {
            Vector2Int pos = randomWalkablePos();
            GameObject obj = Instantiate(collectiblePrefab, transform);
            var collectible = obj.GetComponent<CollectibleEntity>();
            if (collectible != null) {
                collectible.setSpawnPosition(pos);
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
