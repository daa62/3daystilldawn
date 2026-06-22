using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "3DaysTillDawn/Consumable Item")]
public class ConsumableItemData : ItemData
{
    [Tooltip("Optional flag set on GameState when this item is used")]
    public string setFlagOnUse;

    public override bool use(GameObject user)
    {
        if (!string.IsNullOrEmpty(setFlagOnUse) && GameState.Instance != null) {
            GameState.Instance.setFlag(setFlagOnUse);
        }

        return true;
    }
}
