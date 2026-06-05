using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundRenderer : MonoBehaviour
{
    public string spritePath = GameManager.BACKGROUND_SPRITE;

    void Start()
    {
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null) {
            return;
        }

        float width = GameManager.MAP_WIDTH * GameManager.CELL_SIZE;
        float height = GameManager.MAP_HEIGHT * GameManager.CELL_SIZE;

        var renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = GameManager.BACKGROUND_SORTING_ORDER;

        transform.position = new Vector3(width * 0.5f, height * 0.5f, GameManager.BACKGROUND_Z);
        Vector2 spriteSize = sprite.bounds.size;
        transform.localScale = new Vector3(width / spriteSize.x, height / spriteSize.y, 1f);
    }
}
