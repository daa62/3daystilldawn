using System;
using UnityEngine;

public abstract class AbstractTile : ITile
{
    private readonly string id;
    private string spritePath;
    private readonly Action<BaseEntity> trigger;
    private SpriteRenderer renderer;

    protected AbstractTile(string id, string spritePath, Action<BaseEntity> trigger = null)
    {
        this.id = id;
        this.spritePath = spritePath;
        this.trigger = trigger;
    }

    public string getId()
    {
        return id;
    }

    public virtual bool canEnter()
    {
        return true;
    }

    public virtual void onStep(BaseEntity entity)
    {
        trigger?.Invoke(entity);
    }

    public Sprite getSprite()
    {
        return Resources.Load<Sprite>(spritePath);
    }

    public void setSprite(string path)
    {
        spritePath = path;
        if (renderer != null) {
            renderer.sprite = getSprite();
        }
    }

    public string getSpritePath()
    {
        return spritePath;
    }

    public void bindRenderer(SpriteRenderer renderer)
    {
        this.renderer = renderer;
        this.renderer.sprite = getSprite();
    }
}
