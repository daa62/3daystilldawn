using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Builds the ending screen from the run's accumulated GameState, so the outcome is a
// consequence of prior play (supplies gathered, the bond with Mia, whether the player
// survived) rather than a single final choice. Lives in the Ending scene.
public class EndingController : MonoBehaviour
{
    static readonly Color BG = new Color(0.05f, 0.06f, 0.08f, 1f);

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        build();
    }

    (string title, string body) resolveEnding()
    {
        var s = GameState.Instance;
        if (s == null)
            return ("THREE DAYS TILL DAWN", "Thanks for playing the prototype.");

        if (s.getFlag(GameManager.FLAG_DIED))
            return ("YOU DIED",
                s.getFlag(GameManager.FLAG_FRIEND_MET)
                    ? "The dark swallowed you before the third dawn. Somewhere in the mart, Mia waits for a friend who will never come back."
                    : "The horde caught you alone in the aisles. No one was left to remember your name.");

        // The spec's ordered check: health gates first — a failing body turns no matter
        // how strong the bond. Only then does the will to hold on decide it.
        int health = s.getCounter(GameManager.COUNTER_FRIEND_HEALTH);
        int bond   = s.getCounter(GameManager.COUNTER_BOND);

        if (health < GameManager.HEALTH_LINE)
            return ("SHE TURNS",
                "Her body can't fight the infection any longer. Sometime before dawn, Mia leaves the safe room. " +
                "You follow the trail she left behind — and find her already fully transformed. " +
                "The rescue team arrives in time to save you. For Mia, they have no choice.");

        if (bond < GameManager.BOND_LINE)
            return ("SHE SLIPS AWAY",
                "Her body holds on, but she has lost the will to keep fighting. Afraid of what she'll do to you if she turns, " +
                "Mia quietly leaves the safe room during the night. You follow the trail she left behind — " +
                "and find her already fully transformed. The rescue team arrives in time to save you. For Mia, they have no choice.");

        return ("BOTH SAVED",
            "Mia holds onto her health and her sense of self long enough for the rescue team to arrive. " +
            "She is lifted out alongside you — and the team reveals a cure has been developed for survivors " +
            "who haven't yet completely turned. You both made it to the third dawn.");
    }

    void build()
    {
        UiFactory.ensureEventSystem();
        var canvas = UiFactory.overlayCanvas(transform, "EndingCanvas");

        var bg = UiFactory.image(canvas.transform, "Background", BG);
        UiFactory.stretch(bg.rectTransform);

        var (title, body) = resolveEnding();

        var titleLabel = UiFactory.text(canvas.transform, "Title", title, 84,
            new Color(0.85f, 0.3f, 0.25f, 1f), TextAlignmentOptions.Center);
        titleLabel.fontStyle = FontStyles.Bold;
        UiFactory.anchor(titleLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        titleLabel.rectTransform.anchoredPosition = new Vector2(0, 240);
        titleLabel.rectTransform.sizeDelta = new Vector2(1500, 140);

        var bodyLabel = UiFactory.text(canvas.transform, "Body", body, 34, Color.white, TextAlignmentOptions.Top);
        UiFactory.anchor(bodyLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        bodyLabel.rectTransform.anchoredPosition = new Vector2(0, 40);
        bodyLabel.rectTransform.sizeDelta = new Vector2(1200, 260);

        var button = UiFactory.button(canvas.transform, "TitleButton", "Return to Title", 30f);
        UiFactory.anchor(button.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        button.image.rectTransform.anchoredPosition = new Vector2(0, -230);
        button.image.rectTransform.sizeDelta = new Vector2(360, 72);
        button.onClick.AddListener(() =>
        {
            if (GameState.Instance != null) resetRun();
            SceneLoader.load(GameManager.SCENE_TITLE);
        });
    }

    // clear the run's narrative state so a new playthrough starts fresh
    void resetRun()
    {
        DayCycle.reset();   // day counter, friend health/bond, and story flags
    }
}
