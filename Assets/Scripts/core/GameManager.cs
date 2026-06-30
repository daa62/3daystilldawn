using UnityEngine;

public class GameManager : MonoBehaviour
{
    public const float PLAYER_MOVE_SPEED      = 4f;
    public const float PLAYER_SPRINT_SPEED    = 6f;
    public const float PLAYER_SPEED_CHANGE_RATE = 10f;
    public const float PLAYER_LOOK_SENSITIVITY = 2f;
    public const float PLAYER_GRAVITY         = -15f;
    public const float PLAYER_JUMP_HEIGHT     = 1f;
    public const float PLAYER_JUMP_TIMEOUT    = 0.1f;
    public const float PLAYER_FALL_TIMEOUT    = 0.15f;
    public const float VERTICAL_CLAMP        = 89.9f;

    public const float ZOMBIE_MOVE_SPEED   = 2.5f;
    public const float ZOMBIE_SIGHT_RANGE  = 12f;
    public const float ZOMBIE_FOV          = 110f;  // total view-cone angle in degrees
    public const float ZOMBIE_TURN_SPEED   = 8f;
    public const float ZOMBIE_SIGHT_MEMORY = 3f;    // keeps chasing this long after losing sight
    public const float ZOMBIE_EYE_HEIGHT   = 1.6f;

    public const float  INTERACT_RANGE          = 3f;
    public const string INTERACTABLE_LAYER_NAME = "Interactable";

    public const int INVENTORY_MAX_SLOTS = 8;

    public const string SCENE_TITLE = "Title";
    public const string SCENE_INTRO = "Intro";
    public const string SCENE_MAIN  = "Main";

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
