using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Title,
        SelectingDifficulty,
        Playing,
        Won,
        GameOver
    }

    public enum Difficulty { Easy, Medium, Hard }
    public Difficulty selectedDifficulty = Difficulty.Medium;

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

    public static float ANTLION_SPEED = 1.5f;
    public static float ANTLION_CATCH_UP_SPEED = 2.5f;
    public const float ANTLION_X_ALIGN_TIME = 0.15f;
    public const float ANTLION_START_Y_OFFSET = 14f;
    public const float ANTLION_MAX_Y_DISTANCE = 14f;

    public const string WHITE_TILE_SPRITE = "Sprites/Tiles/White";
    public const string WALL_TILE_SPRITE = "Sprites/Tiles/Blue";
    public const string SLOW_TILE_SPRITE = "Sprites/Tiles/Slow";
    public const string SLIDE_TILE_SPRITE = "Sprites/Tiles/Slide";

    public const string HAZARD_SPRITE = "Sprites/Entities/Hazard";
    public const float HAZARD_MOVE_INTERVAL = 0.5f;
    public const float HAZARD_STUN_DURATION = 1f;
    public static int HAZARD_COUNT = 20;

    public const string COLLECTIBLE_SPRITE = "Sprites/Entities/Collectible";
    public static int COLLECTIBLE_COUNT = 15;
    public const int COLLECTIBLE_SCORE_VALUE = 100;

    public const float SLOW_TILE_DURATION = 3f;
    public const float SLOW_TILE_SPEED_MULTIPLIER = 0.25f;
    public const int SLIDE_TILE_CELLS = 3;
    public const float SLIDE_TILE_DURATION = 1f;
    public const float SLIDE_CELL_INTERVAL = SLIDE_TILE_DURATION / SLIDE_TILE_CELLS;

    public const int MAP_SAFE_ROWS = 5;
    public static float SLOW_TILE_SPAWN_CHANCE = 0.025f;
    public static float SLIDE_TILE_SPAWN_CHANCE = 0.025f;
    public static float WALL_TILE_SPAWN_CHANCE = 0.10f;

    public static GameManager Instance { get; private set; }

    public BaseMap currentMap;
    public GameState state = GameState.Title;
    public int score = 0;

    public void addScore(int amount)
    {
        score += amount;
    }

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
        score = 0;
        applyDifficulty();
        state = GameState.Playing;
    }

    private void applyDifficulty()
    {
        switch (selectedDifficulty) {
            case Difficulty.Easy:
                ANTLION_SPEED = 0.8f;
                ANTLION_CATCH_UP_SPEED = 1.5f;
                HAZARD_COUNT = 8;
                COLLECTIBLE_COUNT = 10;
                SLOW_TILE_SPAWN_CHANCE = 0.01f;
                SLIDE_TILE_SPAWN_CHANCE = 0.01f;
                WALL_TILE_SPAWN_CHANCE = 0.05f;
                break;
            case Difficulty.Medium:
                ANTLION_SPEED = 1.5f;
                ANTLION_CATCH_UP_SPEED = 2.5f;
                HAZARD_COUNT = 20;
                COLLECTIBLE_COUNT = 15;
                SLOW_TILE_SPAWN_CHANCE = 0.025f;
                SLIDE_TILE_SPAWN_CHANCE = 0.025f;
                WALL_TILE_SPAWN_CHANCE = 0.10f;
                break;
            case Difficulty.Hard:
                ANTLION_SPEED = 2.5f;
                ANTLION_CATCH_UP_SPEED = 4.0f;
                HAZARD_COUNT = 35;
                COLLECTIBLE_COUNT = 20;
                SLOW_TILE_SPAWN_CHANCE = 0.05f;
                SLIDE_TILE_SPAWN_CHANCE = 0.05f;
                WALL_TILE_SPAWN_CHANCE = 0.15f;
                break;
        }
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

    public void selectDifficulty()
    {
        state = GameState.SelectingDifficulty;
    }

    public bool isPlaying()
    {
        return state == GameState.Playing;
    }
}
