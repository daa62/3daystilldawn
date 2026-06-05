using UnityEngine;

public interface IEntity
{
    Vector2Int getGridPos();

    int getYaw();

    void setYaw(int yaw);
}
