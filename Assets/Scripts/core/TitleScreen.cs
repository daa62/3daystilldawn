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
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.isPlaying()) {
            var hudStyle = new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.UpperLeft,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };
            GUI.Label(new Rect(12, 12, 300, 40), $"Score: {GameManager.Instance.score}", hudStyle);
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
                GameManager.Instance.selectDifficulty();
            }
        } else if (GameManager.Instance.state == GameManager.GameState.SelectingDifficulty) {
            var diffLabelStyle = new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(0, centerY - 120, Screen.width, 50), "Select Difficulty", diffLabelStyle);

            float diffBtnW = 90f;
            float totalW = diffBtnW * 3 + GAP * 2;
            float diffStartX = centerX - totalW * 0.5f;
            float diffY = centerY - BUTTON_HEIGHT * 0.5f - 20f;

            drawDiffButton("Easy",   GameManager.Difficulty.Easy,   new Rect(diffStartX,                      diffY, diffBtnW, BUTTON_HEIGHT));
            drawDiffButton("Medium", GameManager.Difficulty.Medium, new Rect(diffStartX + diffBtnW + GAP,     diffY, diffBtnW, BUTTON_HEIGHT));
            drawDiffButton("Hard",   GameManager.Difficulty.Hard,   new Rect(diffStartX + (diffBtnW + GAP)*2, diffY, diffBtnW, BUTTON_HEIGHT));

            if (GUI.Button(new Rect(centerX - BUTTON_WIDTH * 0.5f, centerY + BUTTON_HEIGHT * 0.5f + GAP, BUTTON_WIDTH, BUTTON_HEIGHT), "Confirm")) {
                GameManager.Instance.startGame();
            }
        } else {
            GUI.Label(new Rect(0, centerY - 140, Screen.width, 64), title, titleStyle);
            var scoreStyle = new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                normal = { textColor = Color.yellow }
            };
            GUI.Label(new Rect(0, centerY - 72, Screen.width, 48), $"Score: {GameManager.Instance.score}", scoreStyle);
        }

        bool onDiffScreen = GameManager.Instance.state == GameManager.GameState.SelectingDifficulty;
        float exitY = onDiffScreen
            ? centerY + BUTTON_HEIGHT * 1.5f + GAP * 2
            : centerY + BUTTON_HEIGHT * 0.5f + GAP;
        if (GUI.Button(new Rect(centerX - BUTTON_WIDTH * 0.5f, exitY, BUTTON_WIDTH, BUTTON_HEIGHT), "Exit")) {
            GameManager.Instance.exitGame();
        }
    }

    private void drawDiffButton(string label, GameManager.Difficulty diff, Rect rect)
    {
        bool selected = GameManager.Instance.selectedDifficulty == diff;
        var prevColor = GUI.backgroundColor;
        if (selected) GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUI.Button(rect, label)) GameManager.Instance.selectedDifficulty = diff;
        GUI.backgroundColor = prevColor;
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
