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

    public const string SCENE_TITLE = "Title";
    public const string SCENE_INTRO = "Intro";
    public const string SCENE_MAIN  = "GameScene";

    // narrative state keys (GameState flags/counters) — shared by dialogue, objectives, endings
    public const string FLAG_FRIEND_MET     = "friend_met";
    public const string FLAG_FRIEND_RESTING = "friend_resting";
    public const string FLAG_REASSURED      = "reassured_friend";
    public const string COUNTER_BOND     = "friend_bond";
    public const string COUNTER_SUPPLIES = "supplies";
    public const int    SUPPLIES_GOAL    = 3;

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
