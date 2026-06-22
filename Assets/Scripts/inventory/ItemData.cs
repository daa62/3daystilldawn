using UnityEngine;

public enum ItemType { Survival, Comfort, Tool }

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

    public virtual bool use(GameObject user)
    {
        return false;
    }
}
