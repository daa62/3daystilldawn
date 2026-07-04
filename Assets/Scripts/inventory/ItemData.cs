using UnityEngine;

// Serialized by index in item assets — only append, never reorder.
// Survival = food/water (friend health at night), Medicine = meds (bigger heal),
// Comfort = personal items (bond when given at night).
public enum ItemType { Survival, Comfort, Tool, Medicine }

[CreateAssetMenu(fileName = "NewItem", menuName = "3DaysTillDawn/Item")]
public class ItemData : ScriptableObject
{
    public string   itemName;
    [TextArea]
    public string   description;
    public Sprite   icon;
    public ItemType type;

    [Tooltip("Comfort items occupy 2 slots; survival and tool items occupy 1")]
    public int slotSize = 1;

    [Tooltip("If true, the item is removed from the inventory after a successful use")]
    public bool consumable = true;

    // Self-use from the inventory: food restores the player's stamina capacity,
    // medicine mends max HP (both live in PlayerCondition, so they persist across
    // scenes). Comfort and tool items have no self-use — they're for Samuel / later.
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
