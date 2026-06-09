using UnityEngine;

public class GridMap : BaseMap
{
    private AbstractTile[,] tiles;
    private Transform tileRoot;

    protected override void Awake()
    {
        base.Awake();
        buildStage();
    }

    public override void onMapLoad()
    {
        renderTiles();
    }

    public override void onMapUnload()
    {
    }

    public override bool hasTile(Vector2Int pos)
    {
        if (!isInBounds(pos)) {
            return false;
        }
        return tiles[pos.x, pos.y] != null;
    }

    public override bool canMoveTo(Vector2Int pos)
    {
        if (!hasTile(pos)) {
            return false;
        }
        return tiles[pos.x, pos.y].canEnter();
    }

    public override void callStepTrigger(Vector2Int pos, BaseEntity entity)
    {
        if (!hasTile(pos)) {
            return;
        }
        tiles[pos.x, pos.y].onStep(entity);
    }

    private void buildStage()
    {
        tiles = new AbstractTile[width, height];
        var rng = new System.Random();

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                tiles[x, y] = createTile(x, y, rng);
            }
        }
    }

    private AbstractTile createTile(int x, int y, System.Random rng)
    {
        if (x == 0 || x == width - 1) {
            return new WallTile();
        }

        if (y < GameManager.MAP_SAFE_ROWS) {
            return new DefaultTile();
        }

        double roll = rng.NextDouble();
        if (roll < GameManager.SLOW_TILE_SPAWN_CHANCE) {
            return new SlowTrapTile();
        }
        if (roll < GameManager.SLOW_TILE_SPAWN_CHANCE + GameManager.SLIDE_TILE_SPAWN_CHANCE) {
            return new SlideTrapTile();
        }
        if (roll < GameManager.SLOW_TILE_SPAWN_CHANCE + GameManager.SLIDE_TILE_SPAWN_CHANCE + GameManager.WALL_TILE_SPAWN_CHANCE) {
            return new WallTile();
        }

        return new DefaultTile();
    }

    private void renderTiles()
    {
        if (tileRoot != null) {
            Destroy(tileRoot.gameObject);
        }

        tileRoot = new GameObject("Tiles").transform;
        tileRoot.SetParent(transform);

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                AbstractTile tile = tiles[x, y];
                if (tile == null) {
                    continue;
                }
                renderTile(x, y, tile);
            }
        }
    }

    private void renderTile(int x, int y, AbstractTile tile)
    {
        var obj = new GameObject($"Tile_{x}_{y}_{tile.getId()}");
        obj.transform.SetParent(tileRoot);
        obj.transform.position = cellToWorld(new Vector2Int(x, y)) + new Vector3(0f, 0f, 0.2f);

        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = -2;
        tile.bindRenderer(renderer);
        applyTileScale(obj.transform, renderer.sprite);
    }

    private void applyTileScale(Transform target, Sprite sprite)
    {
        if (sprite == null) {
            target.localScale = new Vector3(cellSize, cellSize, 1f);
            return;
        }

        Vector2 spriteSize = sprite.bounds.size;
        target.localScale = new Vector3(cellSize / spriteSize.x, cellSize / spriteSize.y, 1f);
    }
}
