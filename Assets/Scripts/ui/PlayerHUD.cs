using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Self-building gameplay HUD: health bar, current-objective line, and a death overlay.
// It creates its own Canvas in code, so nothing has to be wired in the editor — just
// have one PlayerHUD in the gameplay scene (Tools > M2 > Setup Survival adds it).
public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance { get; private set; }

    const float BAR_WIDTH  = 320f;
    const float BAR_HEIGHT = 26f;

    static readonly Color BAR_BG   = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color BAR_FILL = new Color(0.80f, 0.20f, 0.18f, 1f);
    static readonly Color OVERLAY  = new Color(0.05f, 0f, 0f, 0.85f);

    RectTransform healthFill;
    TextMeshProUGUI healthLabel;
    TextMeshProUGUI objectiveLabel;
    GameObject deathOverlay;

    Health playerHealth;

    void Awake()
    {
        Instance = this;
        build();
    }

    void Start()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) playerHealth = player.GetComponent<Health>();

        if (playerHealth != null)
        {
            playerHealth.onHealthChanged.AddListener(updateHealth);
            playerHealth.onDeath.AddListener(showDeath);
            updateHealth(playerHealth.Current, playerHealth.Max);
        }
        else
        {
            Debug.LogWarning("[PlayerHUD] No Health found on the player — health bar will stay full. " +
                             "Run Tools > M2 > Setup Survival.");
        }
    }

    // ---- public API (used by objectives / quests later) ----

    public void setObjective(string text)
    {
        if (objectiveLabel != null)
            objectiveLabel.text = string.IsNullOrEmpty(text) ? "" : "Objective:  " + text;
    }

    // ---- reactions ----

    void updateHealth(float current, float max)
    {
        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        if (healthFill != null)  healthFill.sizeDelta = new Vector2(BAR_WIDTH * ratio, BAR_HEIGHT);
        if (healthLabel != null) healthLabel.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
    }

    void showDeath()
    {
        if (deathOverlay != null) deathOverlay.SetActive(true);

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.lockCursor(false);
            player.enabled = false;
        }
    }

    // ---------------------------------------------------------------- build

    void build()
    {
        ensureEventSystem();

        var canvasGO = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGO.transform;

        // health bar, bottom-left
        var barBg = image(root, "HealthBar", BAR_BG);
        anchor(barBg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
        barBg.rectTransform.anchoredPosition = new Vector2(40, 40);
        barBg.rectTransform.sizeDelta = new Vector2(BAR_WIDTH, BAR_HEIGHT);

        var fill = image(barBg.transform, "Fill", BAR_FILL);
        anchor(fill.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        fill.rectTransform.anchoredPosition = Vector2.zero;
        fill.rectTransform.sizeDelta = new Vector2(BAR_WIDTH, BAR_HEIGHT);
        healthFill = fill.rectTransform;

        healthLabel = text(barBg.transform, "HealthLabel", "100 / 100", 20, Color.white, TextAlignmentOptions.Center);
        stretch(healthLabel.rectTransform);

        // objective line, top-center
        objectiveLabel = text(root, "Objective", "", 28, Color.white, TextAlignmentOptions.Top);
        anchor(objectiveLabel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        objectiveLabel.rectTransform.anchoredPosition = new Vector2(0, -30);
        objectiveLabel.rectTransform.sizeDelta = new Vector2(1200, 44);

        buildDeathOverlay(root);
    }

    void buildDeathOverlay(Transform root)
    {
        var overlay = image(root, "DeathOverlay", OVERLAY);
        stretch(overlay.rectTransform);
        deathOverlay = overlay.gameObject;

        var title = text(overlay.transform, "Title", "YOU DIED", 96,
                         new Color(0.85f, 0.2f, 0.18f, 1f), TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        title.rectTransform.anchoredPosition = new Vector2(0, 80);
        title.rectTransform.sizeDelta = new Vector2(1200, 140);

        var btn = button(overlay.transform, "TitleButton", "Return to Title");
        anchor(btn.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        btn.image.rectTransform.anchoredPosition = new Vector2(0, -60);
        btn.image.rectTransform.sizeDelta = new Vector2(360, 72);
        btn.onClick.AddListener(() => SceneLoader.load(GameManager.SCENE_TITLE));

        deathOverlay.SetActive(false);
    }

    // ---------------------------------------------------------------- ui helpers

    static void ensureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    static Image image(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI text(Transform parent, string name, string value, float size,
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

    static Button button(Transform parent, string name, string label)
    {
        var go = new GameObject(name, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.16f, 0.18f, 0.22f, 1f);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var lbl = text(go.transform, "Label", label, 30, Color.white, TextAlignmentOptions.Center);
        stretch(lbl.rectTransform);
        return btn;
    }

    static void anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
    }

    static void stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
