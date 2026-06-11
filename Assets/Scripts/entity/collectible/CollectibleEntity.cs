using UnityEngine;

public class CollectibleEntity : BaseEntity
{
    private Vector2Int spawnPos = new Vector2Int(-1, -1);
    private bool collected = false;

    public void setSpawnPosition(Vector2Int pos)
    {
        spawnPos = pos;
    }

    protected override void Start()
    {
        spritePath = GameManager.COLLECTIBLE_SPRITE;
        base.Start();

        GetComponent<SpriteRenderer>().sortingOrder = 1;
    }

    public override void tick()
    {
        if (collected) return;

        Player player = FindAnyObjectByType<Player>();
        if (player != null && gridPos == player.getGridPos()) {
            collected = true;
            GameManager.Instance.addScore(GameManager.COLLECTIBLE_SCORE_VALUE);
            FloatingText.Spawn($"+{GameManager.COLLECTIBLE_SCORE_VALUE}", transform.position, Color.yellow);
            Destroy(gameObject);
        }
    }

    protected override Vector2Int startPosition()
    {
        if (spawnPos.x >= 0) return spawnPos;
        if (map == null) return Vector2Int.zero;
        return new Vector2Int(Random.Range(1, map.width - 1), Random.Range(GameManager.MAP_SAFE_ROWS, map.height));
    }
}
