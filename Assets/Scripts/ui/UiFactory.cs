using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Helpers for building uGUI/TMP elements from code, so gameplay UI needs no editor wiring.
public static class UiFactory
{
    public static void ensureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    public static Canvas overlayCanvas(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static Image image(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    public static TextMeshProUGUI text(Transform parent, string name, string value, float size,
                                       Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = value;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        return tmp;
    }

    public static Button button(Transform parent, string name, string label, float fontSize = 28f)
    {
        var go = new GameObject(name, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.16f, 0.18f, 0.22f, 1f);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var lbl = text(go.transform, "Label", label, fontSize, Color.white, TextAlignmentOptions.Center);
        stretch(lbl.rectTransform);
        return btn;
    }

    // dark outline so white text stays readable on bright surfaces.
    // fontMaterial is an instance, so other text on the same font is unaffected
    public static void outline(TextMeshProUGUI label, float width = 0.2f)
    {
        if (label == null) return;
        label.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
        label.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
    }

    public static void anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
    }

    public static void stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
