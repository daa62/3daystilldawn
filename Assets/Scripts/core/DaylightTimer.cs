using UnityEngine;
using TMPro;

// Daylight budget for one in-game day, shown top-right in the store scene.
// The clock is per day, not per visit — leaving and coming back resumes it.
// At zero, night falls: extra zombies spawn and the early-return bond is forfeit.
public class DaylightTimer : MonoBehaviour
{
    public static DaylightTimer Instance { get; private set; }

    [Tooltip("Where the extra night zombies appear. Leave empty to skip spawning.")]
    [SerializeField] Transform[] nightSpawnPoints;

    [Tooltip("Zombie prefab spawned at night. If unset, an existing scene zombie is cloned.")]
    [SerializeField] GameObject zombiePrefab;

    // today's clock, static so it survives scene reloads
    static float remainingToday;
    static bool  nightFellToday;
    static int   stampedDay;

    float remaining;
    bool nightFell;
    TextMeshProUGUI label;

    public float RemainingSeconds => remaining;
    public bool NightFell => nightFell;

    // new game: forget the previous run's clock (called by DayCycle.reset)
    public static void resetClock()
    {
        stampedDay = 0;
    }

    void Awake()
    {
        Instance = this;

        // fresh budget on a new day; otherwise resume today's clock
        if (stampedDay != DayCycle.CurrentDay) {
            stampedDay      = DayCycle.CurrentDay;
            remainingToday  = GameManager.DAYLIGHT_SECONDS;
            nightFellToday  = false;
        }
        remaining = remainingToday;
        nightFell = nightFellToday;

        buildLabel();
    }

    void Start()
    {
        if (nightFell) {
            // re-entering the store after dark: night is still on
            GameState.Instance?.setFlag(GameManager.FLAG_NIGHT_FELL);
            spawnNightZombies();   // the scene reloaded, so the night pack respawns
        } else {
            GameState.Instance?.clearFlag(GameManager.FLAG_NIGHT_FELL);
        }
        Sfx.ambience(nightFell ? Sfx.AMB_STORE_NIGHT : Sfx.AMB_STORE);
        updateLabel();
    }

    void Update()
    {
        if (nightFell) return;

        remaining -= Time.deltaTime;
        remainingToday = remaining;
        if (remaining <= 0f) {
            remaining = 0f;
            fallNight();
        }

        updateLabel();
    }

    void fallNight()
    {
        nightFell = true;
        nightFellToday = true;
        GameState.Instance?.setFlag(GameManager.FLAG_NIGHT_FELL);
        Sfx.play(Sfx.NIGHT_FALL);
        Sfx.ambience(Sfx.AMB_STORE_NIGHT);
        spawnNightZombies();
    }

    void spawnNightZombies()
    {
        // a locked storefront keeps the night horde outside
        var state = GameState.Instance;
        if (state != null && state.getFlag(GameManager.FLAG_STORE_LOCKED)) return;

        ZombieSpawning.spawnAt(zombiePrefab, nightSpawnPoints,
                               GameManager.NIGHT_EXTRA_ZOMBIES, "DaylightTimer");
    }

    // ---------------------------------------------------------------- ui

    void buildLabel()
    {
        UiFactory.ensureEventSystem();
        Canvas canvas = UiFactory.overlayCanvas(transform, "DaylightCanvas");

        label = UiFactory.text(canvas.transform, "TimeLeft", "", 36, Color.white,
                               TextAlignmentOptions.TopRight);
        UiFactory.outline(label);

        var rt = label.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-40f, -30f);
        rt.sizeDelta = new Vector2(320f, 48f);
    }

    void updateLabel()
    {
        if (label == null) return;

        if (nightFell) {
            label.text = "NIGHT";
            label.color = new Color(0.55f, 0.35f, 0.9f, 1f);
            return;
        }

        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        label.text = $"{minutes}:{seconds:00}";

        // drift from white toward red over the final minute
        if (remaining < 60f)
            label.color = Color.Lerp(new Color(0.9f, 0.2f, 0.15f, 1f), Color.white, remaining / 60f);
    }
}
