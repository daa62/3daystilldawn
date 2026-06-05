using UnityEngine;

public interface ITile : ISpriteConfigurable
{
    string getId();

    bool canEnter();

    void onStep(BaseEntity entity);

    Sprite getSprite();

}
