public class SlideTrapTile : TrapTile
{
    public SlideTrapTile() : base("slide_trap", GameManager.SLIDE_TILE_SPRITE)
    {
    }

    public override void onStep(BaseEntity entity)
    {
        if (entity is Player player)
            player.applySlideEffect(GameManager.SLIDE_TILE_CELLS, GameManager.SLIDE_TILE_DURATION);
    }
}
