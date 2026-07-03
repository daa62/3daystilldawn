using UnityEngine;

public class GameManager : MonoBehaviour
{
    // --- Player body (Minecraft: 1.8 tall, eye at 1.62, width 0.6) ---
    public const float PLAYER_HEIGHT     = 1.8f;
    public const float PLAYER_EYE_HEIGHT = 1.62f;
    public const float PLAYER_RADIUS     = 0.3f;

    public const float PLAYER_LOOK_SENSITIVITY = 2f;
    public const float VERTICAL_CLAMP          = 89.9f;

    // --- Minecraft Java movement, simulated at 20 TPS (1 unit = 1 block) ---
    // Emergent result: sprint-jumping (bhop) is faster than plain sprinting because the
    // jump adds a forward impulse and air drag (0.91) is weaker than ground friction (0.546).
    public const float MC_TICK                = 0.05f;   // 20 ticks per second
    public const float MC_GROUND_SLIPPERINESS = 0.6f;    // default block
    public const float MC_AIR_DRAG            = 0.91f;
    public const float MC_GRAVITY             = 0.08f;   // blocks per tick^2
    public const float MC_Y_DRAG              = 0.98f;
    public const float MC_JUMP_VELOCITY       = 0.42f;   // -> ~1.2522 block apex
    public const float MC_WALK_ACCEL          = 0.1f;
    public const float MC_AIR_ACCEL           = 0.02f;
    public const float MC_SPRINT_MULTIPLIER   = 1.3f;
    public const float MC_SPRINT_JUMP_BOOST   = 0.2f;    // forward impulse per sprint-jump

    // gravity for simple non-player entities (zombie / npc), continuous m/s^2
    public const float PLAYER_GRAVITY = -32f;

    public const float ZOMBIE_MOVE_SPEED   = 2.5f;
    public const float ZOMBIE_SIGHT_RANGE  = 12f;
    public const float ZOMBIE_FOV          = 110f;  // total view-cone angle in degrees
    public const float ZOMBIE_TURN_SPEED   = 8f;
    public const float ZOMBIE_SIGHT_MEMORY = 3f;    // keeps chasing this long after losing sight
    public const float ZOMBIE_EYE_HEIGHT   = 1.6f;
    public const float ZOMBIE_HEARING_RANGE = 5f;   // notices the player from any direction this close
    public const float ZOMBIE_ATTACK_DAMAGE   = 12f;
    public const float ZOMBIE_ATTACK_RANGE    = 1.8f;
    public const float ZOMBIE_ATTACK_COOLDOWN = 1f;

    public const float PLAYER_MAX_HEALTH = 100f;

    public const float  INTERACT_RANGE          = 3f;
    public const string INTERACTABLE_LAYER_NAME = "Interactable";

    public const int INVENTORY_MAX_SLOTS = 8;

    public const string SCENE_TITLE     = "Title";
    public const string SCENE_INTRO     = "Intro";
    public const string SCENE_MAIN      = "GameScene";
    public const string SCENE_SAFE_ROOM = "SafeRoom";
    public const string SCENE_ENDING    = "Ending";

    // daylight timer (see spec: 5-minute scavenging budget, tuned against store size)
    public const float DAYLIGHT_SECONDS      = 300f;
    public const int   NIGHT_EXTRA_ZOMBIES   = 4;    // spawned when the timer runs out
    public const int   ZOMBIES_PER_EXTRA_DAY = 2;    // day escalation: +2 on day 2, +4 on day 3
    public const int   BOND_PER_EARLY_MINUTE = 2;    // early-return bond bump per full minute left

    // day cycle + friend variables — spec starter numbers, all tunable here
    public const int TOTAL_DAYS          = 3;
    public const int FRIEND_HEALTH_START = 70;   // already bitten
    public const int FRIEND_BOND_START   = 20;   // the secret is creating distance
    public const int FRIEND_HEALTH_DECAY = 25;   // per night, applied automatically
    public const int HEALTH_LINE         = 40;   // below this at the climax: TURNS
    public const int BOND_LINE           = 50;   // below this (health held): SLIPS_AWAY
    public const int BOND_TALK_AT_NIGHT  = 5;
    public const int FRIEND_STAT_MAX     = 100;  // both axes live on a hidden 0-100 scale

    // night actions (spec: food ~+15 health, meds ~+20, comfort item ~+15 bond)
    public const int FRIEND_HEALTH_FOOD     = 15;
    public const int FRIEND_HEALTH_MEDICINE = 20;
    public const int BOND_COMFORT_ITEM      = 15;

    // narrative state keys (GameState flags/counters) — shared by dialogue, objectives, endings
    public const string FLAG_NIGHT_FELL     = "night_fell";
    public const string FLAG_FRIEND_MET     = "friend_met";
    public const string FLAG_FRIEND_RESTING = "friend_resting";
    public const string FLAG_REASSURED      = "reassured_friend";
    public const string FLAG_DIED           = "player_died";
    public const string FLAG_CARED_OVERNIGHT = "cared_overnight";      // fed/medicated last night; drives the next morning's scene
    public const string MORNING_TALKED_PREFIX = "morning_talked_day";  // + day number; first morning visit plays the full scene
    public const string COUNTER_BOND          = "friend_bond";
    public const string COUNTER_FRIEND_HEALTH = "friend_health";
    public const string COUNTER_LAST_RUN_BOND = "last_run_bond";   // banked on today's early return

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
