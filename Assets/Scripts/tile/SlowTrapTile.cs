public class SlowTrapTile : TrapTile
{
    public SlowTrapTile() : base("slow_trap", GameManager.SLOW_TILE_SPRITE)
    {
    }

    public override void onStep(BaseEntity entity)
    {
        if (entity is Player player)
            player.applySlowEffect(GameManager.SLOW_TILE_DURATION);
    }
}
