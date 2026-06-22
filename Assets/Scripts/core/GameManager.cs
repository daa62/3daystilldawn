using UnityEngine;

public class GameManager : MonoBehaviour
{
    public const float PLAYER_MOVE_SPEED      = 4f;
    public const float PLAYER_LOOK_SENSITIVITY = 2f;
    public const float PLAYER_GRAVITY         = -15f;
    public const float PLAYER_JUMP_HEIGHT     = 1f;
    public const float VERTICAL_CLAMP        = 89.9f;

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
