using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Self-building gameplay HUD: health/stamina bars, day counter, objective line,
// crosshair, death overlay. One per gameplay scene, no editor wiring.
public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance { get; private set; }

    const float BAR_WIDTH  = 320f;
    const float BAR_HEIGHT = 26f;

    const float STAMINA_HEIGHT = 12f;

    static readonly Color BAR_BG        = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color BAR_FILL      = new Color(0.80f, 0.20f, 0.18f, 1f);
    static readonly Color STAMINA_FILL  = new Color(0.85f, 0.75f, 0.25f, 1f);   // amber
    static readonly Color STAMINA_SPENT = new Color(0.55f, 0.30f, 0.20f, 1f);   // dim while exhausted
    static readonly Color OVERLAY       = new Color(0.05f, 0f, 0f, 0.85f);
    static readonly Color HIT_FLASH     = new Color(0.6f, 0f, 0f, 1f);
    const float HIT_FLASH_MAX  = 0.4f;   // peak opacity the instant a hit lands
    const float HIT_FLASH_FADE = 1.2f;   // opacity units drained per second

    RectTransform healthFill;
    TextMeshProUGUI healthLabel;
    TextMeshProUGUI objectiveLabel;
    TextMeshProUGUI dayLabel;
    GameObject deathOverlay;

    UnityEngine.UI.Image hitFlash;
    float hitAlpha;

    RectTransform staminaFill;
    UnityEngine.UI.Image staminaFillImage;

    Health playerHealth;
    Stamina playerStamina;

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
            playerHealth.onDamaged.AddListener(onPlayerDamaged);
            playerHealth.onDeath.AddListener(showDeath);
            updateHealth(playerHealth.Current, playerHealth.Max);
        }
        else
        {
            Debug.LogWarning("[PlayerHUD] No Health found on the player — health bar will stay full.");
        }

        if (player != null) playerStamina = player.GetComponent<Stamina>();
        if (playerStamina != null)
        {
            playerStamina.onChanged.AddListener(updateStamina);
            updateStamina(playerStamina.Current, playerStamina.Max);
        }

        DayCycle.onChanged += updateDayLabel;
        // the morning objective flips once Samuel has been talked to (a flag change)
        GameState.Instance?.onFlagChanged.AddListener(onFlagChanged);
        updateDayLabel();
    }

    void OnDestroy()
    {
        // static event — must unsubscribe
        DayCycle.onChanged -= updateDayLabel;
        GameState.Instance?.onFlagChanged.RemoveListener(onFlagChanged);
    }

    void onFlagChanged(string key, bool value)
    {
        if (key.StartsWith(GameManager.MORNING_TALKED_PREFIX)) refreshObjective();
    }

    // full-screen red kick on every hit, then it drains back to clear
    void onPlayerDamaged(float amount)
    {
        hitAlpha = HIT_FLASH_MAX;
    }

    void Update()
    {
        if (hitAlpha > 0f && hitFlash != null)
        {
            hitAlpha = Mathf.Max(0f, hitAlpha - HIT_FLASH_FADE * Time.deltaTime);
            var c = HIT_FLASH; c.a = hitAlpha;
            hitFlash.color = c;
        }
    }

    public void setObjective(string text)
    {
        if (objectiveLabel != null)
            objectiveLabel.text = string.IsNullOrEmpty(text) ? "" : "Objective:  " + text;
    }

    void updateDayLabel()
    {
        if (dayLabel != null)
            dayLabel.text = $"Day {DayCycle.CurrentDay} of {GameManager.TOTAL_DAYS}";

        refreshObjective();
    }

    // objective follows the day/night phase so the player always knows what's next
    void refreshObjective()
    {
        switch (DayCycle.CurrentPhase)
        {
            case DayCycle.Phase.Morning:
                bool talked = GameState.Instance != null &&
                    GameState.Instance.getFlag(GameManager.MORNING_TALKED_PREFIX + DayCycle.CurrentDay);
                setObjective(talked
                    ? "Head out through the door to scavenge the store"
                    : "Check in with Samuel");
                break;
            case DayCycle.Phase.Scavenging:
                setObjective("Find food, medicine, or comfort — return before nightfall");
                break;
            case DayCycle.Phase.Night:
                setObjective("Care for Samuel, then rest until morning");
                break;
        }
    }

    // bars are drawn against the absolute scale (100), not the current max —
    // lost capacity from wounds/hunger shows as bar that can never fill
    void updateHealth(float current, float max)
    {
        float ratio = Mathf.Clamp01(current / GameManager.PLAYER_MAX_HEALTH);
        if (healthFill != null)  healthFill.sizeDelta = new Vector2(BAR_WIDTH * ratio, BAR_HEIGHT);
        if (healthLabel != null) healthLabel.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
    }

    void updateStamina(float current, float max)
    {
        float ratio = Mathf.Clamp01(current / GameManager.STAMINA_MAX);
        if (staminaFill != null)
            staminaFill.sizeDelta = new Vector2(BAR_WIDTH * ratio, STAMINA_HEIGHT);
        // dim the bar while exhausted so the sprint lockout reads at a glance
        if (staminaFillImage != null)
            staminaFillImage.color = (playerStamina != null && playerStamina.IsExhausted)
                ? STAMINA_SPENT : STAMINA_FILL;
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

        // death is a narrative ending — Ending scene if it exists, overlay as fallback
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

        // stamina bar, a slim strip just above the health bar
        var stamBg = image(root, "StaminaBar", BAR_BG);
        anchor(stamBg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
        stamBg.rectTransform.anchoredPosition = new Vector2(40, 40 + BAR_HEIGHT + 6);
        stamBg.rectTransform.sizeDelta = new Vector2(BAR_WIDTH, STAMINA_HEIGHT);

        var stamFill = image(stamBg.transform, "Fill", STAMINA_FILL);
        anchor(stamFill.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        stamFill.rectTransform.anchoredPosition = Vector2.zero;
        stamFill.rectTransform.sizeDelta = new Vector2(BAR_WIDTH, STAMINA_HEIGHT);
        staminaFill = stamFill.rectTransform;
        staminaFillImage = stamFill;

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
        buildHitFlash(root);
        buildDeathOverlay(root);
    }

    void buildHitFlash(Transform root)
    {
        var flash = image(root, "HitFlash", new Color(HIT_FLASH.r, HIT_FLASH.g, HIT_FLASH.b, 0f));
        stretch(flash.rectTransform);
        flash.raycastTarget = false;   // never swallow clicks on the death button
        hitFlash = flash;
    }

    const float CROSSHAIR_SPRITE_SIZE = 48f;   // on-screen size of a custom crosshair sprite

    void buildCrosshair(Transform root)
    {
        var go = new GameObject("Crosshair", typeof(RectTransform));
        go.transform.SetParent(root, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        Color core = new Color(1f, 1f, 1f, 0.9f);

        // custom art wins when present (Resources/Ui/crosshair); the drawn dot is the fallback
        var custom = Resources.Load<Sprite>("Ui/crosshair");
        if (custom != null) {
            var img = image(go.transform, "Sprite", core);
            img.sprite = custom;
            img.preserveAspect = true;
            var srt = img.rectTransform;
            srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = Vector2.zero;
            srt.sizeDelta = new Vector2(CROSSHAIR_SPRITE_SIZE, CROSSHAIR_SPRITE_SIZE);
        } else {
            crosshairDot(go.transform, 4f, core);
        }
    }

    void crosshairDot(Transform parent, float diameter, Color color)
    {
        var dot = image(parent, "Dot", color);
        dot.sprite = circleSprite();
        var rt = dot.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(diameter, diameter);
    }

    // white circle texture built once at runtime, so no imported sprite is needed
    static Sprite cachedCircle;
    static Sprite circleSprite()
    {
        if (cachedCircle != null) return cachedCircle;

        const int res = 64;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float c = (res - 1) * 0.5f;
        float r = res * 0.5f - 1f;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(r - d + 0.5f);   // solid inside, 1px anti-aliased edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();

        cachedCircle = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        return cachedCircle;
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
