public class WallTile : AbstractTile
{
    public WallTile() : base("wall", GameManager.WALL_TILE_SPRITE)
    {
    }

    public override bool canEnter()
    {
        return false;
    }
}
