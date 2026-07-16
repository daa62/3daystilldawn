using UnityEngine;

// serialized by index in item assets — only append, never reorder
public enum ItemType { Survival, Comfort, Tool, Medicine }

[CreateAssetMenu(fileName = "NewItem", menuName = "3DaysTillDawn/Item")]
public class ItemData : ScriptableObject
{
    public string   itemName;
    [TextArea]
    public string   description;
    public Sprite   icon;
    public ItemType type;

    [Tooltip("How many inventory slots this item occupies (default 1)")]
    public int slotSize = 1;

    [Tooltip("If true, the item is removed from the inventory after a successful use")]
    public bool consumable = true;

    // self-use: food restores stamina capacity, medicine mends max HP.
    // comfort/tool items have no self-use — they're for Samuel
    public virtual bool use(GameObject user)
    {
        switch (type)
        {
            case ItemType.Survival:
                PlayerCondition.eat(GameManager.FOOD_STAMINA_RESTORE);
                return true;
            case ItemType.Medicine:
                PlayerCondition.treat(GameManager.MEDICINE_MAX_HP_RESTORE);
                return true;
            default:
                return false;
        }
    }
}
