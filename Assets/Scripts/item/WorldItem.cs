using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour, IInteractable
{
    [SerializeField] ItemData itemData;

    // runtime-spawned (player drop): tracked by DroppedItems, not LootState
    bool dynamic;

    // in Start, not Awake: spawnDropped must be able to flag the item dynamic
    // right after AddComponent, before this check runs
    void Start()
    {
        if (!dynamic && LootState.isCollected(lootId()))
            Destroy(gameObject);   // picked up on an earlier day — don't restock
    }

    public string getPrompt() => $"Pick up {itemData.itemName}";

    public void interact(PlayerInteractor interactor)
    {
        if (interactor.getInventory().addItem(itemData)) {
            if (dynamic) DroppedItems.forget(itemData, transform.position);
            else         LootState.markCollected(lootId());
            Sfx.play(Sfx.PICKUP);
            PickupFeed.push(itemData.itemName);
            Destroy(gameObject);
        } else {
            PickupFeed.notice("Inventory full");
        }
    }

    // Builds a pickup for an item dropped from the inventory. Items carry no world
    // model (only ItemData + icon), so the drop renders as a small icon card lying
    // on the floor — guaranteed to exist for every item.
    public static WorldItem spawnDropped(ItemData item, Vector3 position, bool record = true)
    {
        var go = new GameObject($"Dropped {item.itemName}");
        go.transform.position = position;
        go.layer = LayerMask.NameToLayer(GameManager.INTERACTABLE_LAYER_NAME);

        var col  = go.AddComponent<BoxCollider>();
        col.size = new Vector3(0.35f, 0.12f, 0.35f);

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(quad.GetComponent<Collider>());   // the box above is the hit target
        quad.transform.SetParent(go.transform, false);
        quad.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        quad.transform.localRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
        quad.transform.localScale    = Vector3.one * 0.3f;

        if (item.icon != null) {
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.material.mainTexture = item.icon.texture;
            // HDRP/Lit alpha clip so the icon's transparent background stays invisible
            renderer.material.SetFloat("_AlphaCutoffEnable", 1f);
            renderer.material.SetFloat("_AlphaCutoff", 0.4f);
            renderer.material.EnableKeyword("_ALPHATEST_ON");
        }

        var worldItem = go.AddComponent<WorldItem>();
        worldItem.itemData = item;
        worldItem.dynamic  = true;
        if (record) DroppedItems.record(item, position);
        return worldItem;
    }

    // Stable across the daily scene reloads: the scene restores every item to the same
    // spot, so scene + position identifies this pickup without any authored id.
    string lootId()
    {
        Vector3 p = transform.position;
        return $"{gameObject.scene.name}:{p.x:F2}:{p.y:F2}:{p.z:F2}";
    }
}
