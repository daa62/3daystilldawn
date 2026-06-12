using UnityEngine;

public class Player : BaseEntity
{
    public const float SIZE_IN_CELLS = 1f;

    private float lastMoveTime = -999f;
    private float slowEffectEndTime = -1f;
    private float stunEffectEndTime = -1f;
    private int slideCellsRemaining = 0;
    private float slideNextMoveTime = -1f;

    protected override float getSizeInCells()
    {
        return SIZE_IN_CELLS;
    }

    public void applySlowEffect(float duration)
    {
        if (Time.time >= slowEffectEndTime) {
            FloatingText.Spawn("Slowed!", transform.position, new Color(1f, 0.8f, 0f));
            GameManager.Instance.addScore(-25);
        }
        slowEffectEndTime = Time.time + duration;
    }

    public void applyStunEffect(float duration)
    {
        if (Time.time >= stunEffectEndTime) {
            FloatingText.Spawn("Stunned!", transform.position, Color.red);
            GameManager.Instance.addScore(-75);
        }
        stunEffectEndTime = Time.time + duration;
    }

    public bool isStunned()
    {
        return Time.time < stunEffectEndTime;
    }

    public void applySlideEffect(int cells)
    {
        slideCellsRemaining = cells;
        slideNextMoveTime = Time.time;
        FloatingText.Spawn("Be Careful!", transform.position, new Color(1f, 0.4f, 0f));
        GameManager.Instance.addScore(-50);
    }

    private float getEffectiveCooldown()
    {
        if (Time.time < slowEffectEndTime)
            return GameManager.PLAYER_MOVE_COOLDOWN / GameManager.SLOW_TILE_SPEED_MULTIPLIER;
        return GameManager.PLAYER_MOVE_COOLDOWN;
    }

    public override void tick()
    {
        if (isMoving || isStunned()) {
            return;
        }

        if (slideCellsRemaining > 0) {
            if (Time.time >= slideNextMoveTime) {
                if (move(Vector2Int.down)) {
                    slideCellsRemaining--;
                    slideNextMoveTime += GameManager.SLIDE_CELL_INTERVAL;
                } else {
                    slideCellsRemaining = 0;
                }
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
