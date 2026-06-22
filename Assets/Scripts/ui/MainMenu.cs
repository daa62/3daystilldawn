using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void startGame()
    {
        SceneLoader.load(GameManager.SCENE_MAIN);
    }

    public void quitGame()
    {
        Application.Quit();
    }
}
