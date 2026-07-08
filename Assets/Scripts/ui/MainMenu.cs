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
        SceneLoader.load(GameManager.SCENE_SAFE_ROOM);
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
