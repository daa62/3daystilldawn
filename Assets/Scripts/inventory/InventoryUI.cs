using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
        buildControlsHint();
        panelRoot.SetActive(false);
    }

    // small reminder along the panel's bottom edge, visible whenever the inventory is
    void buildControlsHint()
    {
        var hint = UiFactory.text(panelRoot.transform, "ControlsHint",
            "Left-click  —  use / eat     Right-click  —  drop", 20,
            new Color(1f, 1f, 1f, 0.55f), TextAlignmentOptions.Center);
        UiFactory.anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        hint.rectTransform.anchoredPosition = new Vector2(0f, 16f);
        hint.rectTransform.sizeDelta = new Vector2(760f, 28f);
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

            // left-click to use (eat food / take meds); the prefab has no Button, so add one
            var button = slot.GetComponent<Button>();
            if (button == null) button = slot.AddComponent<Button>();
            if (button.targetGraphic == null) button.targetGraphic = icon;
            ItemData captured = item;
            button.onClick.AddListener(() => inventory.useItem(captured));

            // right-click to drop it on the floor (Button only fires on left-click)
            var rightClick = slot.AddComponent<SlotRightClick>();
            rightClick.onRight = () => inventory.dropItem(captured);
        }

        if (slotCountText) {
            slotCountText.text = $"{inventory.getUsedSlots()} / {inventory.getMaxSlots()} slots";
        }
    }

    // Button only reacts to left-clicks; this catches the right-click for dropping
    class SlotRightClick : MonoBehaviour, IPointerClickHandler
    {
        public Action onRight;

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Right) onRight?.Invoke();
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
