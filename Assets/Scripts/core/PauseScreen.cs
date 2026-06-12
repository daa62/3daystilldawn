using UnityEngine;

public class PauseScreen : MonoBehaviour
{
    private const int BUTTON_WIDTH = 220;
    private const int BUTTON_HEIGHT = 48;
    private const int GAP = 14;

    void Update()
    {
        bool pausePressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
        if (pausePressed) {
            if (GameManager.Instance.state == GameManager.GameState.Playing)
                GameManager.Instance.pauseGame();
            else if (GameManager.Instance.state == GameManager.GameState.Paused)
                GameManager.Instance.resumeGame();
        }
    }

    void OnGUI()
    {
        if (GameManager.Instance == null || GameManager.Instance.state != GameManager.GameState.Paused)
            return;

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;

        float boxW = 280f;
        float boxH = 260f;
        GUI.Box(new Rect(centerX - boxW * 0.5f, centerY - boxH * 0.5f, boxW, boxH), "");

        var titleStyle = new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.black },
            hover  = { textColor = Color.black }
        };
        GUI.Label(new Rect(centerX - boxW * 0.5f, centerY - boxH * 0.5f + 10f, boxW, 40f), "Paused", titleStyle);

        float btnX = centerX - BUTTON_WIDTH * 0.5f;
        float btnY = centerY - boxH * 0.5f + 60f;

        if (GUI.Button(new Rect(btnX, btnY, BUTTON_WIDTH, BUTTON_HEIGHT), "Resume")) {
            GameManager.Instance.resumeGame();
        }

        if (GUI.Button(new Rect(btnX, btnY + BUTTON_HEIGHT + GAP, BUTTON_WIDTH, BUTTON_HEIGHT), "Main Menu")) {
            GameManager.Instance.returnToMenu();
        }

        if (GUI.Button(new Rect(btnX, btnY + (BUTTON_HEIGHT + GAP) * 2, BUTTON_WIDTH, BUTTON_HEIGHT), "Exit")) {
            GameManager.Instance.exitGame();
        }
    }
}
