using UnityEngine;

// Drives navigation for the Title and Intro menu scenes.
// Button onClick events are wired to these methods (see Editor/MenuSceneBuilder).
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

    // Title "Start Game": step into the introduction before the main scene.
    public void startGame()
    {
        SceneLoader.load(GameManager.SCENE_INTRO);
    }

    // Intro "Continue": drop the player into the main gameplay scene.
    public void continueToGame()
    {
        SceneLoader.load(GameManager.SCENE_MAIN);
    }

    // Any "Back" button that returns to the Title scene.
    public void backToTitle()
    {
        SceneLoader.load(GameManager.SCENE_TITLE);
    }

    public void showInstructions()
    {
        if (instructionsPanel != null) {
            instructionsPanel.SetActive(true);
        }
    }

    public void hideInstructions()
    {
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
