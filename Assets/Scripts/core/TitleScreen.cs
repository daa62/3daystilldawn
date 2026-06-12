using System.Collections.Generic;
using UnityEngine;

public class TitleScreen : MonoBehaviour
{
    private Texture2D mainBg, levelBg;
    private Texture2D texStartMain, texExitMain;
    private Texture2D texStartLevel, texExitLevel;
    private Texture2D texEasy, texMedium, texHard;

    private readonly Dictionary<Texture2D, GUIStyle> styleCache = new();

    void Start()
    {
        mainBg        = Resources.Load<Texture2D>("UI/main page - background");
        levelBg       = Resources.Load<Texture2D>("UI/level choice - background");
        texStartMain  = Resources.Load<Texture2D>("UI/start - main page");
        texExitMain   = Resources.Load<Texture2D>("UI/exit - main page");
        texStartLevel = Resources.Load<Texture2D>("UI/start - level page");
        texExitLevel  = Resources.Load<Texture2D>("UI/exit - level page");
        texEasy       = Resources.Load<Texture2D>("UI/easy");
        texMedium     = Resources.Load<Texture2D>("UI/medium");
        texHard       = Resources.Load<Texture2D>("UI/hard");
    }

    void OnGUI()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.isPlaying()) {
            drawHUD();
            return;
        }

        switch (GameManager.Instance.state) {
            case GameManager.GameState.Title:
                drawTitleScreen();
                break;
            case GameManager.GameState.SelectingDifficulty:
                drawDifficultyScreen();
                break;
            default:
                drawEndScreen();
                break;
        }
    }

    private void drawHUD()
    {
        var style = new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.UpperLeft,
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.black }
        };
        style.hover.textColor = Color.black;
        GUI.Label(new Rect(12, 12, 300, 40), $"Score: {GameManager.Instance.score}", style);
    }

    private void drawTitleScreen()
    {
        float w = Screen.width, h = Screen.height;

        if (mainBg != null)
            GUI.DrawTexture(new Rect(0, 0, w, h), mainBg, ScaleMode.StretchToFill);

        float btnW = w * 0.25f;
        float btnX = w * 0.08f;
        Rect startR = texRect(btnX, h * 0.63f, btnW, texStartMain);
        Rect exitR  = texRect(btnX, startR.yMax + h * 0.04f, btnW, texExitMain);

        if (imgButton(startR, texStartMain))
            GameManager.Instance.selectDifficulty();

        if (imgButton(exitR, texExitMain))
            GameManager.Instance.exitGame();
    }

    private void drawDifficultyScreen()
    {
        float w = Screen.width, h = Screen.height;

        if (levelBg != null)
            GUI.DrawTexture(new Rect(0, 0, w, h), levelBg, ScaleMode.StretchToFill);

        float diffW = w * 0.185f;
        float diffGap = w * 0.02f;
        float totalDiff = diffW * 3 + diffGap * 2;
        float diffX = (w - totalDiff) * 0.5f;
        float diffY = h * 0.42f;

        Rect easyR   = texRect(diffX,                       diffY, diffW, texEasy);
        Rect mediumR = texRect(diffX + diffW + diffGap,     diffY, diffW, texMedium);
        Rect hardR   = texRect(diffX + (diffW + diffGap)*2, diffY, diffW, texHard);

        drawDiffBtn(easyR,   texEasy,   GameManager.Difficulty.Easy);
        drawDiffBtn(mediumR, texMedium, GameManager.Difficulty.Medium);
        drawDiffBtn(hardR,   texHard,   GameManager.Difficulty.Hard);

        float bw   = w * 0.17f;
        float bGap = w * 0.025f;
        float bx   = (w - bw * 2 - bGap) * 0.5f;
        float by   = easyR.yMax + h * 0.10f;

        Rect startLR = texRect(bx,            by, bw, texStartLevel);
        Rect exitLR  = texRect(bx + bw + bGap, by, bw, texExitLevel);

        if (imgButton(startLR, texStartLevel))
            GameManager.Instance.startGame();

        if (imgButton(exitLR, texExitLevel))
            GameManager.Instance.state = GameManager.GameState.Title;
    }

    private void drawEndScreen()
    {
        float w = Screen.width, h = Screen.height;

        if (mainBg != null)
            GUI.DrawTexture(new Rect(0, 0, w, h), mainBg, ScaleMode.StretchToFill);

        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        bool won = GameManager.Instance.state == GameManager.GameState.Won;

        var titleStyle = new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(h * 0.07f),
            fontStyle = FontStyle.Bold,
            normal = { textColor = won ? new Color(0.9f, 0.5f, 0.1f) : Color.white }
        };
        titleStyle.hover.textColor = titleStyle.normal.textColor;
        GUI.Label(new Rect(0, h * 0.25f, w, h * 0.12f), won ? "You Win!" : "Game Over", titleStyle);

        var scoreStyle = new GUIStyle(GUI.skin.label) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(h * 0.045f),
            normal = { textColor = Color.white }
        };
        scoreStyle.hover.textColor = Color.white;
        GUI.Label(new Rect(0, h * 0.40f, w, h * 0.08f), $"Score: {GameManager.Instance.score}", scoreStyle);

        if (won) {
            int mins = Mathf.FloorToInt(GameManager.Instance.completionTime / 60f);
            int secs = Mathf.FloorToInt(GameManager.Instance.completionTime % 60f);
            var timeStyle = new GUIStyle(scoreStyle) { fontSize = Mathf.RoundToInt(h * 0.032f) };
            GUI.Label(new Rect(0, h * 0.50f, w, h * 0.06f),
                $"Time: {mins:00}:{secs:00}  |  x{GameManager.Instance.scoreMultiplier:F1}", timeStyle);
        }

        float btnW = w * 0.22f;
        float btnX = (w - btnW) * 0.5f;
        Rect startR = texRect(btnX, h * 0.64f, btnW, texStartMain);
        Rect exitR  = texRect(btnX, startR.yMax + h * 0.03f, btnW, texExitMain);

        if (imgButton(startR, texStartMain))
            GameManager.Instance.selectDifficulty();

        if (imgButton(exitR, texExitMain))
            GameManager.Instance.exitGame();
    }

    private void drawDiffBtn(Rect rect, Texture2D tex, GameManager.Difficulty diff)
    {
        bool selected = GameManager.Instance.selectedDifficulty == diff;
        if (!selected) GUI.color = new Color(1f, 1f, 1f, 0.45f);
        bool clicked = imgButton(rect, tex);
        GUI.color = Color.white;
        if (clicked) GameManager.Instance.selectedDifficulty = diff;
    }

    // Returns a Rect preserving the texture's aspect ratio, anchored at (x, y) with the given width.
    private Rect texRect(float x, float y, float width, Texture2D tex)
    {
        float height = tex != null ? width * ((float)tex.height / tex.width) : width * 0.25f;
        return new Rect(x, y, width, height);
    }

    private bool imgButton(Rect rect, Texture2D tex)
    {
        if (tex == null) return GUI.Button(rect, "");
        if (!styleCache.TryGetValue(tex, out var style)) {
            style = new GUIStyle(GUIStyle.none) {
                normal = { background = tex },
                hover  = { background = tex },
                active = { background = tex },
            };
            styleCache[tex] = style;
        }
        return GUI.Button(rect, GUIContent.none, style);
    }
}
