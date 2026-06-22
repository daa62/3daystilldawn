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
            var        icon  = slot.GetComponentInChildren<Image>();
            var        label = slot.GetComponentInChildren<TextMeshProUGUI>();

            if (icon  && item.icon) icon.sprite = item.icon;
            if (label)              label.text  = item.itemName;

            var button = slot.GetComponent<Button>();
            if (button) button.onClick.AddListener(() => inventory.useItem(item));
        }

        if (slotCountText) {
            slotCountText.text = $"{inventory.getUsedSlots()} / {inventory.getMaxSlots()} slots";
        }
    }
}
