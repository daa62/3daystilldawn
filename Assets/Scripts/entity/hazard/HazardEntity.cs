using UnityEngine;

public class HazardEntity : BaseEntity
{
    private static readonly Vector2Int[] DIRECTIONS = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private Player player;
    private float nextMoveTime;
    private Vector2Int spawnPos = new Vector2Int(-1, -1);
    private Vector2Int lastDir = Vector2Int.down;

    public void setSpawnPosition(Vector2Int pos)
    {
        spawnPos = pos;
    }

    protected override void Start()
    {
        spritePath = GameManager.HAZARD_SPRITE;
        base.Start();

        player = FindAnyObjectByType<Player>();
        nextMoveTime = Time.time + Random.Range(0f, GameManager.HAZARD_MOVE_INTERVAL);

        GetComponent<SpriteRenderer>().sortingOrder = 0;
    }

    public override void tick()
    {
        if (player == null) {
            player = FindAnyObjectByType<Player>();
            if (player == null) return;
        }

        checkPlayerContact();

        if (Time.time >= nextMoveTime) {
            moveRandom();
            nextMoveTime += GameManager.HAZARD_MOVE_INTERVAL;
        }
    }

    private void moveRandom()
    {
        // Shuffle directions, prefer continuing in the same direction
        Vector2Int[] dirs = (Vector2Int[])DIRECTIONS.Clone();
        for (int i = dirs.Length - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
        }

        // Try last direction first ~60% of the time for smoother wandering
        if (Random.value < 0.6f) {
            if (tryMoveThrough(lastDir)) return;
        }

        foreach (var dir in dirs) {
            if (tryMoveThrough(dir)) return;
        }
    }

    private bool tryMoveThrough(Vector2Int dir)
    {
        Vector2Int next = gridPos + dir;
        if (!map.canMoveTo(next)) return false;

        gridPos = next;
        moveStartWorldPos = transform.position;
        targetWorldPos = map.cellToWorld(gridPos);
        moveElapsedTime = 0f;
        moveDuration = GameManager.MOVE_INTERPOLATION_TIME;
        isMoving = true;
        lastDir = dir;
        updateFacing(dir);
        return true;
    }

    private void updateFacing(Vector2Int dir)
    {
        if (dir == Vector2Int.up)         setYaw(0);
        else if (dir == Vector2Int.right) setYaw(90);
        else if (dir == Vector2Int.down)  setYaw(180);
        else if (dir == Vector2Int.left)  setYaw(270);
    }

    private void checkPlayerContact()
    {
        if (gridPos == player.getGridPos()) {
            player.applyStunEffect(GameManager.HAZARD_STUN_DURATION);
        }
    }

    protected override Vector2Int startPosition()
    {
        if (spawnPos.x >= 0) return spawnPos;
        if (map == null) return Vector2Int.zero;
        return new Vector2Int(Random.Range(1, map.width - 1), Random.Range(0, map.height));
    }
}
