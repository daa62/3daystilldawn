using UnityEngine;

public class Player : BaseEntity
{
    public const float SIZE_IN_CELLS = 1f;

    private float lastMoveTime = -999f;

    protected override float getSizeInCells()
    {
        return SIZE_IN_CELLS;
    }

    public override void tick()
    {
        if (isMoving) {
            return;
        }

        if (Time.time - lastMoveTime < GameManager.PLAYER_MOVE_COOLDOWN) {
            return;
        }

        Vector2Int dir = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) {
            dir = Vector2Int.up;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) {
            dir = Vector2Int.down;
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) {
            dir = Vector2Int.left;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) {
            dir = Vector2Int.right;
        }

        if (dir == Vector2Int.up && getGridPos().y == map.height - 1) {
            GameManager.Instance.winGame();
            lastMoveTime = Time.time;
            return;
        }

        if (dir != Vector2Int.zero && move(dir)) {
            lastMoveTime = Time.time;
        }
    }
}
