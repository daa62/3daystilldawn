using UnityEngine;

// Navigation for the Title and Intro scenes; buttons are wired to these methods.
public class MainMenu : MonoBehaviour
{
    [Tooltip("Instructions overlay shown on the Title scene. Optional on other scenes.")]
    [SerializeField] GameObject instructionsPanel;

    void Start()
    {
        // menu scenes are UI-only, so make sure the cursor is free and visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (instructionsPanel != null) {
            instructionsPanel.SetActive(false);
        }
    }

    public void startGame()
    {
        Sfx.play(Sfx.UI_CLICK);
        Inventory.clearCarried();   // new game starts empty-handed
        DayCycle.reset();
        SceneLoader.load(GameManager.SCENE_INTRO);
    }

    public void continueToGame()
    {
        Sfx.play(Sfx.UI_CLICK);
        CutscenePlayer.play(new[] { "intro_1", "intro_2", "intro_3" },
            new[] {
                "While you and a group of survivors are on the move, you pass through a shopping mall parking lot to avoid a blocked highway.",
                "A roaming horde attacks and the group is quickly overwhelmed. Forced to scatter, you and your friend, Samuel, flee inside the mall with a wounded soldier.",
                "Before he dies from a fatal wound, the soldier sends out an SOS and tells you that help will come in three days."
            },
            () => SceneLoader.load(GameManager.SCENE_SAFE_ROOM));
    }

    public void backToTitle()
    {
        Sfx.play(Sfx.UI_CLICK);
        SceneLoader.load(GameManager.SCENE_TITLE);
    }

    public void showInstructions()
    {
        Sfx.play(Sfx.UI_CLICK);
        if (instructionsPanel != null) {
            instructionsPanel.SetActive(true);
        }
    }

    public void hideInstructions()
    {
        Sfx.play(Sfx.UI_CLICK);
        if (instructionsPanel != null) {
            instructionsPanel.SetActive(false);
        }
    }

    public void quitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
