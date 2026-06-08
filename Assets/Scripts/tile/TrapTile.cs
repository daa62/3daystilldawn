public abstract class TrapTile : AbstractTile
{
    protected TrapTile(string id, string spritePath) : base(id, spritePath)
    {
    }

    public override abstract void onStep(BaseEntity entity);
}
