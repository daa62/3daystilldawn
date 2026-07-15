using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Pickup feed on the right edge, under the daylight timer: "+1 Bandage" lines
// that stack newest-first and fade out. Rapid repeats of the same item merge
// into one "+n" line. Builds itself on first use — no scene wiring.
public class PickupFeed : MonoBehaviour
{
    const float HOLD_SECONDS = 2f;
    const float FADE_SECONDS = 0.6f;
    const float MERGE_WINDOW = 1.5f;
    const float LINE_HEIGHT  = 32f;
    const int   MAX_ENTRIES  = 5;

    static PickupFeed instance;

    class Entry
    {
        public TextMeshProUGUI label;
        public CanvasGroup group;
        public string itemName;   // null for plain notices
        public int count;
        public float age;
    }

    readonly List<Entry> entries = new List<Entry>();
    RectTransform stack;

    public static void push(string itemName)
    {
        ensure().add(itemName, $"+1 {itemName}", Color.white);
    }

    public static void notice(string message)
    {
        ensure().add(null, message, new Color(1f, 0.8f, 0.35f, 1f));
    }

    static PickupFeed ensure()
    {
        if (instance == null)
            instance = new GameObject("PickupFeed").AddComponent<PickupFeed>();
        return instance;
    }

    void Awake()
    {
        instance = this;
        UiFactory.ensureEventSystem();
        Canvas canvas = UiFactory.overlayCanvas(transform, "PickupFeedCanvas");

        var go = new GameObject("Stack", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        stack = (RectTransform)go.transform;
        UiFactory.anchor(stack, Vector2.one, Vector2.one, Vector2.one);
        stack.anchoredPosition = new Vector2(-40f, -90f);   // just below the daylight timer
        stack.sizeDelta = new Vector2(360f, MAX_ENTRIES * LINE_HEIGHT);
    }

    void add(string itemName, string text, Color color)
    {
        // repeat pickups of the same item bump the newest line to "+n"
        if (itemName != null && entries.Count > 0) {
            Entry newest = entries[0];
            if (newest.itemName == itemName && newest.age < MERGE_WINDOW) {
                newest.count++;
                newest.age = 0f;
                newest.group.alpha = 1f;
                newest.label.text = $"+{newest.count} {itemName}";
                return;
            }
        }

        var label = UiFactory.text(stack, "Entry", text, 26f, color,
                                   TextAlignmentOptions.TopRight);
        UiFactory.outline(label);
        var rt = label.rectTransform;
        UiFactory.anchor(rt, Vector2.one, Vector2.one, Vector2.one);
        rt.sizeDelta = new Vector2(360f, LINE_HEIGHT);

        var group = label.gameObject.AddComponent<CanvasGroup>();
        entries.Insert(0, new Entry { label = label, group = group,
                                      itemName = itemName, count = 1 });

        while (entries.Count > MAX_ENTRIES) {
            Destroy(entries[entries.Count - 1].label.gameObject);
            entries.RemoveAt(entries.Count - 1);
        }
        layout();
    }

    void Update()
    {
        bool removed = false;
        for (int i = entries.Count - 1; i >= 0; i--) {
            Entry e = entries[i];
            e.age += Time.deltaTime;
            if (e.age <= HOLD_SECONDS) continue;

            float fade = 1f - (e.age - HOLD_SECONDS) / FADE_SECONDS;
            if (fade <= 0f) {
                Destroy(e.label.gameObject);
                entries.RemoveAt(i);
                removed = true;
            } else {
                e.group.alpha = fade;
            }
        }
        if (removed) layout();
    }

    void layout()
    {
        for (int i = 0; i < entries.Count; i++)
            entries[i].label.rectTransform.anchoredPosition = new Vector2(0f, -i * LINE_HEIGHT);
    }
}
