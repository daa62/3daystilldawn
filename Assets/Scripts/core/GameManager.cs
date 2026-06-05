using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Title,
        Playing,
        Won,
        GameOver
    }

    public const int MAP_WIDTH = 20;
    public const int MAP_HEIGHT = 100;
    public const float CELL_SIZE = 1f;

    public const float GAME_TICK_TIME = 0.1f;
    public const float MOVE_INTERPOLATION_TIME = GAME_TICK_TIME;

    public const float ENTITY_MOVE_SPEED = 12f;
    public const float PLAYER_MOVE_COOLDOWN = 0.1f;

    public const float CAMERA_PADDING = 0.5f;
    public const float CAMERA_FOLLOW_SPEED = 8f;
    public const float CAMERA_Z = -10f;
    public static readonly bool CAMERA_SHOW_FULL_MAP = false;

    public const float GRID_LINE_WIDTH = 0.04f;
    public static readonly Color GRID_LINE_COLOR = new Color(0.4f, 0.4f, 0.4f, 1f);

    public const string BACKGROUND_SPRITE = "Sprites/Backgrounds/Stage";
    public const int BACKGROUND_SORTING_ORDER = -20;
    public const float BACKGROUND_Z = 0.5f;

    public const string PLAYER_SPRITE = "Sprites/Entities/Player";
    public const string ANTLION_SPRITE = "Sprites/Entities/Antlion";
    public const string FALLBACK_PLAYER_SPRITE = "Sprites/Player";

    public const float ANTLION_SPEED = 1.6f;
    public const float ANTLION_CATCH_UP_SPEED = 6f;
    public const float ANTLION_X_ALIGN_TIME = 0.15f;
    public const float ANTLION_START_Y_OFFSET = 14f;
    public const float ANTLION_MAX_Y_DISTANCE = 14f;

    public const string WHITE_TILE_SPRITE = "Sprites/Tiles/White";
    public const string WALL_TILE_SPRITE = "Sprites/Tiles/Blue";
    public const string TRAP_TILE_SPRITE = "Sprites/Tiles/Red";

    public const int DEMO_TRAP_TILE_INTERVAL = 13;

    public static GameManager Instance { get; private set; }

    public BaseMap currentMap;
    public GameState state = GameState.Title;

    void Awake()
    {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        BaseMap map = currentMap != null ? currentMap : FindAnyObjectByType<BaseMap>();
        if (map != null) {
            loadMap(map);
        }
    }

    public void loadMap(BaseMap map)
    {
        if (map == null) {
            return;
        }

        if (currentMap != null && currentMap != map) {
            currentMap.unload();
        }

        currentMap = map;
        currentMap.load();
    }

    public void startGame()
    {
        state = GameState.Playing;
    }

    public void winGame()
    {
        state = GameState.Won;
    }

    public void gameOver()
    {
        state = GameState.GameOver;
    }

    public void exitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public bool isPlaying()
    {
        return state == GameState.Playing;
    }
}
