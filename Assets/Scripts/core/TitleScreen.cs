using UnityEngine;

public class TitleScreen : MonoBehaviour
{
    private const int BUTTON_WIDTH = 220;
    private const int BUTTON_HEIGHT = 48;
    private const int GAP = 14;

    private Texture2D titleBg;

    void Start()
    {
        titleBg = Resources.Load<Texture2D>("Sprites/Backgrounds/Title");
    }

    void OnGUI()
    {
        if (GameManager.Instance == null || GameManager.Instance.isPlaying()) {
            return;
        }

        if (titleBg != null) {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), titleBg, ScaleMode.ScaleToFit);
        }

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        var titleStyle = new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 42
        };

        string title = getTitle();
        if (titleBg == null || GameManager.Instance.state != GameManager.GameState.Title) {
            GUI.Label(new Rect(0, centerY - 140, Screen.width, 64), title, titleStyle);
        }

        if (GameManager.Instance.state == GameManager.GameState.Title) {
            if (GUI.Button(new Rect(centerX - BUTTON_WIDTH * 0.5f, centerY - BUTTON_HEIGHT * 0.5f, BUTTON_WIDTH, BUTTON_HEIGHT), "Game Start")) {
                GameManager.Instance.startGame();
            }
        } else {
            GUI.Label(new Rect(0, centerY - 140, Screen.width, 64), title, titleStyle);
        }

        float exitY = centerY + BUTTON_HEIGHT * 0.5f + GAP;
        if (GUI.Button(new Rect(centerX - BUTTON_WIDTH * 0.5f, exitY, BUTTON_WIDTH, BUTTON_HEIGHT), "Exit")) {
            GameManager.Instance.exitGame();
        }
    }

    private string getTitle()
    {
        if (GameManager.Instance.state == GameManager.GameState.Won) {
            return "You Win";
        }
        if (GameManager.Instance.state == GameManager.GameState.GameOver) {
            return "Game Over";
        }
        return "Antlion";
    }
}
