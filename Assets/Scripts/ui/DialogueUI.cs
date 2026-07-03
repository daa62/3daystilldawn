using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Self-building dialogue window with a speaker line and either a Continue button or choice
// buttons; callers drive it via show() / showChoice() and nest calls to chain lines.
// While open the cursor is freed, which also stops player movement.
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    public bool IsOpen { get; private set; }

    // The frame the dialogue last closed. PlayerInteractor checks this so the key
    // press that dismissed the final line can't re-trigger the NPC the same frame.
    public int LastClosedFrame { get; private set; } = -1;

    // GetKeyDown stays true for the whole frame, so the E that opened the dialogue
    // would also "advance" it instantly — single-line conversations would open and
    // close invisibly. Ignore advance input on the frame a line appears.
    int shownFrame = -1;

    static readonly Color PANEL_BG = new Color(0.04f, 0.05f, 0.07f, 0.94f);

    GameObject panel;
    TextMeshProUGUI speakerLabel;
    TextMeshProUGUI bodyLabel;
    Button continueButton;
    RectTransform choiceArea;

    readonly List<GameObject> choiceButtons = new List<GameObject>();
    Action onContinue;
    Action<int> onChoose;

    void Awake()
    {
        Instance = this;
        build();
        panel.SetActive(false);
    }

    void Update()
    {
        // let Space / E advance a plain line (choices must be clicked)
        if (IsOpen && shownFrame != Time.frameCount && continueButton.gameObject.activeSelf &&
            (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E)))
            continueClicked();
    }

    // ---- public API ----

    public void show(string speaker, string line, Action onContinue)
    {
        open(speaker, line);
        this.onContinue = onContinue;
        this.onChoose = null;
        clearChoices();
        continueButton.gameObject.SetActive(true);
    }

    public void showChoice(string speaker, string line, string[] labels, Action<int> onChoose)
    {
        open(speaker, line);
        this.onChoose = onChoose;
        this.onContinue = null;
        continueButton.gameObject.SetActive(false);
        buildChoices(labels);
    }

    public void close()
    {
        IsOpen = false;
        LastClosedFrame = Time.frameCount;
        onContinue = null;
        onChoose = null;
        clearChoices();
        panel.SetActive(false);
        freezePlayer(false);
    }

    // ---- internals ----

    void open(string speaker, string line)
    {
        if (!IsOpen)
        {
            IsOpen = true;
            panel.SetActive(true);
            freezePlayer(true);
        }
        shownFrame = Time.frameCount;
        speakerLabel.text = speaker;
        bodyLabel.text = line;
    }

    void continueClicked()
    {
        Action cb = onContinue;
        onContinue = null;
        if (cb != null) cb();
        else close();
    }

    void choiceClicked(int index)
    {
        Action<int> cb = onChoose;
        onChoose = null;
        clearChoices();
        if (cb != null) cb(index);
        else close();
    }

    void freezePlayer(bool frozen)
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.lockCursor(!frozen);   // unlocked cursor = player input disabled
    }

    // ---- build ----

    void build()
    {
        UiFactory.ensureEventSystem();
        var canvas = UiFactory.overlayCanvas(transform, "DialogueCanvas");

        var bg = UiFactory.image(canvas.transform, "Panel", PANEL_BG);
        panel = bg.gameObject;
        UiFactory.anchor(bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        bg.rectTransform.anchoredPosition = new Vector2(0, 40);
        bg.rectTransform.sizeDelta = new Vector2(1500, 400);

        speakerLabel = UiFactory.text(panel.transform, "Speaker", "", 30,
                                      new Color(0.85f, 0.5f, 0.3f, 1f), TextAlignmentOptions.TopLeft);
        UiFactory.anchor(speakerLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        speakerLabel.rectTransform.anchoredPosition = new Vector2(40, -24);
        speakerLabel.rectTransform.sizeDelta = new Vector2(1000, 44);
        speakerLabel.fontStyle = FontStyles.Bold;

        bodyLabel = UiFactory.text(panel.transform, "Body", "", 30, Color.white, TextAlignmentOptions.TopLeft);
        UiFactory.anchor(bodyLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        bodyLabel.rectTransform.anchoredPosition = new Vector2(40, -80);
        bodyLabel.rectTransform.sizeDelta = new Vector2(1420, 100);

        continueButton = UiFactory.button(panel.transform, "Continue", "Continue  [Space]", 26f);
        UiFactory.anchor(continueButton.image.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
        continueButton.image.rectTransform.anchoredPosition = new Vector2(-40, 30);
        continueButton.image.rectTransform.sizeDelta = new Vector2(320, 60);
        continueButton.onClick.AddListener(continueClicked);

        // choices stack themselves: the layout group spaces the buttons, the fitter
        // grows the container upward from the panel's bottom edge to fit however
        // many there are — no per-button position math anywhere
        var areaGO = new GameObject("Choices", typeof(RectTransform));
        areaGO.transform.SetParent(panel.transform, false);
        choiceArea = areaGO.GetComponent<RectTransform>();
        UiFactory.anchor(choiceArea, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        choiceArea.anchoredPosition = new Vector2(0, 24);
        choiceArea.sizeDelta = new Vector2(1000, 0);

        var layout = areaGO.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = areaGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void buildChoices(string[] labels)
    {
        clearChoices();
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            var btn = UiFactory.button(choiceArea, "Choice" + i, labels[i], 20f);
            btn.image.rectTransform.sizeDelta = new Vector2(1000, 42);
            btn.onClick.AddListener(() => choiceClicked(index));
            choiceButtons.Add(btn.gameObject);
        }
    }

    void clearChoices()
    {
        foreach (var go in choiceButtons)
        {
            if (go == null) continue;
            // Destroy() is deferred to end of frame; deactivate first so the layout
            // group doesn't count the dead buttons when the next menu builds
            go.SetActive(false);
            Destroy(go);
        }
        choiceButtons.Clear();
    }
}
