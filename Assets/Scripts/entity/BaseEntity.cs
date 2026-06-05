using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public abstract class BaseEntity : MonoBehaviour, IEntity, ITickable, IMovable, ISpriteConfigurable
{
    public float moveSpeed = GameManager.ENTITY_MOVE_SPEED;
    public string spritePath = GameManager.PLAYER_SPRITE;

    protected BaseMap map;
    protected Vector2Int gridPos;
    protected Vector3 moveStartWorldPos;
    protected Vector3 targetWorldPos;
    protected float moveElapsedTime;
    protected float moveDuration = GameManager.MOVE_INTERPOLATION_TIME;
    protected bool isMoving;
    protected int yaw;

    protected virtual void Start()
    {
        map = FindAnyObjectByType<BaseMap>();
        gridPos = startPosition();
        targetWorldPos = map.cellToWorld(gridPos);
        transform.position = targetWorldPos;
        setSprite(spritePath);
        applyEntityScale();
        setYaw(0);
        map.callStepTrigger(gridPos, this);
    }

    protected virtual void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isPlaying()) {
            return;
        }

        tick();
        smoothMove();
    }

    public abstract void tick();

    protected virtual Vector2Int startPosition()
    {
        return new Vector2Int(map.width / 2, 0);
    }

    protected virtual float getSizeInCells()
    {
        return 1f;
    }

    protected void applyEntityScale()
    {
        float size = getSizeInCells() * GameManager.CELL_SIZE;
        Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        if (sprite == null) {
            transform.localScale = new Vector3(size, size, 1f);
            return;
        }

        Vector2 spriteSize = sprite.bounds.size;
        transform.localScale = new Vector3(size / spriteSize.x, size / spriteSize.y, 1f);
    }

    public virtual bool move(Vector2Int vec)
    {
        if (isMoving) {
            return false;
        }

        Vector2Int next = gridPos + vec;
        if (!map.canMoveTo(next)) {
            return false;
        }

        gridPos = next;
        moveStartWorldPos = transform.position;
        targetWorldPos = map.cellToWorld(gridPos);
        moveElapsedTime = 0f;
        moveDuration = GameManager.MOVE_INTERPOLATION_TIME;
        isMoving = true;
        updateYaw(vec);
        return true;
    }

    public virtual void move(Vector3 vec)
    {
        targetWorldPos = transform.position + vec;
        transform.position = targetWorldPos;
    }

    public virtual void teleport(Vector3 position)
    {
        targetWorldPos = position;
        transform.position = position;
        isMoving = false;
    }

    protected virtual void smoothMove()
    {
        if (!isMoving) {
            return;
        }

        moveElapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(moveElapsedTime / moveDuration);
        transform.position = Vector3.Lerp(moveStartWorldPos, targetWorldPos, t);

        if (t >= 1f) {
            transform.position = targetWorldPos;
            isMoving = false;
            map.callStepTrigger(gridPos, this);
        }
    }

    public void setSprite(string path)
    {
        spritePath = path;

        var sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null) {
            sprite = Resources.Load<Sprite>(GameManager.FALLBACK_PLAYER_SPRITE);
        }

        if (sprite == null) {
            return;
        }

        GetComponent<SpriteRenderer>().sprite = sprite;
        applyEntityScale();
    }

    public string getSpritePath()
    {
        return spritePath;
    }

    private void updateYaw(Vector2Int vec)
    {
        if (vec == Vector2Int.up) {
            setYaw(0);
        }
        else if (vec == Vector2Int.right) {
            setYaw(90);
        }
        else if (vec == Vector2Int.down) {
            setYaw(180);
        }
        else if (vec == Vector2Int.left) {
            setYaw(270);
        }
    }

    public void setYaw(int yaw)
    {
        this.yaw = ((yaw % 360) + 360) % 360;
        transform.rotation = Quaternion.Euler(0f, 0f, -this.yaw);
    }

    public int getYaw()
    {
        return yaw;
    }

    public float getSize()
    {
        return getSizeInCells() * GameManager.CELL_SIZE;
    }

    public Vector2Int getGridPos()
    {
        return gridPos;
    }
}
