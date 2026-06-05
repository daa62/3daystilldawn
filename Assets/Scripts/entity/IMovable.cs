using UnityEngine;

public interface IMovable
{
    bool move(Vector2Int vec);

    void move(Vector3 vec);

    void teleport(Vector3 position);
}
