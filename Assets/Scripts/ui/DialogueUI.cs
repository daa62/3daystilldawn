using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Self-building dialogue window: a speaker name, a line of text, and either a
// "Continue" button or a set of choice buttons. Callers drive the conversation with
// show() / showChoice() and can nest calls to chain lines (see FriendNpc).
// While open it frees the cursor and (because the player controller gates on a locked
// cursor) the player stops moving.
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    public bool IsOpen { get; private set; }

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
        if (IsOpen && continueButton.gameObject.activeSelf &&
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
        bg.rectTransform.sizeDelta = new Vector2(1500, 320);

        speakerLabel = UiFactory.text(panel.transform, "Speaker", "", 30,
                                      new Color(0.85f, 0.5f, 0.3f, 1f), TextAlignmentOptions.TopLeft);
        UiFactory.anchor(speakerLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        speakerLabel.rectTransform.anchoredPosition = new Vector2(40, -24);
        speakerLabel.rectTransform.sizeDelta = new Vector2(1000, 44);
        speakerLabel.fontStyle = FontStyles.Bold;

        bodyLabel = UiFactory.text(panel.transform, "Body", "", 30, Color.white, TextAlignmentOptions.TopLeft);
        UiFactory.anchor(bodyLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        bodyLabel.rectTransform.anchoredPosition = new Vector2(40, -80);
        bodyLabel.rectTransform.sizeDelta = new Vector2(1420, 150);

        continueButton = UiFactory.button(panel.transform, "Continue", "Continue  [Space]", 26f);
        UiFactory.anchor(continueButton.image.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
        continueButton.image.rectTransform.anchoredPosition = new Vector2(-40, 30);
        continueButton.image.rectTransform.sizeDelta = new Vector2(320, 60);
        continueButton.onClick.AddListener(continueClicked);

        var areaGO = new GameObject("Choices", typeof(RectTransform));
        areaGO.transform.SetParent(panel.transform, false);
        choiceArea = areaGO.GetComponent<RectTransform>();
        UiFactory.anchor(choiceArea, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
        choiceArea.anchoredPosition = new Vector2(0, 24);
        choiceArea.sizeDelta = new Vector2(-80, 0);
    }

    void buildChoices(string[] labels)
    {
        clearChoices();
        const float height = 60f, gap = 10f;
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            var btn = UiFactory.button(choiceArea, "Choice" + i, labels[i], 26f);
            var rt = btn.image.rectTransform;
            UiFactory.anchor(rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            rt.sizeDelta = new Vector2(1300, height);
            rt.anchoredPosition = new Vector2(0, (labels.Length - 1 - i) * (height + gap));
            btn.onClick.AddListener(() => choiceClicked(index));
            choiceButtons.Add(btn.gameObject);
        }
    }

    void clearChoices()
    {
        foreach (var go in choiceButtons)
            if (go != null) Destroy(go);
        choiceButtons.Clear();
    }
}
