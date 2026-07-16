using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] KeyCode    toggleKey    = KeyCode.Tab;
    [SerializeField] GameObject panelRoot;
    [SerializeField] Transform  slotContainer;
    [SerializeField] GameObject slotPrefab;
    [SerializeField] TextMeshProUGUI slotCountText;

    Inventory        inventory;
    PlayerController playerController;

    void Start()
    {
        inventory        = FindAnyObjectByType<Inventory>();
        playerController = FindAnyObjectByType<PlayerController>();
        inventory.onChanged.AddListener(refresh);
        refresh(new List<ItemData>(inventory.getItems()));   // items carried in from the previous scene
        panelRoot.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) {
            toggle();
        }
    }

    void toggle()
    {
        Sfx.play(Sfx.UI_CLICK, 0.5f);
        bool open = !panelRoot.activeSelf;
        panelRoot.SetActive(open);
        playerController.lockCursor(!open);
    }

    void refresh(List<ItemData> items)
    {
        foreach (Transform child in slotContainer) {
            Destroy(child.gameObject);
        }

        foreach (ItemData item in items) {
            GameObject slot  = Instantiate(slotPrefab, slotContainer);
            var        icon  = findIcon(slot);
            var        label = slot.GetComponentInChildren<TextMeshProUGUI>();

            if (icon  && item.icon) icon.sprite = item.icon;
            if (label)              label.text  = item.itemName;

            // click to use (eat food / take meds); the prefab has no Button, so add one
            var button = slot.GetComponent<Button>();
            if (button == null) button = slot.AddComponent<Button>();
            if (button.targetGraphic == null) button.targetGraphic = icon;
            ItemData captured = item;
            button.onClick.AddListener(() => inventory.useItem(captured));
        }

        if (slotCountText) {
            slotCountText.text = $"{inventory.getUsedSlots()} / {inventory.getMaxSlots()} slots";
        }
    }

    // the slot's own Image is the background; the item sprite belongs on the "Icon" child.
    // GetComponentInChildren would grab the root background first, leaving the icon blank white.
    static Image findIcon(GameObject slot)
    {
        Transform iconTf = slot.transform.Find("Icon");
        if (iconTf && iconTf.TryGetComponent(out Image icon)) return icon;

        foreach (Image img in slot.GetComponentsInChildren<Image>())
            if (img.transform != slot.transform) return img;   // fallback: first non-background Image
        return null;
    }
}
