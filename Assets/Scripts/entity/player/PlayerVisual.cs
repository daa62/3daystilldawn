using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerVisual : MonoBehaviour
{
    void Start()
    {
        if (GetComponent<BaseEntity>() != null) {
            return;
        }

        var sprite = Resources.Load<Sprite>(GameManager.FALLBACK_PLAYER_SPRITE);
        if (sprite != null) {
            GetComponent<SpriteRenderer>().sprite = sprite;
        }
    }
}
