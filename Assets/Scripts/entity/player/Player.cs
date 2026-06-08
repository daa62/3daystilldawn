using UnityEngine;

public class Player : BaseEntity
{
    public const float SIZE_IN_CELLS = 1f;

    private float lastMoveTime = -999f;
    private float slowEffectEndTime = -1f;
    private int slideCellsRemaining = 0;
    private float slideEndTime = -1f;

    protected override float getSizeInCells()
    {
        return SIZE_IN_CELLS;
    }

    public void applySlowEffect(float duration)
    {
        slowEffectEndTime = Time.time + duration;
    }

    public void applySlideEffect(int cells, float duration)
    {
        slideCellsRemaining = cells;
        slideEndTime = Time.time + duration;
    }

    private float getEffectiveCooldown()
    {
        if (Time.time < slowEffectEndTime)
            return GameManager.PLAYER_MOVE_COOLDOWN / GameManager.SLOW_TILE_SPEED_MULTIPLIER;
        return GameManager.PLAYER_MOVE_COOLDOWN;
    }

    public override void tick()
    {
        if (isMoving) {
            return;
        }

        bool inSlide = slideCellsRemaining > 0 || Time.time < slideEndTime;
        if (inSlide) {
            if (slideCellsRemaining > 0 && Time.time - lastMoveTime >= GameManager.PLAYER_MOVE_COOLDOWN) {
                if (move(Vector2Int.down))
                    slideCellsRemaining--;
                else
                    slideCellsRemaining = 0;
                lastMoveTime = Time.time;
            }
            return;
        }

        if (Time.time - lastMoveTime < getEffectiveCooldown()) {
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
