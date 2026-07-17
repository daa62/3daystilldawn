using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Ending scene: picks the outcome from the run's accumulated GameState.
public class EndingController : MonoBehaviour
{
    static readonly Color BG = new Color(0.05f, 0.06f, 0.08f, 1f);

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        string slide = resolveSlide();
        if (slide != null) {
            CutscenePlayer.play(new[] { slide }, new[] { resolveEnding().body }, () =>
            {
                Sfx.ambience(Sfx.MUSIC_ENDING);
                build();
            });
        } else {
            build();
        }
    }

    // death has no slide; both lost-Samuel endings share one
    string resolveSlide()
    {
        var s = GameState.Instance;
        if (s == null || s.getFlag(GameManager.FLAG_DIED)) return null;

        bool bothSaved = s.getCounter(GameManager.COUNTER_FRIEND_HEALTH) >= GameManager.HEALTH_LINE
                      && s.getCounter(GameManager.COUNTER_BOND) >= GameManager.BOND_LINE;
        return bothSaved ? "ending_both" : "ending_alone";
    }

    (string title, string body) resolveEnding()
    {
        var s = GameState.Instance;
        if (s == null)
            return ("THREE DAYS TILL DAWN", "Thanks for playing the prototype.");

        if (s.getFlag(GameManager.FLAG_DIED))
            return ("YOU DIED",
                s.getFlag(GameManager.FLAG_FRIEND_MET)
                    ? "The dark swallowed you before the third dawn. Somewhere in the mart, Samuel waits for a friend who will never come back."
                    : "The horde caught you alone in the aisles. No one was left to remember your name.");

        // health gates first, bond only matters if the body held up
        int health = s.getCounter(GameManager.COUNTER_FRIEND_HEALTH);
        int bond   = s.getCounter(GameManager.COUNTER_BOND);

        if (health < GameManager.HEALTH_LINE)
            return ("HE TURNS",
                "Samuel can no longer fight the infection and quietly leaves the safe room. " +
                "You follow the trail he leaves behind, only to find that he has already fully transformed. " +
                "The rescue team arrives in time to save you, but is forced to put Samuel down.");

        if (bond < GameManager.BOND_LINE)
            return ("HE SLIPS AWAY",
                "Although Samuel's body continues to hold on, he loses the will to keep fighting. " +
                "Fearing he might hurt you if he turns, he quietly leaves the safe room during the night. " +
                "You follow the trail he leaves behind, only to find that he has already fully transformed. " +
                "The rescue team arrives in time to save you, but is forced to put Samuel down.");

        return ("BOTH SAVED",
            "Samuel holds onto both his health and his sense of self long enough for the rescue team to arrive. " +
            "You are rescued together before the infection fully takes over. " +
            "The rescue team reveals that a cure has been developed for survivors who have not yet " +
            "completely turned, giving Samuel a chance to recover.");
    }

    void build()
    {
        UiFactory.ensureEventSystem();
        var canvas = UiFactory.overlayCanvas(transform, "EndingCanvas");

        string slide = resolveSlide();
        var art = slide != null ? Resources.Load<Sprite>("Cutscenes/" + slide) : null;
        if (art != null) {
            var back = UiFactory.image(canvas.transform, "Backdrop", Color.white);
            UiFactory.stretch(back.rectTransform);
            back.sprite = art;
            back.preserveAspect = true;
        }

        var bg = UiFactory.image(canvas.transform, "Background",
            art != null ? new Color(BG.r, BG.g, BG.b, 0.78f) : BG);
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

    void resetRun()
    {
        DayCycle.reset();   // day counter, friend health/bond, story flags
    }
}
