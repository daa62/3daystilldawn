using UnityEngine;

public class Antlion : BaseEntity
{
    public const float SIZE_IN_CELLS = 6f;

    private Player player;

    protected override void Start()
    {
        spritePath = GameManager.ANTLION_SPRITE;
        moveSpeed = GameManager.ANTLION_SPEED;
        base.Start();

        player = FindAnyObjectByType<Player>();

        var renderer = GetComponent<SpriteRenderer>();
        renderer.sortingOrder = -1;
    }

    protected override float getSizeInCells()
    {
        return SIZE_IN_CELLS;
    }

    public override void tick()
    {
        if (player == null) {
            player = FindAnyObjectByType<Player>();
            if (player == null) {
                return;
            }
        }

        Vector3 playerPos = player.transform.position;
        float xFollowRatio = 1f - Mathf.Exp(-Time.deltaTime / GameManager.ANTLION_X_ALIGN_TIME);
        float nextX = Mathf.Lerp(transform.position.x, playerPos.x, xFollowRatio);
        float desiredY = playerPos.y;
        float yDistance = playerPos.y - transform.position.y;
        bool isTooFar = yDistance > GameManager.ANTLION_MAX_Y_DISTANCE;
        float ySpeed = isTooFar ? GameManager.ANTLION_CATCH_UP_SPEED : moveSpeed;
        float nextY = Mathf.MoveTowards(transform.position.y, desiredY, ySpeed * Time.deltaTime);

        float lowestAllowedY = playerPos.y - GameManager.ANTLION_MAX_Y_DISTANCE;
        if (isTooFar || nextY < lowestAllowedY) {
            nextY = lowestAllowedY;
        }

        Vector3 nextPos = new Vector3(nextX, nextY, 0f);
        move(nextPos - transform.position);

        if (isTouchingPlayer(player, nextPos)) {
            GameManager.Instance.gameOver();
        }
    }

    private bool isTouchingPlayer(Player player, Vector3 antlionPos)
    {
        float halfSize = getSize() * 0.5f;
        float playerHalfSize = player.getSize() * 0.5f;
        Vector3 playerPos = player.transform.position;

        return Mathf.Abs(antlionPos.x - playerPos.x) <= halfSize + playerHalfSize
            && Mathf.Abs(antlionPos.y - playerPos.y) <= halfSize + playerHalfSize;
    }

    protected override Vector2Int startPosition()
    {
        return new Vector2Int(GameManager.MAP_WIDTH / 2, -Mathf.RoundToInt(GameManager.ANTLION_START_Y_OFFSET));
    }
}
