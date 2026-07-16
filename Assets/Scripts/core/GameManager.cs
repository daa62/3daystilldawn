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
    // sprint-jumping ends up faster than plain sprinting, same as in mc
    public const float MC_TICK                = 0.05f;   // 20 ticks per second
    public const float MC_GROUND_SLIPPERINESS = 0.6f;    // default block
    public const float MC_AIR_DRAG            = 0.91f;
    public const float MC_GRAVITY             = 0.08f;   // blocks per tick^2
    public const float MC_Y_DRAG              = 0.98f;
    public const float MC_JUMP_VELOCITY       = 0.42f;   // -> ~1.2522 block apex
    public const float MC_WALK_ACCEL          = 0.13f;
    public const float MC_AIR_ACCEL           = 0.02f;
    public const float MC_SPRINT_MULTIPLIER   = 2.2f;
    public const float MC_SPRINT_JUMP_BOOST   = 0.2f;    // forward impulse per sprint-jump

    // --- crouch (hold Left Ctrl): slow, silent, camera lowered ---
    public const float MC_CROUCH_MULTIPLIER = 0.5f;  // crouch speed relative to walking
    public const float CROUCH_CAMERA_DROP   = 0.6f;   // metres the eye lowers while crouched
    public const float CROUCH_LERP_SPEED    = 5f;    // how fast the camera eases down/up

    // --- stamina (drains on sprint + jump; hard-lockout exhaustion) ---
    public const float STAMINA_MAX               = 100f;
    public const float STAMINA_SPRINT_DRAIN      = 25f;   // per second sprinting -> ~4s of sprint
    public const float STAMINA_JUMP_COST         = 15f;   // per jump
    public const float STAMINA_REGEN             = 18f;   // per second once regen kicks in
    public const float STAMINA_REGEN_DELAY       = 0.6f;  // seconds after last use before regen starts
    public const float STAMINA_RECOVER_THRESHOLD = 30f;   // exhausted -> can't sprint until stamina climbs back to this

    // --- camera feel (CameraEffects) ---
    public const float SPRINT_FOV_KICK   = 6f;    // degrees added to base FOV while sprinting
    public const float FOV_LERP_SPEED    = 8f;     // how fast FOV eases toward its target
    public const float HEADBOB_FREQUENCY = 2.5f;   // bob cycles scale with speed * this
    public const float HEADBOB_AMPLITUDE = 0.03f; // vertical bob height at walking speed (metres)
    public const float HEADBOB_SPRINT_MULT = 1.3f; // amplitude/pace boost at full sprint speed
    public const float HEADBOB_MIN_SPEED = 0.5f;   // below this speed (units/s) the bob rests

    // gravity for simple non-player entities (zombie / npc), continuous m/s^2
    public const float PLAYER_GRAVITY = -32f;

    public const float ZOMBIE_MOVE_SPEED   = 1.5f;
    public const float ZOMBIE_SIGHT_RANGE  = 12f;
    public const float ZOMBIE_FOV          = 110f;  // total view-cone angle in degrees
    public const float ZOMBIE_TURN_SPEED   = 8f;
    public const float ZOMBIE_SIGHT_MEMORY = 3f;    // keeps chasing this long after losing sight
    public const float ZOMBIE_EYE_HEIGHT   = 1.6f;
    public const float ZOMBIE_HEARING_RANGE = 5f;   // notices the player from any direction this close
    public const float ZOMBIE_INVESTIGATE_SPEED  = 1.5f;   // slower shamble toward a heard noise
    public const float ZOMBIE_INVESTIGATE_LINGER = 2.5f;   // seconds spent at the noise spot before idling

    // noise emission (Noise bus): sprint is loud, walking is quiet, crouching is silent
    public const float NOISE_SPRINT_RADIUS    = 30f;    // how far sprint footsteps carry
    public const float NOISE_WALK_RADIUS      = 8f;     // walking footsteps carry about one aisle
    public const float NOISE_FOOTSTEP_INTERVAL        = 0.4f;   // seconds between footsteps while walking
    public const float NOISE_SPRINT_FOOTSTEP_INTERVAL = 0.2f;  // faster cadence while sprinting
    public const float ZOMBIE_ATTACK_DAMAGE   = 12f;
    public const float ZOMBIE_ATTACK_RANGE    = 1.8f;
    public const float ZOMBIE_ATTACK_COOLDOWN = 1f;

    public const float PLAYER_MAX_HEALTH = 100f;

    // --- player condition (persistent across scenes; see PlayerCondition) ---
    public const float STAMINA_START_MAX   = 60f;    // starting stamina capacity (x% of the bar)
    public const float PLAYER_HEALTH_REGEN = 1.5f;   // HP per second, up to the current max
    public const float PLAYER_MIN_MAX_HEALTH   = 20f;   // wounds can't take max HP below this
    public const float ZOMBIE_WOUND_MAX_HP     = 5f;    // lasting max-HP loss per zombie hit
    public const float STAMINA_MIN_MAX         = 10f;   // hunger can't take stamina capacity below this
    public const float HUNGER_STAMINA_DECAY    = 40f;   // stamina capacity lost per night
    public const float FOOD_STAMINA_RESTORE    = 25f;   // eating food: stamina capacity back
    public const float MEDICINE_MAX_HP_RESTORE = 25f;   // using meds: max HP back

    public const float  INTERACT_RANGE          = 5f;
    public const string INTERACTABLE_LAYER_NAME = "Interactable";

    public const int INVENTORY_MAX_SLOTS = 10;

    public const string SCENE_TITLE     = "Title";
    public const string SCENE_INTRO     = "Intro";
    public const string SCENE_MAIN      = "GameScene";
    public const string SCENE_SAFE_ROOM = "SafeRoom";
    public const string SCENE_ENDING    = "Ending";

    // daylight timer: 5-minute scavenging budget
    public const float DAYLIGHT_SECONDS      = 300f;
    public const int   NIGHT_EXTRA_ZOMBIES   = 4;    // spawned when the timer runs out
    public const int   ZOMBIES_PER_EXTRA_DAY = 2;    // day escalation: +2 on day 2, +4 on day 3
    public const int   BOND_PER_EARLY_MINUTE = 2;    // early-return bond bump per full minute left

    // day cycle + friend tuning
    public const int TOTAL_DAYS          = 3;
    public const int FRIEND_HEALTH_START = 75;   // already bitten
    public const int FRIEND_BOND_START   = 20;   // the secret is creating distance
    public const int FRIEND_HEALTH_DECAY = 30;   // per night, applied automatically
    public const int HEALTH_LINE         = 40;   // below this at the climax: TURNS
    public const int BOND_LINE           = 50;   // below this (health held): SLIPS_AWAY
    public const int BOND_TALK_AT_NIGHT  = 5;
    public const int FRIEND_STAT_MAX     = 100;  // both axes live on a hidden 0-100 scale

    // night actions
    public const int FRIEND_HEALTH_FOOD     = 15;
    public const int FRIEND_HEALTH_MEDICINE = 20;
    public const int BOND_COMFORT_ITEM      = 15;

    // GameState flag/counter keys, shared by dialogue, objectives, endings
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
