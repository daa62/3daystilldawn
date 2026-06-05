using UnityEngine;

public abstract class BaseMap : MonoBehaviour, IStageMap
{
    public int width = GameManager.MAP_WIDTH;
    public int height = GameManager.MAP_HEIGHT;
    public float cellSize = GameManager.CELL_SIZE;

    protected virtual void Awake()
    {
        width = GameManager.MAP_WIDTH;
        height = GameManager.MAP_HEIGHT;
        cellSize = GameManager.CELL_SIZE;
    }

    public void load()
    {
        onMapLoad();
    }

    public void unload()
    {
        onMapUnload();
    }

    public abstract void onMapLoad();

    public abstract void onMapUnload();

    public Vector3 cellToWorld(Vector2Int pos)
    {
        float half = cellSize * 0.5f;
        return new Vector3(pos.x * cellSize + half, pos.y * cellSize + half, 0f);
    }

    public bool isInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    public abstract bool hasTile(Vector2Int pos);

    public abstract bool canMoveTo(Vector2Int pos);

    public abstract void callStepTrigger(Vector2Int pos, BaseEntity entity);
}
