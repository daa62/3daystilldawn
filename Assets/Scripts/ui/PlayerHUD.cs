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
    TextMeshProUGUI dayLabel;
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

        DayCycle.onChanged += updateDayLabel;
        updateDayLabel();
    }

    void OnDestroy()
    {
        // DayCycle.onChanged is static — a destroyed HUD must let go or it leaks
        DayCycle.onChanged -= updateDayLabel;
    }

    // ---- public API (used by objectives / quests later) ----

    public void setObjective(string text)
    {
        if (objectiveLabel != null)
            objectiveLabel.text = string.IsNullOrEmpty(text) ? "" : "Objective:  " + text;
    }

    // Day counter, top-center ("Day 1 of 3 — Morning")
    void updateDayLabel()
    {
        if (dayLabel != null)
            dayLabel.text = $"Day {DayCycle.CurrentDay} of {GameManager.TOTAL_DAYS} — {DayCycle.CurrentPhase}";

        // the objective follows the phase of the loop, so it refreshes on the same signal
        refreshObjective();
    }

    // Current objective, driven by the day/night phase (spec: the player must always be
    // able to read what to do next). No explicit call site sets this otherwise.
    void refreshObjective()
    {
        switch (DayCycle.CurrentPhase)
        {
            case DayCycle.Phase.Morning:
                setObjective("Head out through the door to scavenge the store");
                break;
            case DayCycle.Phase.Scavenging:
                setObjective("Find food, medicine, or comfort — return before nightfall");
                break;
            case DayCycle.Phase.Night:
                setObjective("Care for Mia, then rest until morning");
                break;
        }
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
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.lockCursor(false);
            player.enabled = false;
        }

        if (GameState.Instance != null) GameState.Instance.setFlag(GameManager.FLAG_DIED);

        // death is a narrative ending — go to the Ending scene if it exists, else show the overlay
        if (Application.CanStreamedLevelBeLoaded(GameManager.SCENE_ENDING))
            SceneLoader.load(GameManager.SCENE_ENDING);
        else if (deathOverlay != null)
            deathOverlay.SetActive(true);
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

        // day counter, top-center
        dayLabel = text(root, "DayCounter", "", 28, Color.white, TextAlignmentOptions.Top);
        anchor(dayLabel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        dayLabel.rectTransform.anchoredPosition = new Vector2(0, -30);
        dayLabel.rectTransform.sizeDelta = new Vector2(1200, 44);
        UiFactory.outline(dayLabel);   // stays readable over bright surfaces

        // objective / story-note line, below the day counter
        objectiveLabel = text(root, "Objective", "", 24, new Color(0.95f, 0.9f, 0.7f, 1f), TextAlignmentOptions.Top);
        anchor(objectiveLabel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        objectiveLabel.rectTransform.anchoredPosition = new Vector2(0, -72);
        objectiveLabel.rectTransform.sizeDelta = new Vector2(1200, 40);
        UiFactory.outline(objectiveLabel);

        buildCrosshair(root);
        buildDeathOverlay(root);
    }

    // A small "+" reticle with a dark outline behind a bright core, so it stays visible on
    // both bright and dark backgrounds.
    void buildCrosshair(Transform root)
    {
        var go = new GameObject("Crosshair", typeof(RectTransform));
        go.transform.SetParent(root, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        Color outline = new Color(0f, 0f, 0f, 0.65f);
        Color core = new Color(1f, 1f, 1f, 0.9f);
        crosshairBar(go.transform, new Vector2(26, 6), outline);
        crosshairBar(go.transform, new Vector2(6, 26), outline);
        crosshairBar(go.transform, new Vector2(22, 2), core);
        crosshairBar(go.transform, new Vector2(2, 22), core);
    }

    void crosshairBar(Transform parent, Vector2 size, Color color)
    {
        var bar = image(parent, "Bar", color);
        var rt = bar.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
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
