using System;

public class TrapTile : AbstractTile
{
    public TrapTile(Action<BaseEntity> trigger = null) : base("trap", GameManager.TRAP_TILE_SPRITE, trigger)
    {
    }
}
