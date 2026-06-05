using UnityEngine;

public interface IStageMap
{
    bool isInBounds(Vector2Int pos);

    bool hasTile(Vector2Int pos);

    bool canMoveTo(Vector2Int pos);

    Vector3 cellToWorld(Vector2Int pos);

    void callStepTrigger(Vector2Int pos, BaseEntity entity);
}
